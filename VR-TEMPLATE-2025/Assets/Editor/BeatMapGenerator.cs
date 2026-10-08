using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public enum BeatMapDifficulty
{
    Beginner,
    VeryEasy,
    Easier,
    Easy,
    Normal,
    Medium,
    Hard
}

public class BeatMapGenerator : EditorWindow
{
    // ---------------------------------------------------------------
    // Configuração básica
    // ---------------------------------------------------------------
    private AudioClip clip;
    private BeatMapDifficulty difficulty = BeatMapDifficulty.Medium;

    private Vector2 scrollPos;
    private List<BeatPoint> previewBeats = new();
    private bool hasPreview = false;

    // ---------------------------------------------------------------
    // Warmup (igual ao original)
    // ---------------------------------------------------------------
    private float warmupThresholdMultiplier = 2.0f;
    private float warmupDuration = 5f;
    private float warmupMinDistanceMultiplier = 3f;

    // ---------------------------------------------------------------
    // Análise espectral (novo)
    // ---------------------------------------------------------------
    private static readonly int[] FftSizes = { 512, 1024, 2048, 4096 };
    private static readonly string[] FftSizeLabels = { "512", "1024", "2048", "4096" };
    private int fftSizeIndex = 1; // 1024 por padrão

    private static readonly int[] HopDivisors = { 1, 2, 4 };
    private static readonly string[] HopLabels = { "1x (sem overlap)", "2x (50% overlap)", "4x (75% overlap)" };
    private int hopDivisorIndex = 1; // 50% overlap por padrão

    private bool showBands = false;
    private float bassMaxHz = 150f;   // "tum" — grave / kick / linha de baixo
    private float midMaxHz = 2000f;   // "bam" — médio / snare / vocal / corpo do instrumento
    private float trebleMaxHz = 8000f; // agudo — hi-hat / prato / "tss"

    // ---------------------------------------------------------------
    // Curvas por dificuldade (mesmas do original)
    // ---------------------------------------------------------------
    private static readonly float[] ThresholdByDifficulty =
    {
        4.0f, // Beginner
        3.5f, // VeryEasy
        3.0f, // Easier
        2.6f, // Easy
        2.2f, // Normal
        1.8f, // Medium
        1.4f
    };

    private static readonly float[] MinDistanceByDifficulty =
    {
        2.00f, // Beginner
        1.60f, // VeryEasy
        1.30f, // Easier
        1.00f, // Easy
        0.75f, // Normal
        0.55f, // Medium
        0.40f
    };

    private static readonly int[] HistorySizeByDifficulty =
    {
        20, // Beginner
        25, // VeryEasy
        30, // Easier
        35, // Easy
        40, // Normal
        50, // Medium
        60
    };

    // Quantos cubos "densos" seguidos (gap perto do mínimo) antes de forçar um respiro.
    private static readonly int[] MaxStreakByDifficulty =
    {
        3,  // Beginner
        4,  // VeryEasy
        5,  // Easier
        6,  // Easy
        8,  // Normal
        10, // Medium
        12
    };

    // ---------------------------------------------------------------
    // Ritmo / Respiro (pacing)
    // Nunca altera o tempo de um cubo: só escolhe QUAIS onsets detectados viram cubo.
    // ---------------------------------------------------------------
    private bool showPacing = true;
    private bool usePacing = true;
    private bool snapToBeatGrid = true;
    private float gridToleranceMs = 60f;
    private int phraseBars = 4;
    private float breathGapMultiplier = 2.5f;
    private float energyInfluence = 0.5f;

    private const float DenseGapFactor = 1.5f;     // gap < minDistance * isso conta como "denso"
    private const float LookaheadFactor = 0.3f;    // janela pra procurar um onset melhor à frente
    private const float MaxGridRejectRatio = 0.6f; // acima disso o grid provavelmente está errado

    private string lastStats = "";

    // ---------------------------------------------------------------
    // Geração em lote (novo)
    // ---------------------------------------------------------------
    private string batchFolderPath = "Assets/Audio";
    private string batchOutputFolder = "Assets/BeatMaps";
    private bool batchOverwriteExisting = false;

    [MenuItem("Tools/Generate Beat Map")]
    public static void Open() => GetWindow<BeatMapGenerator>("Beat Map Generator");

    private void OnGUI()
    {
        GUILayout.Label("Beat Map Generator", EditorStyles.boldLabel);
        GUILayout.Space(6);

        clip = (AudioClip)EditorGUILayout.ObjectField(
            "Audio Clip", clip, typeof(AudioClip), false);

        GUILayout.Space(8);

        difficulty = (BeatMapDifficulty)EditorGUILayout.EnumPopup("Difficulty", difficulty);

        GUILayout.Space(4);

        int idx = (int)difficulty;
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.FloatField("  Beat Threshold", ThresholdByDifficulty[idx]);
        EditorGUILayout.FloatField("  Min Beat Distance", MinDistanceByDifficulty[idx]);
        EditorGUILayout.IntField("  History Size", HistorySizeByDifficulty[idx]);
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(8);
        GUILayout.Label("Análise Espectral (FFT)", EditorStyles.boldLabel);
        fftSizeIndex = EditorGUILayout.Popup("Tamanho da Janela FFT", fftSizeIndex, FftSizeLabels);
        hopDivisorIndex = EditorGUILayout.Popup("Overlap entre janelas", hopDivisorIndex, HopLabels);

        showBands = EditorGUILayout.Foldout(showBands, "Bandas de Frequência (Hz)");
        if (showBands)
        {
            EditorGUI.indentLevel++;
            bassMaxHz = EditorGUILayout.Slider("Fim do Grave (tum)", bassMaxHz, 40f, 400f);
            midMaxHz = EditorGUILayout.Slider("Fim do Médio (bam)", midMaxHz, bassMaxHz + 50f, 6000f);
            trebleMaxHz = EditorGUILayout.Slider("Fim do Agudo", trebleMaxHz, midMaxHz + 200f, 16000f);
            EditorGUI.indentLevel--;
        }

        GUILayout.Space(8);
        GUILayout.Label("Warmup", EditorStyles.boldLabel);
        warmupDuration = EditorGUILayout.Slider("Warmup Duration (s)", warmupDuration, 1f, 10f);
        warmupMinDistanceMultiplier = EditorGUILayout.Slider(
            "Warmup Min Distance Mult", warmupMinDistanceMultiplier, 1f, 15f);
        warmupThresholdMultiplier = EditorGUILayout.Slider(
            "Warmup Threshold Mult", warmupThresholdMultiplier, 1.2f, 4f);

        GUILayout.Space(8);
        showPacing = EditorGUILayout.Foldout(showPacing, "Ritmo / Respiro", true, EditorStyles.foldoutHeader);
        if (showPacing)
        {
            EditorGUI.indentLevel++;
            usePacing = EditorGUILayout.Toggle("Ativar Ritmo/Respiro", usePacing);
            EditorGUI.BeginDisabledGroup(!usePacing);
            snapToBeatGrid = EditorGUILayout.Toggle(
                new GUIContent("Somente no Beat", "Descarta onsets fora do grid de beats detectado (BPM)."),
                snapToBeatGrid);
            gridToleranceMs = EditorGUILayout.Slider("Tolerância do Grid (ms)", gridToleranceMs, 20f, 120f);
            phraseBars = EditorGUILayout.IntSlider(
                new GUIContent("Frase (compassos)", "Tamanho do ciclo sobe → pico → respiro, em compassos de 4 beats."),
                phraseBars, 1, 16);
            breathGapMultiplier = EditorGUILayout.Slider(
                new GUIContent("Gap no Respiro (x)", "Quanto o espaço mínimo entre cubos cresce nos momentos de respiro."),
                breathGapMultiplier, 1f, 6f);
            energyInfluence = EditorGUILayout.Slider(
                new GUIContent("Influência da Energia", "0 = só o ciclo da frase, 1 = só a energia da música."),
                energyInfluence, 0f, 1f);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField("  Máx. Cubos Densos Seguidos", MaxStreakByDifficulty[idx]);
            EditorGUI.EndDisabledGroup();
            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;
        }

        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Preview"))
            Preview();

        EditorGUI.BeginDisabledGroup(!hasPreview);
        if (GUILayout.Button("Save Asset"))
            SaveAsset();
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();

        if (hasPreview && previewBeats.Count > 0)
        {
            GUILayout.Space(8);
            GUILayout.Label(
                $"Beats detectados: {previewBeats.Count}",
                EditorStyles.boldLabel);

            if (!string.IsNullOrEmpty(lastStats))
                EditorGUILayout.HelpBox(lastStats, MessageType.None);

            scrollPos = EditorGUILayout.BeginScrollView(
                scrollPos, GUILayout.Height(200));

            float prevTime = float.NaN;
            foreach (var b in previewBeats)
            {
                string gap = float.IsNaN(prevTime) ? "" : $"   gap={b.time - prevTime:F2}s";
                EditorGUILayout.LabelField(
                    $"t={b.time:F3}s   intensity={b.intensity:F2}   affinity={b.affinity}{gap}");
                prevTime = b.time;
            }

            EditorGUILayout.EndScrollView();
        }

        GUILayout.Space(16);
        GUILayout.Label("Geração em Lote (pasta inteira)", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        batchFolderPath = EditorGUILayout.TextField("Pasta de Áudio", batchFolderPath);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string picked = EditorUtility.OpenFolderPanel("Selecionar pasta com músicas", "Assets", "");
            if (!string.IsNullOrEmpty(picked))
            {
                if (picked.StartsWith(Application.dataPath))
                    batchFolderPath = "Assets" + picked.Substring(Application.dataPath.Length);
                else
                    Debug.LogWarning("Selecione uma pasta dentro do projeto (Assets/...).");
            }
        }
        EditorGUILayout.EndHorizontal();

        batchOutputFolder = EditorGUILayout.TextField("Pasta de Saída", batchOutputFolder);
        batchOverwriteExisting = EditorGUILayout.Toggle("Sobrescrever existentes", batchOverwriteExisting);

        GUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "Gera um BeatMap para CADA música encontrada na pasta, usando a Dificuldade e os " +
            "parâmetros acima. Ideal pra deixar rodando sozinho enquanto você faz outra coisa.",
            MessageType.Info);

        if (GUILayout.Button("Gerar Beat Maps da Pasta Inteira"))
            RunBatch();
    }

    private void Preview()
    {
        if (clip == null)
        {
            Debug.LogError("Selecione um AudioClip antes de gerar o preview.");
            return;
        }

        previewBeats = DetectBeats(clip);
        hasPreview = true;
        Repaint();
    }

    private void SaveAsset()
    {
        if (previewBeats == null || previewBeats.Count == 0)
        {
            Debug.LogWarning("Nenhum beat para salvar. Rode o Preview primeiro.");
            return;
        }

        SongBeatMap beatMap = ScriptableObject.CreateInstance<SongBeatMap>();
        beatMap.audioClip = clip;
        beatMap.beats = previewBeats;

        string path = EditorUtility.SaveFilePanelInProject(
            "Salvar Beat Map",
            $"{clip.name}_{difficulty}_BeatMap",
            "asset",
            "Escolha onde salvar");

        if (string.IsNullOrEmpty(path))
            return;

        AssetDatabase.CreateAsset(beatMap, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"BeatMap salvo: {previewBeats.Count} beats em '{path}'.");
    }

    // =================================================================
    // GERAÇÃO EM LOTE
    // =================================================================
    private void RunBatch()
    {
        if (!AssetDatabase.IsValidFolder(batchFolderPath))
        {
            Debug.LogError($"Pasta de áudio inválida: '{batchFolderPath}'. Precisa ser um caminho dentro de Assets/.");
            return;
        }

        if (!AssetDatabase.IsValidFolder(batchOutputFolder))
            CreateFolderRecursive(batchOutputFolder);

        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { batchFolderPath });
        int total = guids.Length;
        int processed = 0, skipped = 0, failed = 0;

        if (total == 0)
        {
            Debug.LogWarning($"Nenhum AudioClip encontrado em '{batchFolderPath}'.");
            return;
        }

        try
        {
            for (int i = 0; i < total; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);

                bool cancel = EditorUtility.DisplayCancelableProgressBar(
                    "Gerando Beat Maps",
                    $"({i + 1}/{total}) {Path.GetFileNameWithoutExtension(assetPath)}",
                    (float)i / total);
                if (cancel)
                {
                    Debug.LogWarning($"Geração em lote cancelada pelo usuário em {i}/{total}.");
                    break;
                }

                AudioClip audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                if (audioClip == null)
                {
                    Debug.LogError($"Não foi possível carregar '{assetPath}'.");
                    failed++;
                    continue;
                }

                // AudioClips com Load Type = Streaming não expõem PCM via GetData()
                // (retorna silêncio) — detectamos e pulamos em vez de gerar um beat map vazio.
                if (IsStreamingClip(assetPath))
                {
                    Debug.LogWarning(
                        $"'{assetPath}' está com Load Type = Streaming; GetData() não funciona nesse modo. " +
                        "Selecione o clip e mude para 'Decompress On Load' ou 'Compressed In Memory'. Pulando.");
                    skipped++;
                    continue;
                }

                string outputAssetPath = $"{batchOutputFolder}/{audioClip.name}_{difficulty}_BeatMap.asset";

                if (!batchOverwriteExisting &&
                    AssetDatabase.LoadAssetAtPath<SongBeatMap>(outputAssetPath) != null)
                {
                    skipped++;
                    continue;
                }

                try
                {
                    bool wasLoaded = audioClip.loadState == AudioDataLoadState.Loaded;
                    audioClip.LoadAudioData();

                    List<BeatPoint> beats = DetectBeats(audioClip);

                    if (!wasLoaded)
                        audioClip.UnloadAudioData();

                    Debug.Log($"[BeatMap] {audioClip.name} ({difficulty}): {beats.Count} cubos — {lastStats}");

                    SongBeatMap beatMap = ScriptableObject.CreateInstance<SongBeatMap>();
                    beatMap.audioClip = audioClip;
                    beatMap.beats = beats;

                    if (AssetDatabase.LoadAssetAtPath<SongBeatMap>(outputAssetPath) != null)
                        AssetDatabase.DeleteAsset(outputAssetPath);

                    AssetDatabase.CreateAsset(beatMap, outputAssetPath);
                    processed++;
                }
                catch (System.Exception ex)
                {
                    // Uma música problemática não deve derrubar uma rodada de horas.
                    Debug.LogError($"Falha ao processar '{assetPath}': {ex.Message}");
                    failed++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
        }

        Debug.Log(
            $"Lote concluído: {processed} gerados, {skipped} pulados, {failed} falharam (de {total} músicas). " +
            $"Saída em '{batchOutputFolder}'.");
    }

    private static bool IsStreamingClip(string assetPath)
    {
        AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
        if (importer == null) return false;
        return importer.defaultSampleSettings.loadType == AudioClipLoadType.Streaming;
    }

    private static void CreateFolderRecursive(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        string[] parts = folderPath.Split('/');
        string current = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    // =================================================================
    // DETECÇÃO DE BEATS — FFT + Spectral Flux por banda
    // =================================================================

    private class BandState
    {
        public MusicAffinity affinity;
        public int binStart;
        public int binEnd; // exclusivo
        public List<float> history = new();
        public float lastBeatTime;
        public float maxFlux;
        public List<BeatPoint> beats = new();
    }

    private List<BeatPoint> DetectBeats(AudioClip targetClip)
    {
        if (targetClip == null) return new List<BeatPoint>();

        int idx = (int)difficulty;
        float thresholdMultiplier = ThresholdByDifficulty[idx];
        float minDistance = MinDistanceByDifficulty[idx];
        int historySize = HistorySizeByDifficulty[idx];

        int fftSize = FftSizes[fftSizeIndex];
        int hop = Mathf.Max(1, fftSize / HopDivisors[hopDivisorIndex]);

        float[] mono = DownmixToMono(targetClip);
        int sampleRate = targetClip.frequency;
        float nyquist = sampleRate / 2f;

        if (mono.Length < fftSize)
        {
            Debug.LogWarning($"'{targetClip.name}' é curto demais pra janela FFT escolhida ({fftSize} amostras).");
            return new List<BeatPoint>();
        }

        // Garante bandas válidas mesmo se o usuário setar valores estranhos ou o clipe tiver sample rate baixo.
        float clampedTrebleMax = Mathf.Min(trebleMaxHz, nyquist - 1f);
        float clampedMidMax = Mathf.Min(midMaxHz, clampedTrebleMax - 1f);
        float clampedBassMax = Mathf.Min(bassMaxHz, clampedMidMax - 1f);

        float[] window = BuildHannWindow(fftSize);
        int binCount = fftSize / 2;
        float freqPerBin = sampleRate / (float)fftSize;

        int bassEndBin = BinFor(clampedBassMax, freqPerBin, binCount);
        int midEndBin = BinFor(clampedMidMax, freqPerBin, binCount);
        int trebleEndBin = BinFor(clampedTrebleMax, freqPerBin, binCount);

        var bands = new[]
        {
            new BandState { affinity = MusicAffinity.Bass,   binStart = 1,               binEnd = bassEndBin },
            new BandState { affinity = MusicAffinity.Mid,    binStart = bassEndBin + 1,  binEnd = midEndBin },
            new BandState { affinity = MusicAffinity.Treble, binStart = midEndBin + 1,   binEnd = trebleEndBin },
        };

        float initialLastBeat = -minDistance * warmupMinDistanceMultiplier;
        foreach (var b in bands) b.lastBeatTime = initialLastBeat;

        float[] prevMag = new float[binCount];
        float[] re = new float[fftSize];
        float[] im = new float[fftSize];

        int totalFrames = Mathf.Max(0, (mono.Length - fftSize) / hop + 1);
        int minHistoryToDetect = Mathf.Max(4, historySize / 4);

        // Curvas por frame usadas pelo pacing: força de onset (pro beat tracking) e energia (pra densidade).
        float[] onsetStrength = new float[totalFrames];
        float[] frameEnergy = new float[totalFrames];

        for (int frame = 0; frame < totalFrames; frame++)
        {
            int start = frame * hop;

            for (int n = 0; n < fftSize; n++)
            {
                re[n] = mono[start + n] * window[n];
                im[n] = 0f;
            }

            SimpleFFT.Forward(re, im);

            float time = (start + fftSize * 0.5f) / sampleRate;
            bool isWarmup = time < warmupDuration;

            foreach (var band in bands)
            {
                float flux = 0f;
                for (int k = band.binStart; k < band.binEnd; k++)
                {
                    float mag = Mathf.Sqrt(re[k] * re[k] + im[k] * im[k]);
                    float diff = mag - prevMag[k];
                    if (diff > 0f) flux += diff; // spectral flux: só conta aumento de energia (ataque/onset)
                    prevMag[k] = mag;
                    frameEnergy[frame] += mag;
                }

                onsetStrength[frame] += flux;

                float avg = Average(band.history);
                float stdDev = StdDev(band.history, avg);

                band.history.Add(flux);
                if (band.history.Count > historySize)
                    band.history.RemoveAt(0);

                if (band.history.Count < minHistoryToDetect)
                    continue; // baseline ainda instável, evita falso positivo no começo

                float activeThreshold = isWarmup ? thresholdMultiplier * warmupThresholdMultiplier : thresholdMultiplier;
                float dynamicThreshold = avg + stdDev * activeThreshold;

                float activeMinDistance = isWarmup
                    ? Mathf.Lerp(minDistance * warmupMinDistanceMultiplier, minDistance, time / warmupDuration)
                    : minDistance;

                if (flux <= dynamicThreshold) continue;
                if (time - band.lastBeatTime < activeMinDistance) continue;

                band.lastBeatTime = time;
                band.maxFlux = Mathf.Max(band.maxFlux, flux);
                band.beats.Add(new BeatPoint { time = time, intensity = flux, affinity = band.affinity });
            }
        }

        List<BeatPoint> merged = new();
        foreach (var band in bands)
        {
            if (band.maxFlux > 0f)
            {
                for (int i = 0; i < band.beats.Count; i++)
                {
                    var b = band.beats[i];
                    b.intensity = Mathf.Clamp01(b.intensity / band.maxFlux);
                    band.beats[i] = b;
                }
            }
            merged.AddRange(band.beats);
        }

        merged.Sort((a, b) => a.time.CompareTo(b.time));

        // Duas bandas podem disparar quase no mesmo instante (ex.: kick + snare juntos
        // formando o "tum-bam" do refrão). Isso funde esses casos num único cubo,
        // ficando com o de maior intensidade, em vez de gerar dois cubos colados.
        List<BeatPoint> final = new();
        float dedupeWindow = minDistance * 0.5f;
        foreach (var b in merged)
        {
            if (final.Count > 0 && b.time - final[final.Count - 1].time < dedupeWindow)
            {
                if (b.intensity > final[final.Count - 1].intensity)
                    final[final.Count - 1] = b;
                continue;
            }
            final.Add(b);
        }

        float frameRate = sampleRate / (float)hop;
        float frameOffset = fftSize * 0.5f / sampleRate;

        if (!usePacing)
        {
            lastStats = BuildStats(final, minDistance, 0f, false, targetClip.length);
            return final;
        }

        List<BeatPoint> paced = ApplyPacing(final, onsetStrength, frameEnergy, frameRate, frameOffset, minDistance, idx,
            out float bpm, out bool gridUsed);
        lastStats = BuildStats(paced, minDistance, bpm, gridUsed, targetClip.length);
        return paced;
    }

    // =================================================================
    // PACING — Ritmo / Respiro
    // Recebe os onsets detectados (já no tempo exato da música) e escolhe quais viram cubo.
    // Nenhum tempo é alterado ou criado: todo cubo continua exatamente em cima de um onset.
    // =================================================================
    private List<BeatPoint> ApplyPacing(List<BeatPoint> candidates, float[] onsetStrength, float[] frameEnergy,
        float frameRate, float frameOffset, float minDistance, int diffIdx, out float bpm, out bool gridUsed)
    {
        gridUsed = false;

        // 1. Beat tracking → grid de beats reais da música (acompanha variações leves de tempo).
        List<float> beatTimes = TrackBeats(onsetStrength, frameRate, frameOffset, out bpm);
        bool hasGrid = beatTimes.Count >= 8;

        // Dificuldades mais altas aceitam contratempo (meio beat); as mais baixas só o beat cheio.
        int subdivisions = diffIdx >= (int)BeatMapDifficulty.Normal ? 2 : 1;
        List<float> grid = hasGrid ? BuildSubdividedGrid(beatTimes, subdivisions) : null;
        float tolerance = gridToleranceMs / 1000f;

        // 2. Reforça "cubo no beat": descarta onsets fora do grid. Se o grid descartaria demais,
        //    o BPM detectado provavelmente está errado — aí usa o grid só como preferência.
        List<BeatPoint> pool = candidates;
        if (hasGrid && snapToBeatGrid)
        {
            List<BeatPoint> onGrid = new();
            foreach (var c in candidates)
                if (DistanceToNearest(grid, c.time) <= tolerance)
                    onGrid.Add(c);

            float rejected = 1f - onGrid.Count / (float)Mathf.Max(1, candidates.Count);
            if (rejected <= MaxGridRejectRatio)
            {
                pool = onGrid;
                gridUsed = true;
            }
            else
            {
                Debug.LogWarning(
                    $"[BeatMap] Grid de beats ({bpm:F0} BPM) descartaria {rejected:P0} dos onsets; " +
                    "usando o grid só como preferência.");
            }
        }

        // 3. Curva de energia suavizada (~1.5s) e normalizada (p95 = 1).
        float[] energy = SmoothAndNormalize(frameEnergy, Mathf.Max(1, Mathf.RoundToInt(frameRate * 1.5f)));

        int maxStreak = MaxStreakByDifficulty[diffIdx];
        float breathGap = minDistance * breathGapMultiplier;
        int phraseBeats = Mathf.Max(1, phraseBars * 4);

        // 4. Seleção gulosa com gap variável: o espaço mínimo entre cubos sobe e desce ao longo
        //    da frase e com a energia. Depois de muitos cubos densos seguidos, força um respiro.
        List<BeatPoint> result = new();
        float lastTime = float.NegativeInfinity;
        int streak = 0;
        int i = 0;

        while (i < pool.Count)
        {
            float t = pool[i].time;
            float need = RequiredGap(t, minDistance, beatTimes, hasGrid, phraseBeats, energy, frameRate, frameOffset);
            if (streak >= maxStreak)
                need = Mathf.Max(need, breathGap);

            if (t - lastTime < need)
            {
                i++;
                continue;
            }

            // Olha um pouco à frente: se tiver um onset mais forte / mais no beat logo depois, fica com ele.
            int best = i;
            float bestScore = Score(pool[i], grid, beatTimes, hasGrid, tolerance);
            for (int j = i + 1; j < pool.Count && pool[j].time <= t + need * LookaheadFactor; j++)
            {
                float s = Score(pool[j], grid, beatTimes, hasGrid, tolerance);
                if (s > bestScore)
                {
                    bestScore = s;
                    best = j;
                }
            }

            BeatPoint chosen = pool[best];
            float gap = chosen.time - lastTime;
            streak = gap < minDistance * DenseGapFactor ? streak + 1 : 0;

            result.Add(chosen);
            lastTime = chosen.time;
            i = best + 1;
        }

        return result;
    }

    /// <summary>
    /// Gap mínimo exigido no tempo t. Mistura um ciclo por frase (sobe → pico → respira)
    /// com a energia da música: densidade 1 = minDistance, densidade 0 = minDistance * breathGapMultiplier.
    /// </summary>
    private float RequiredGap(float t, float minDistance, List<float> beatTimes, bool hasGrid, int phraseBeats,
        float[] energy, float frameRate, float frameOffset)
    {
        float phase;
        if (hasGrid)
        {
            phase = Mathf.Repeat(BeatPosition(beatTimes, t), phraseBeats) / phraseBeats;
        }
        else
        {
            // Sem grid confiável: usa ~2s por compasso como aproximação.
            float phraseSeconds = phraseBars * 2f;
            phase = Mathf.Repeat(t, phraseSeconds) / phraseSeconds;
        }

        // Sobe durante 80% da frase, desce até o respiro no fim (e início da próxima).
        float wave = phase < 0.8f
            ? Mathf.SmoothStep(0f, 1f, phase / 0.8f)
            : Mathf.SmoothStep(1f, 0f, (phase - 0.8f) / 0.2f);

        int frame = Mathf.Clamp(Mathf.RoundToInt((t - frameOffset) * frameRate), 0, energy.Length - 1);
        float density = Mathf.Lerp(wave, energy[frame], energyInfluence);

        return minDistance * Mathf.Lerp(breathGapMultiplier, 1f, density);
    }

    /// <summary>Intensidade ponderada pelo alinhamento com o beat (beat cheio vale mais que contratempo).</summary>
    private static float Score(BeatPoint b, List<float> grid, List<float> beatTimes, bool hasGrid, float tolerance)
    {
        if (!hasGrid) return b.intensity;

        float onBeat = 1f - Mathf.Clamp01(DistanceToNearest(beatTimes, b.time) / tolerance);
        float onSub = 1f - Mathf.Clamp01(DistanceToNearest(grid, b.time) / tolerance);
        float align = Mathf.Max(onBeat, onSub * 0.7f);

        return b.intensity * (1f + 0.5f * align);
    }

    /// <summary>
    /// Beat tracker por programação dinâmica (Ellis 2007): estima o período por autocorrelação
    /// da curva de onset e acha a sequência de beats que melhor casa com os onsets mantendo o período.
    /// </summary>
    private static List<float> TrackBeats(float[] onset, float frameRate, float frameOffset, out float bpm)
    {
        bpm = 0f;
        List<float> beats = new();
        int n = onset.Length;
        if (n < 16) return beats;

        float mean = 0f;
        foreach (float v in onset) mean += v;
        mean /= n;
        float variance = 0f;
        foreach (float v in onset) variance += (v - mean) * (v - mean);
        float std = Mathf.Sqrt(variance / n);
        if (std <= 0f) return beats;

        // Autocorrelação ponderada em torno de 120 BPM pra evitar erro de oitava (60/240).
        int minLag = Mathf.Max(1, Mathf.RoundToInt(frameRate * 60f / 200f));
        int maxLag = Mathf.Min(n / 2, Mathf.RoundToInt(frameRate * 60f / 60f));
        if (maxLag <= minLag + 1) return beats;

        float[] ac = new float[maxLag + 2];
        int bestLag = minLag;
        float bestWeighted = float.MinValue;
        for (int lag = minLag - 1; lag <= maxLag + 1; lag++)
        {
            if (lag < 1) continue;
            double sum = 0;
            for (int k = 0; k + lag < n; k++)
                sum += (onset[k] - mean) * (onset[k + lag] - mean);
            ac[lag] = (float)(sum / (n - lag));

            if (lag < minLag || lag > maxLag) continue;
            float lagBpm = 60f * frameRate / lag;
            float octaves = Mathf.Log(lagBpm / 120f, 2f);
            float weighted = ac[lag] * Mathf.Exp(-0.5f * octaves * octaves);
            if (weighted > bestWeighted)
            {
                bestWeighted = weighted;
                bestLag = lag;
            }
        }

        // Refino sub-frame do período (interpolação parabólica).
        float period = bestLag;
        float a = ac[Mathf.Max(1, bestLag - 1)], b = ac[bestLag], c = ac[bestLag + 1];
        float denom = a - 2f * b + c;
        if (Mathf.Abs(denom) > 1e-9f)
            period += Mathf.Clamp(0.5f * (a - c) / denom, -0.5f, 0.5f);
        bpm = 60f * frameRate / period;

        // Programação dinâmica.
        const float tightness = 100f;
        float[] norm = new float[n];
        for (int k = 0; k < n; k++) norm[k] = onset[k] / std;

        float[] score = new float[n];
        int[] back = new int[n];
        int lo = Mathf.RoundToInt(period * 2f);
        int hi = Mathf.Max(1, Mathf.RoundToInt(period * 0.5f));

        for (int t = 0; t < n; t++)
        {
            int bestPrev = -1;
            float bestVal = float.MinValue;
            for (int p = Mathf.Max(0, t - lo); p <= t - hi; p++)
            {
                float r = Mathf.Log((t - p) / period);
                float val = score[p] - tightness * r * r;
                if (val > bestVal)
                {
                    bestVal = val;
                    bestPrev = p;
                }
            }

            score[t] = norm[t] + (bestPrev >= 0 ? Mathf.Max(0f, bestVal) : 0f);
            back[t] = bestVal > 0f ? bestPrev : -1;
        }

        // Começa do melhor frame no último período e volta pelos backlinks.
        int end = n - 1;
        for (int t = Mathf.Max(0, n - Mathf.CeilToInt(period)); t < n; t++)
            if (score[t] > score[end]) end = t;

        List<int> frames = new();
        for (int t = end; t >= 0; t = back[t])
            frames.Add(t);
        frames.Reverse();

        foreach (int f in frames)
            beats.Add(f / frameRate + frameOffset);

        return beats;
    }

    private static List<float> BuildSubdividedGrid(List<float> beatTimes, int subdivisions)
    {
        List<float> grid = new();
        for (int k = 0; k < beatTimes.Count; k++)
        {
            grid.Add(beatTimes[k]);
            if (k + 1 >= beatTimes.Count) break;

            float span = beatTimes[k + 1] - beatTimes[k];
            for (int s = 1; s < subdivisions; s++)
                grid.Add(beatTimes[k] + span * s / subdivisions);
        }
        return grid;
    }

    /// <summary>Posição contínua em beats (ex.: 12.5 = meio caminho entre o beat 12 e o 13).</summary>
    private static float BeatPosition(List<float> beatTimes, float t)
    {
        int k = beatTimes.BinarySearch(t);
        if (k < 0) k = ~k - 1;

        if (k < 0)
        {
            float span0 = beatTimes[1] - beatTimes[0];
            return (t - beatTimes[0]) / span0;
        }
        if (k >= beatTimes.Count - 1)
        {
            int last = beatTimes.Count - 1;
            float spanN = beatTimes[last] - beatTimes[last - 1];
            return last + (t - beatTimes[last]) / spanN;
        }

        return k + (t - beatTimes[k]) / (beatTimes[k + 1] - beatTimes[k]);
    }

    private static float DistanceToNearest(List<float> sorted, float t)
    {
        int k = sorted.BinarySearch(t);
        if (k >= 0) return 0f;
        k = ~k;

        float best = float.MaxValue;
        if (k < sorted.Count) best = sorted[k] - t;
        if (k > 0) best = Mathf.Min(best, t - sorted[k - 1]);
        return best;
    }

    private static float[] SmoothAndNormalize(float[] values, int window)
    {
        int n = values.Length;
        float[] result = new float[n];
        if (n == 0) return result;

        double[] prefix = new double[n + 1];
        for (int k = 0; k < n; k++) prefix[k + 1] = prefix[k] + values[k];

        int half = window / 2;
        for (int k = 0; k < n; k++)
        {
            int from = Mathf.Max(0, k - half);
            int to = Mathf.Min(n, k + half + 1);
            result[k] = (float)((prefix[to] - prefix[from]) / (to - from));
        }

        float[] sorted = (float[])result.Clone();
        System.Array.Sort(sorted);
        float p95 = sorted[Mathf.Clamp(Mathf.FloorToInt(n * 0.95f), 0, n - 1)];
        if (p95 > 0f)
            for (int k = 0; k < n; k++)
                result[k] = Mathf.Clamp01(result[k] / p95);

        return result;
    }

    private string BuildStats(List<BeatPoint> beats, float minDistance, float bpm, bool gridUsed, float songLength)
    {
        if (beats.Count == 0) return "Nenhum cubo gerado.";

        int breaths = 0;
        float longest = 0f;
        float breathGap = minDistance * breathGapMultiplier;
        for (int k = 1; k < beats.Count; k++)
        {
            float gap = beats[k].time - beats[k - 1].time;
            if (gap >= breathGap) breaths++;
            longest = Mathf.Max(longest, gap);
        }

        float perMinute = songLength > 0f ? beats.Count / (songLength / 60f) : 0f;
        string bpmText = bpm > 0f ? $"{bpm:F1} BPM{(gridUsed ? " (somente no beat)" : " (preferência)")}" : "sem BPM";

        return $"{bpmText}   |   {perMinute:F0} cubos/min   |   {breaths} respiros   |   maior gap {longest:F2}s";
    }

    private float[] DownmixToMono(AudioClip targetClip)
    {
        float[] raw = new float[targetClip.samples * targetClip.channels];
        targetClip.GetData(raw, 0);

        if (targetClip.channels == 1) return raw;

        float[] mono = new float[targetClip.samples];
        int channels = targetClip.channels;
        for (int s = 0; s < targetClip.samples; s++)
        {
            float sum = 0f;
            int baseIdx = s * channels;
            for (int c = 0; c < channels; c++)
                sum += raw[baseIdx + c];
            mono[s] = sum / channels;
        }
        return mono;
    }

    private static float[] BuildHannWindow(int size)
    {
        float[] w = new float[size];
        for (int i = 0; i < size; i++)
            w[i] = 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * i / (size - 1)));
        return w;
    }

    private static int BinFor(float hz, float freqPerBin, int binCount)
    {
        return Mathf.Clamp(Mathf.RoundToInt(hz / freqPerBin), 1, binCount - 1);
    }

    private float Average(List<float> list)
    {
        if (list.Count == 0) return 0f;
        float sum = 0f;
        foreach (float v in list) sum += v;
        return sum / list.Count;
    }

    private float StdDev(List<float> list, float mean)
    {
        if (list.Count == 0) return 0f;
        float variance = 0f;
        foreach (float v in list)
        {
            float diff = v - mean;
            variance += diff * diff;
        }
        return Mathf.Sqrt(variance / list.Count);
    }
}

// =====================================================================
// FFT simples (Cooley-Tukey, radix-2, in-place). Tamanho precisa ser
// potência de 2 — por isso o dropdown "Tamanho da Janela FFT" só oferece
// 512/1024/2048/4096.
// =====================================================================
public static class SimpleFFT
{
    public static void Forward(float[] real, float[] imag)
    {
        int n = real.Length;

        for (int i = 1, j = 0; i < n; i++)
        {
            int bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1)
                j ^= bit;
            j ^= bit;
            if (i < j)
            {
                (real[i], real[j]) = (real[j], real[i]);
                (imag[i], imag[j]) = (imag[j], imag[i]);
            }
        }

        for (int len = 2; len <= n; len <<= 1)
        {
            float ang = -2f * Mathf.PI / len;
            float wReal = Mathf.Cos(ang);
            float wImag = Mathf.Sin(ang);

            for (int i = 0; i < n; i += len)
            {
                float curReal = 1f, curImag = 0f;
                for (int k = 0; k < len / 2; k++)
                {
                    int a = i + k;
                    int b = i + k + len / 2;

                    float uReal = real[a];
                    float uImag = imag[a];
                    float vReal = real[b] * curReal - imag[b] * curImag;
                    float vImag = real[b] * curImag + imag[b] * curReal;

                    real[a] = uReal + vReal;
                    imag[a] = uImag + vImag;
                    real[b] = uReal - vReal;
                    imag[b] = uImag - vImag;

                    float nextReal = curReal * wReal - curImag * wImag;
                    float nextImag = curReal * wImag + curImag * wReal;
                    curReal = nextReal;
                    curImag = nextImag;
                }
            }
        }
    }
}