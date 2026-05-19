using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

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
    [Range(0f, 1f)] public float maxVolumeBoostPercent = 0.25f; 
    [Range(-0.2f, 0.2f)] public float maxPitchBoost = 0.05f;      

    [Space(15)]
    [Header("Touch Effect")]
    public List<AudioClip> touchAudio;
    public AudioSource TouchAudioSource;
    [Header("Fail Effect")]
    [Space(15)]
    public List<AudioClip> failAudio;
    public AudioSource FailAudioSource;
    [Header("Sequence Effect")]
    public List<AudioClip> sequenceAudio;
    public AudioSource SequenceAudioSource;
    [Range(1f, 3f)] public float maxSequencePitch = 2.0f; // Limite máximo do pitch
    [Range(0f, 0.5f)] public float pitchIncreaseStep = 0.05f;

    private List<AudioClip> playedTouchAudios = new List<AudioClip>();
    private List<AudioClip> playedFailAudio = new List<AudioClip>();

    private List<AudioClip> shuffledSequence = new List<AudioClip>();
    private int currentSequenceIndex = 0;

    public AudioSource audioSource { get; set; }

    private float[] spectrumData;
    private Coroutine impactRoutine;

    private float baseVolume;
    private float basePitch;
    private float initialSequencePitch;

    private void Awake()
    {
        Instance = this;
    }
    private bool isPaused = false;
    private void Update()
    {
        if (audioSource != null)
        {
            UIResult.Instance.UpdateSlider(audioSource.time);
            if (!audioSource.isPlaying && audioSource.time == 0)
            {
                FinishSong();
            }
        }
    }

    /// <summary>
    /// Carrega o AudioClip, atualiza nome/duração e toca. Atua como o Start Game.
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

        if (SequenceAudioSource != null)
        {
            initialSequencePitch = SequenceAudioSource.pitch;
        }

        InitializeSequenceOrder();
    }

    /// <summary>
    /// Clona a lista original de sequenceAudio e a embaralha aleatoriamente.
    /// </summary>
    private void InitializeSequenceOrder()
    {
        shuffledSequence.Clear();
        currentSequenceIndex = 0;

        if (sequenceAudio == null || sequenceAudio.Count == 0) return;

        shuffledSequence.AddRange(sequenceAudio);

        for (int i = shuffledSequence.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            AudioClip temp = shuffledSequence[i];
            shuffledSequence[i] = shuffledSequence[randomIndex];
            shuffledSequence[randomIndex] = temp;
        }
    }

    public void PauseSong()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Pause();
            isPaused = true;
            Debug.Log("Música Pausada");
        }
    }
    public void ResumeSong()
    {
        if (audioSource != null && isPaused)
        {
            audioSource.UnPause();
            isPaused = false;
            Debug.Log("Música Retomada");
        }
    }

    private void FinishSong()
    {
        isPaused = true;
        GameState.Instance.GameStateEnd();
    }
    public void ImpactBoost(float intensity, float duration)
    {
        if (audioSource == null || !audioSource.isPlaying)
            return;

        intensity = Mathf.Clamp01(intensity);

        PlayTouchEffect();
        PlaySequenceSuccessEffect(); 

        if (impactRoutine != null)
            StopCoroutine(impactRoutine);

        impactRoutine = StartCoroutine(ImpactRoutine(intensity, duration));
    }

    private void PlayTouchEffect()
    {
        if (touchAudio == null || touchAudio.Count == 0 || TouchAudioSource == null) return;
        if (playedTouchAudios.Count >= touchAudio.Count)
        {
            playedTouchAudios.Clear();
        }

        List<AudioClip> availableAudios = new List<AudioClip>();
        foreach (var clip in touchAudio)
        {
            if (!playedTouchAudios.Contains(clip))
            {
                availableAudios.Add(clip);
            }
        }
        if (availableAudios.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, availableAudios.Count);
            AudioClip selectedClip = availableAudios[randomIndex];

            playedTouchAudios.Add(selectedClip);
            TouchAudioSource.PlayOneShot(selectedClip);
        }
    }

    /// <summary>
    /// Toca a próxima nota na sequência sorteada aumentando o Pitch de forma linear até o limite.
    /// </summary>
    private void PlaySequenceSuccessEffect()
    {
        if (shuffledSequence == null || shuffledSequence.Count == 0 || SequenceAudioSource == null) return;

        AudioClip clipToPlay = shuffledSequence[currentSequenceIndex];
        SequenceAudioSource.PlayOneShot(clipToPlay);

        currentSequenceIndex++;
        SequenceAudioSource.pitch = Mathf.Min(SequenceAudioSource.pitch + pitchIncreaseStep, maxSequencePitch);

        if (currentSequenceIndex >= shuffledSequence.Count)
        {
            currentSequenceIndex = 0;
        }
    }

    public void PlayFailEffect()
    {
        currentSequenceIndex = 0;

        if (SequenceAudioSource != null)
        {
            SequenceAudioSource.pitch = initialSequencePitch;
        }

        if (failAudio == null || failAudio.Count == 0 || FailAudioSource == null) return;

        if (playedFailAudio.Count >= failAudio.Count)
        {
            playedFailAudio.Clear();
        }

        List<AudioClip> availableAudios = new List<AudioClip>();
        foreach (var clip in failAudio)
        {
            if (!playedFailAudio.Contains(clip))
            {
                availableAudios.Add(clip);
            }
        }
        if (availableAudios.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, availableAudios.Count);
            AudioClip selectedClip = availableAudios[randomIndex];

            playedFailAudio.Add(selectedClip);
            FailAudioSource.PlayOneShot(selectedClip);
        }
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

    public float GetGlobalFrequency()
    {
        var sources = AudioController.Instance.GetPlayingSources();
        if (sources.Count == 0)
            return 0f;

        if (spectrumData == null || spectrumData.Length != spectrumSize)
            spectrumData = new float[spectrumSize];

        float totalEnergy = 0f;

        foreach (var source in sources)
        {
            source.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);

            float sum = 0f;
            for (int i = 0; i < spectrumData.Length; i++)
                sum += spectrumData[i];

            totalEnergy += sum * source.volume;
        }

        return totalEnergy;
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
    public float GetGlobalFrequencyMultiplicative()
    {
        var sources = AudioController.Instance.GetPlayingSources();
        if (sources.Count == 0)
            return 0f;

        float result = 1f;

        foreach (var source in sources)
        {
            source.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);

            float peak = 0f;
            foreach (var v in spectrumData)
                if (v > peak) peak = v;

            result *= Mathf.Clamp(peak * 10f, 0.8f, 1.5f);
        }

        return result;
    }
    public float[] GetGlobalSpectrum()
    {
        var sources = AudioController.Instance.GetPlayingSources();
        if (sources.Count == 0)
            return null;

        if (spectrumData == null || spectrumData.Length != spectrumSize)
            spectrumData = new float[spectrumSize];

        System.Array.Clear(spectrumData, 0, spectrumData.Length);

        foreach (var source in sources)
        {
            float[] temp = new float[spectrumSize];
            source.GetSpectrumData(temp, 0, FFTWindow.BlackmanHarris);

            for (int i = 0; i < spectrumSize; i++)
                spectrumData[i] += temp[i] * source.volume;
        }

        return spectrumData;
    }

    public float GetPeakFrequency()
    {
        if (audioSource == null)
            return 0f;
        audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);

        float maxValue = 0f;
        foreach (var v in spectrumData)
            if (v > maxValue) maxValue = v;

        return maxValue;
    }
}