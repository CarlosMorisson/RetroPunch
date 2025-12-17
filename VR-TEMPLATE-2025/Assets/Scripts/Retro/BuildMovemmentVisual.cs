using System.Collections.Generic;
using UnityEngine;

public class BuildMovemmentVisual : MonoBehaviour
{
    public List<Transform> RightList = new();
    public List<Transform> LeftList = new();

    [Header("Audio Source")]
    public AudioSource audioSource;
    public int spectrumSize = 128; 

    [Header("Audio Reaction")]
    public float amplitude = 30f;
    public float smoothSpeed = 10f;
    public float scaleMultiplier;

    private float[] spectrum;
    private Dictionary<Transform, float> baseY = new();
    private Dictionary<Transform, float> smoothValues = new();

    void Start()
    {
        spectrum = new float[spectrumSize];
    }

    void Update()
    {
        if (audioSource == null) return;

        audioSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        AutoRegister(RightList);
        AutoRegister(LeftList);

        AnimateList(RightList);
        AnimateList(LeftList);
    }

    void AutoRegister(List<Transform> list)
    {
        foreach (Transform t in list)
        {
            if (!baseY.ContainsKey(t))
                baseY[t] = t.localScale.y;

            if (!smoothValues.ContainsKey(t))
                smoothValues[t] = 0f;
        }
    }

    void AnimateList(List<Transform> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int index = Mathf.Clamp(Mathf.FloorToInt((float)i / list.Count * spectrumSize), 0, spectrumSize - 1);

            float rawValue = spectrum[index] * amplitude;

            rawValue = Mathf.Clamp(rawValue, 0f, 10f); 

            smoothValues[list[i]] = Mathf.Lerp(smoothValues[list[i]], rawValue, Time.deltaTime * smoothSpeed);

            float targetY = baseY[list[i]] + smoothValues[list[i]] * scaleMultiplier;

            Vector3 s = list[i].localScale;
            s.y = targetY;
            list[i].localScale = s;
        }
    }
}
