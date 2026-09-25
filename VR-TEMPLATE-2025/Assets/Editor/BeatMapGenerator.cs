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

            scrollPos = EditorGUILayout.BeginScrollView(
                scrollPos, GUILayout.Height(200));

            foreach (var b in previewBeats)
            {
                EditorGUILayout.LabelField(
                    $"t={b.time:F3}s   intensity={b.intensity:F2}   affinity={b.affinity}");
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
                }

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

        return final;
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