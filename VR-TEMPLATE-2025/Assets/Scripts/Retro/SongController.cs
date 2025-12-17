using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SongController : MonoBehaviour
{
    public static SongController Instance;
    [Header("Song Data (Auto)")]
    public string songName;
    public float songDuration;

    [Header("Config")]
    public AudioClip songClip;
    public int spectrumSize = 512; // resolução da FFT (pode ser 128, 256, 512, 1024)

    private AudioSource audioSource;
    private float[] spectrumData;
    private void Awake() => Instance = this;
    private void Update() => UIResult.Instance.UpdateSlider(audioSource.time);

    /// <summary>
    /// Carrega o AudioClip, atualiza nome/duração e toca.
    /// </summary>
    public void LoadSong(AudioClip clip)
    {
        audioSource = GetComponent<AudioSource>();
        spectrumData = new float[spectrumSize];
        songClip = clip;
        songName = clip.name;
        songDuration = clip.length;
        UIResult.Instance.UpdateMusicName(songName);
        UIResult.Instance.UpdateMusicLenght(songDuration);


        audioSource.clip = songClip;
        audioSource.Play();
    }

    /// <summary>
    /// Retorna o valor da frequência dominante (intensidade) no momento.
    /// Usado para responsividade visual/gameplay.
    /// </summary>
    public float GetFrequency()
    {
        if (!audioSource.isPlaying) return 0f;

        // Coleta o espectro atual
        audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);

        float sum = 0f;
        for (int i = 0; i < spectrumData.Length; i++)
        {
            sum += spectrumData[i];
        }

        // Normaliza num float conveniente
        return sum * 10f;
    }

    /// <summary>
    /// Retorna a frequência mais forte (pico), caso precise disso também.
    /// </summary>
    public float GetPeakFrequency()
    {
        audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);

        float maxValue = 0f;
        foreach (var v in spectrumData)
        {
            if (v > maxValue) maxValue = v;
        }

        return maxValue;
    }
}
