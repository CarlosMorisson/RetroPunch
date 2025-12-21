using UnityEngine;
using System.Collections;

public class SongController : MonoBehaviour
{
    public static SongController Instance;

    [Header("Song Data (Auto)")]
    public string songName;
    public float songDuration;

    [Header("Config")]
    public AudioClip songClip;
    public int spectrumSize = 512;

    [Header("Impact FX")]
    [Range(0f, 1f)] public float maxVolumeBoostPercent = 0.25f; // +25%
    [Range(0f, 0.2f)] public float maxPitchBoost = 0.05f;       // pitch leve

    public AudioSource audioSource { get; private set; }

    private float[] spectrumData;
    private Coroutine impactRoutine;

    private float baseVolume;
    private float basePitch;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (audioSource != null)
            UIResult.Instance.UpdateSlider(audioSource.time);
    }

    /// <summary>
    /// Carrega o AudioClip, atualiza nome/duração e toca.
    /// </summary>
    public void LoadSong(AudioClip clip)
    {
        spectrumData = new float[spectrumSize];

        songClip = clip;
        songName = clip.name;
        songDuration = clip.length;

        audioSource = AudioController.Instance.PlayMusic(songName);

        UIResult.Instance.UpdateMusicName(songName);
        UIResult.Instance.UpdateMusicLenght(songDuration);

        audioSource.clip = songClip;
        audioSource.Play();

        baseVolume = audioSource.volume;
        basePitch = audioSource.pitch;
    }


    public void ImpactBoost(float intensity, float duration)
    {
        if (audioSource == null || !audioSource.isPlaying)
            return;

        intensity = Mathf.Clamp01(intensity);

        if (impactRoutine != null)
            StopCoroutine(impactRoutine);

        impactRoutine = StartCoroutine(ImpactRoutine(intensity, duration));
    }

    private IEnumerator ImpactRoutine(float intensity, float duration)
    {
        float targetVolume = baseVolume * (1f + maxVolumeBoostPercent * intensity);
        float targetPitch = basePitch + maxPitchBoost * intensity;

        float attackTime = 0.03f;
        float releaseTime = 0.08f;

        float t = 0f;
        while (t < attackTime)
        {
            t += Time.deltaTime;
            float lerp = t / attackTime;

            audioSource.volume = Mathf.Lerp(baseVolume, targetVolume, lerp);
            audioSource.pitch = Mathf.Lerp(basePitch, targetPitch, lerp);

            yield return null;
        }

        yield return new WaitForSeconds(duration);

        t = 0f;

        while (t < releaseTime)
        {
            t += Time.deltaTime;
            float lerp = t / releaseTime;

            audioSource.volume = Mathf.Lerp(targetVolume, baseVolume, lerp);
            audioSource.pitch = Mathf.Lerp(targetPitch, basePitch, lerp);

            yield return null;
        }

        audioSource.volume = baseVolume;
        audioSource.pitch = basePitch;

        impactRoutine = null;
    }

    public float GetFrequency()
    {
        if (!audioSource.isPlaying) return 0f;

        audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);

        float sum = 0f;
        for (int i = 0; i < spectrumData.Length; i++)
            sum += spectrumData[i];

        return sum * 10f;
    }

    public float GetPeakFrequency()
    {
        audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);

        float maxValue = 0f;
        foreach (var v in spectrumData)
            if (v > maxValue) maxValue = v;

        return maxValue;
    }
}
