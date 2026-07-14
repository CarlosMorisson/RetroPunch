using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

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
    private AudioClip clip;

    private BeatMapDifficulty difficulty = BeatMapDifficulty.Medium;

    private int sampleWindow = 1024;

    private Vector2 scrollPos;
    private List<BeatPoint> previewBeats = new();
    private bool hasPreview = false;
    private float warmupThresholdMultiplier = 2.0f;
    private float warmupDuration = 5f;
    private float warmupMinDistanceMultiplier = 3f;

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

    [MenuItem("Tools/Generate Beat Map")]
    public static void Open() => GetWindow<BeatMapGenerator>("Beat Map Generator");

    private void OnGUI()
    {
        warmupDuration = EditorGUILayout.Slider(
    "Warmup Duration (s)", warmupDuration, 1f, 10f);

        warmupMinDistanceMultiplier = EditorGUILayout.Slider(
    "Warmup Min Distance Mult", warmupMinDistanceMultiplier, 1f, 15f);

        warmupThresholdMultiplier = EditorGUILayout.Slider(
            "Warmup Threshold Mult", warmupThresholdMultiplier, 1.2f, 4f);
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

        GUILayout.Space(4);
        sampleWindow = EditorGUILayout.IntSlider("Sample Window", sampleWindow, 512, 4096);

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
    }
    private void Preview()
    {
        if (clip == null)
        {
            Debug.LogError("Selecione um AudioClip antes de gerar o preview.");
            return;
        }

        previewBeats = DetectBeats();
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

    private List<BeatPoint> DetectBeats()
    {
        int idx = (int)difficulty;
        float threshold = ThresholdByDifficulty[idx];
        float minDistance = MinDistanceByDifficulty[idx];
        int historySize = HistorySizeByDifficulty[idx];

        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        List<float> energyHistory = new();
        int totalWindows = samples.Length / sampleWindow;
        int midStart = totalWindows / 4;      
        int midEnd = totalWindows / 2;      

        for (int w = midStart; w < midEnd && energyHistory.Count < historySize; w++)
        {
            int s = w * sampleWindow;
            if (s + sampleWindow >= samples.Length) break;
            energyHistory.Add(ComputeEnergy(samples, s, sampleWindow));
        }

        List<BeatPoint> beats = new();
        float lastBeatTime = -minDistance * warmupMinDistanceMultiplier;
        float maxEnergy = 0f;
        int windowIndex = 0;

        for (int i = 0; i < samples.Length - sampleWindow; i += sampleWindow)
        {
            float energy = ComputeEnergy(samples, i, sampleWindow);
            maxEnergy = Mathf.Max(maxEnergy, energy);

            float avgEnergy = Average(energyHistory);
            float stdDev = StdDev(energyHistory, avgEnergy);

            float time = (float)i / (clip.frequency * clip.channels);
            bool isWarmup = time < warmupDuration;
            float activeThreshold = isWarmup
                ? threshold * warmupThresholdMultiplier
                : threshold;

            float dynamicThreshold = avgEnergy + stdDev * activeThreshold;

            energyHistory.Add(energy);
            if (energyHistory.Count > historySize)
                energyHistory.RemoveAt(0);

            if (avgEnergy <= 0f) continue;
            if (energy <= dynamicThreshold) continue;
            float activeMinDistance = time < warmupDuration
                ? Mathf.Lerp(minDistance * warmupMinDistanceMultiplier, minDistance, time / warmupDuration)
                : minDistance;

            if (time - lastBeatTime < activeMinDistance) continue;

            lastBeatTime = time;
            beats.Add(new BeatPoint
            {
                time = time,
                intensity = energy,
                affinity = GuessAffinity(samples, i, sampleWindow)
            });

            windowIndex++;
        }

        NormalizeIntensity(beats, maxEnergy);
        return beats;
    }

    private float ComputeGlobalAverage(float[] samples)
    {
        // Pega apenas os primeiros 5 segundos para calibrar
        int totalSamples5s = Mathf.Min(
            clip.frequency * clip.channels * 5,
            samples.Length
        );

        int totalWindows = totalSamples5s / sampleWindow;
        float sum = 0f;
        int count = 0;

        for (int i = 0; i < totalWindows; i++)
        {
            sum += ComputeEnergy(samples, i * sampleWindow, sampleWindow);
            count++;
        }

        return count > 0 ? sum / count : 0f;
    }
    private float ComputeEnergy(float[] samples, int start, int window)
    {
        float e = 0f;
        for (int j = 0; j < window; j++)
        {
            float s = samples[start + j];
            e += s * s;
        }
        return e / window;
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

    private void NormalizeIntensity(List<BeatPoint> beats, float maxEnergy)
    {
        if (maxEnergy <= 0f) return;
        foreach (var beat in beats)
            beat.intensity = Mathf.Clamp01(beat.intensity / maxEnergy);
    }

    private MusicAffinity GuessAffinity(float[] samples, int start, int window)
    {

        float bass = 0f, mid = 0f, treble = 0f;
        int third = window / 3;

        for (int i = 0; i < third; i++) bass += Mathf.Abs(samples[start + i]);
        for (int i = third; i < third * 2; i++) mid += Mathf.Abs(samples[start + i]);
        for (int i = third * 2; i < window; i++) treble += Mathf.Abs(samples[start + i]);

        if (bass > mid && bass > treble) return MusicAffinity.Bass;
        if (mid > bass && mid > treble) return MusicAffinity.Mid;
        return MusicAffinity.Treble;
    }
}