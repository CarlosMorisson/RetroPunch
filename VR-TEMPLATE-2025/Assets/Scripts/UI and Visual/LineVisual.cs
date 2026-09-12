using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(LineRenderer))]
public class LineEffectWaveform : MonoBehaviour
{
    public static LineEffectWaveform Instance;

    [Header("Audio")]
    public SongController audioSource;
    public int channel = 0;
    public FFTWindow fftWindow = FFTWindow.BlackmanHarris;
    [Tooltip("Compress�o aplicada na magnitude do espectro (0.5 = raiz quadrada) para nivelar graves x agudos, como um equalizador de app de m�sica.")]
    [Range(0.1f, 1f)] public float spectrumCompression = 0.5f;

    [Header("Line")]
    [Tooltip("N�mero de pontos do line renderer. Precisa ser pot�ncia de 2 (64-8192) pois tamb�m � o tamanho da FFT.")]
    public int points = 256;
    public float lineLength = 40f;
    public float baseHeight = 0f;
    public float amplitude = 5f;
    public float width = 0.08f;
    public bool useLocalSpace = true;

    [Header("Smoothing")]
    [Range(0f, 1f)] public float smoothIn = 0.6f;
    [Range(0f, 1f)] public float smoothOut = 0.2f;

    [Header("Reaction (Hit Feedback)")]
    [Tooltip("Multiplicador atual da reatividade da linha. Sobe a cada acerto e cai a cada erro.")]
    public float reactionMultiplier = 1f;
    public float minReactionMultiplier = 1f;
    public float maxReactionMultiplier = 3f;
    public float successMultiplierStep = 0.2f;
    public float failMultiplierStep = 0.5f;

    [Header("Punch Feedback")]
    public float punchUpAmount = 8f;
    public float punchDownAmount = 8f;
    public float punchDuration = 0.4f;
    public float punchElasticAmplitude = 1f;
    public float punchElasticPeriod = 0.3f;

    private float punchOffset = 0f;
    private Tween punchTween;

    private LineRenderer lr;
    private float[] audioBuffer;
    private float[] pointsValues;
    private float[] smooth;

    private const float AMPLITUDE_VALUE = 50;
    private const float WIDTH_VALUE = 20;

    void Awake()
    {
        Instance = this;

        points = ClampToFFTSize(points);

        lr = GetComponent<LineRenderer>();
        lr.positionCount = points;
        lr.widthMultiplier = width;
        lr.useWorldSpace = !useLocalSpace;

        audioBuffer = new float[points];
        pointsValues = new float[points];
        smooth = new float[points];

        for (int i = 0; i < points; i++)
        {
            float tx = (float)i / (points - 1);
            float x = Mathf.Lerp(-lineLength * 0.5f, lineLength * 0.5f, tx);
            lr.SetPosition(i, useLocalSpace ? new Vector3(x, baseHeight, 0) : transform.TransformPoint(new Vector3(x, baseHeight, 0)));
        }

        if (audioSource != null && audioSource.audioSource != null)
            audioSource.audioSource.spatialBlend = 0f;
    }

    private void Start()
    {
        amplitude = AMPLITUDE_VALUE;
        width = WIDTH_VALUE;
    }
    void Update()
    {
        if (audioSource == null)
            audioSource = SongController.Instance;

        if (audioSource == null || audioSource.audioSource == null) return;
        if (!audioSource.audioSource.isPlaying) return;

        audioSource.audioSource.spatialBlend = 0f;
        audioSource.audioSource.GetSpectrumData(audioBuffer, channel, fftWindow);
        Color startColor = ColorController.Instance.CurrentPrimary;
        Color endColor = ColorController.Instance.CurrentSecondary;

        // For�a o Alpha a ser 1f (100% opaco)
        startColor.a = 1f;
        endColor.a = 1f;

        // Aplica no Line Renderer
        lr.startColor = startColor;
        lr.endColor = endColor;

        for (int i = 0; i < points; i++)
        {
            float magnitude = audioBuffer[i];

            pointsValues[i] = Mathf.Pow(magnitude, spectrumCompression);
        }

        for (int i = 0; i < points; i++)
        {
            float target = pointsValues[i] * amplitude * reactionMultiplier + punchOffset;

            if (Mathf.Abs(target) > Mathf.Abs(smooth[i]))
            {
                // subir
                smooth[i] = Mathf.Lerp(smooth[i], target, 1f - smoothIn); 
            }
            else
            {
                smooth[i] = Mathf.Lerp(smooth[i], target, 1f - smoothOut);
            }
        }

        for (int i = 0; i < points; i++)
        {
            float t = (float)i / (points - 1);
            float x = Mathf.Lerp(-lineLength * 0.5f, lineLength * 0.5f, t);

            float y = baseHeight + smooth[i];
            if (i == 0 || i == points - 1) y = baseHeight;

            Vector3 pos = new Vector3(x, y, 0f);
            lr.SetPosition(i, useLocalSpace ? pos : transform.TransformPoint(pos));
        }
    }

    #region Hit Feedback

    /// <summary>
    /// Chamar no evento de SUCESSO. Aumenta o multiplicador de reatividade da linha
    /// (permanece elevado) e da um "punch" para cima como resposta imediata ao acerto.
    /// </summary>
    public void TriggerSuccessReaction()
    {
        reactionMultiplier = Mathf.Min(reactionMultiplier + successMultiplierStep, maxReactionMultiplier);
        Punch(punchUpAmount);
    }

    /// <summary>
    /// Chamar no evento de FALHA. Reduz o multiplicador de reatividade da linha
    /// e da um "punch" para baixo como resposta imediata ao erro.
    /// </summary>
    public void TriggerFailReaction()
    {
        reactionMultiplier = Mathf.Max(reactionMultiplier - failMultiplierStep, minReactionMultiplier);
        Punch(-punchDownAmount);
    }

    private void Punch(float amount)
    {
        punchTween?.Kill();
        punchOffset = amount;
        punchTween = DOTween.To(() => punchOffset, x => punchOffset = x, 0f, punchDuration)
            .SetEase(Ease.OutElastic, punchElasticAmplitude, punchElasticPeriod);
    }

    #endregion

    /// <summary>
    /// GetSpectrumData exige um tamanho que seja pot�ncia de 2 entre 64 e 8192.
    /// </summary>
    private static int ClampToFFTSize(int value)
    {
        int size = 64;
        while (size < value && size < 8192)
            size <<= 1;
        return size;
    }
}
