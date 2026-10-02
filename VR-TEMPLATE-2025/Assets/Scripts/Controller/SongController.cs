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
    [Range(1f, 3f)] public float maxSequencePitch = 2.0f; // Limite m�ximo do pitch
    [Range(0f, 0.5f)] public float pitchIncreaseStep = 0.05f;
    [Header("Pitch Down Stop")]
    [Tooltip("Tempo (em segundos) que o pitch leva pra descer até 0 antes da música parar.")]
    [Min(0f)] public float pitchDownDuration = 2f;
    [Header("Victory")]
    public List<AudioClip> VictoryAudio;

    private List<AudioClip> playedTouchAudios = new List<AudioClip>();
    private List<AudioClip> playedFailAudio = new List<AudioClip>();

    private List<AudioClip> shuffledSequence = new List<AudioClip>();
    private int currentSequenceIndex = 0;

    public AudioSource audioSource { get; set; }

    public bool LoadMusic=false;

    private float[] spectrumData;
    private Coroutine impactRoutine;
    private Coroutine pitchDownRoutine;
    private bool isStopped = false;

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
            // Depois do StopSong o source fica parado com time 0, então não pode contar como fim da música
            if (!isStopped && !audioSource.isPlaying && audioSource.time == 0)
            {
                FinishSong();
            }
        }
    }

    /// <summary>
    /// Carrega o AudioClip, atualiza nome/dura��o e toca. Atua como o Start Game.
    /// </summary>
    public void LoadSong(AudioClip clip)
    {
        if (audioSource != null)
            return;
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
        LoadMusic = true;
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
    [ContextMenu("Pause")]
    public void PauseSong()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Pause();
            isPaused = true;
            Debug.Log("M�sica Pausada");
        }
    }
    [ContextMenu("Despause")]
    public void ResumeSong()
    {
        if (audioSource != null && isPaused)
        {
            audioSource.UnPause();
            isPaused = false;
            Debug.Log("M�sica Retomada");
        }
    }

    private void FinishSong()
    {
        isPaused = true;
        GameState.Instance.GameStateEnd();
    }

    /// <summary>
    /// Diminui o pitch da música gradativamente até 0 durante pitchDownDuration e depois chama StopSong.
    /// </summary>
    [ContextMenu("Pitch Down e Stop")]
    public void PitchDownAndStop()
    {
        if (audioSource == null || isStopped)
            return;

        if (impactRoutine != null)
        {
            StopCoroutine(impactRoutine);
            impactRoutine = null;
        }

        if (pitchDownRoutine != null)
            StopCoroutine(pitchDownRoutine);

        pitchDownRoutine = StartCoroutine(PitchDownRoutine());
    }

    private IEnumerator PitchDownRoutine()
    {
        float startPitch = audioSource.pitch;
        float t = 0f;

        // Tempo sem escala pra não ser afetado pelo slow motion do FreezeEffect
        while (t < pitchDownDuration)
        {
            t += Time.unscaledDeltaTime;
            audioSource.pitch = Mathf.Lerp(startPitch, 0f, t / pitchDownDuration);
            yield return null;
        }

        audioSource.pitch = 0f;
        pitchDownRoutine = null;
        StopSong();
    }

    [ContextMenu("Stop Song")]
    public void StopSong()
    {
        if (audioSource == null)
            return;

        if (pitchDownRoutine != null)
        {
            StopCoroutine(pitchDownRoutine);
            pitchDownRoutine = null;
        }

        isStopped = true;
        audioSource.Stop();

        // O AudioSource é reaproveitado pelo AudioController, então devolve o pitch original
        audioSource.pitch = basePitch;
    }

    /// <summary>
    /// Troca o clip do audioSource por um aleatório da lista VictoryAudio e dá play.
    /// </summary>
    [ContextMenu("Play Victory")]
    public void PlayVictory()
    {
        PauseSong();
        if (audioSource == null || VictoryAudio == null || VictoryAudio.Count == 0)
            return;

        if (pitchDownRoutine != null)
        {
            StopCoroutine(pitchDownRoutine);
            pitchDownRoutine = null;
        }

        if (impactRoutine != null)
        {
            StopCoroutine(impactRoutine);
            impactRoutine = null;
        }

        // Marca como parada pra quando o clip de vitória acabar o Update não chamar o FinishSong de novo
        isStopped = true;

        audioSource.Stop();
        audioSource.pitch = basePitch;
        audioSource.volume = baseVolume;
        audioSource.clip = VictoryAudio[UnityEngine.Random.Range(0, VictoryAudio.Count)];
        audioSource.Play();
    }

    public void ImpactBoost(float intensity, float duration)
    {
        if (audioSource == null || !audioSource.isPlaying || pitchDownRoutine != null)
            return;

        intensity = Mathf.Clamp01(intensity);

        PlayTouchEffect();
        PlaySequenceSuccessEffect();

        if (impactRoutine != null)
            StopCoroutine(impactRoutine);

        impactRoutine = StartCoroutine(ImpactRoutine(intensity, duration));
    }

    private IEnumerator ImpactRoutine(float intensity, float duration)
    {
        // Queda brusca de volume e pitch � sensa��o de "peso"
        float duckVolume = baseVolume * Mathf.Lerp(0.4f, 0.2f, intensity);
        float duckPitch = basePitch - Mathf.Lerp(0.08f, 0.2f, intensity);

        // Overshoot de pitch no retorno � sensa��o de "ressalto"
        float boostPitch = basePitch + Mathf.Lerp(0.03f, 0.08f, intensity);
        float boostVolume = baseVolume * Mathf.Lerp(1.1f, 1.25f, intensity);

        float punchDown = 0.04f; 
        float hold = duration * 0.1f; 
        float punchUp = 0.06f; 
        float settle = 0.12f; 

        float t = 0f;

        while (t < punchDown)
        {
            t += Time.deltaTime;
            float lerp = t / punchDown;
            audioSource.volume = Mathf.Lerp(baseVolume, duckVolume, lerp);
            audioSource.pitch = Mathf.Lerp(basePitch, duckPitch, lerp);
            yield return null;
        }

        // 2. Hold no fundo
        yield return new WaitForSeconds(hold);

        t = 0f;
        while (t < punchUp)
        {
            t += Time.deltaTime;
            float lerp = t / punchUp;
            audioSource.volume = Mathf.Lerp(duckVolume, boostVolume, lerp);
            audioSource.pitch = Mathf.Lerp(duckPitch, boostPitch, lerp);
            yield return null;
        }

        t = 0f;
        while (t < settle)
        {
            t += Time.deltaTime;
            float lerp = t / settle;
            audioSource.volume = Mathf.Lerp(boostVolume, baseVolume, lerp);
            audioSource.pitch = Mathf.Lerp(boostPitch, basePitch, lerp);
            yield return null;
        }

        audioSource.volume = baseVolume;
        audioSource.pitch = basePitch;
        impactRoutine = null;
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
    /// Toca a pr�xima nota na sequ�ncia sorteada aumentando o Pitch de forma linear at� o limite.
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
    public MusicAnalysis GetAnalysis()
    {
        float[] spectrum = GetGlobalSpectrum();

        if (spectrum == null)
            return null;

        float bass = 0;
        float mid = 0;
        float treble = 0;

        int bassEnd = spectrum.Length / 8;
        int midEnd = spectrum.Length / 2;

        for (int i = 0; i < bassEnd; i++)
            bass += spectrum[i];

        for (int i = bassEnd; i < midEnd; i++)
            mid += spectrum[i];

        for (int i = midEnd; i < spectrum.Length; i++)
            treble += spectrum[i];

        return new MusicAnalysis
        {
            Bass = bass,
            Mid = mid,
            Treble = treble,
            Intensity = bass + mid + treble
        };
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

    /// <summary>
    /// Analisa apenas a música principal (audioSource) em bandas de grave/médio/agudo,
    /// normalizadas pela quantidade de bins de cada banda para terem escalas comparáveis.
    /// </summary>
    public MusicAnalysis GetSongAnalysis()
    {
        if (audioSource == null || !audioSource.isPlaying)
            return null;

        audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);

        int bassEnd = spectrumData.Length / 8;
        int midEnd = spectrumData.Length / 2;

        float bass = 0f, mid = 0f, treble = 0f;

        for (int i = 0; i < bassEnd; i++)
            bass += spectrumData[i];

        for (int i = bassEnd; i < midEnd; i++)
            mid += spectrumData[i];

        for (int i = midEnd; i < spectrumData.Length; i++)
            treble += spectrumData[i];

        bass /= bassEnd;
        mid /= (midEnd - bassEnd);
        treble /= (spectrumData.Length - midEnd);

        return new MusicAnalysis
        {
            Bass = bass,
            Mid = mid,
            Treble = treble,
            Intensity = bass + mid + treble
        };
    }
}

public class MusicAnalysis
{
    public float Bass;
    public float Mid;
    public float Treble;
    public float Intensity;
}