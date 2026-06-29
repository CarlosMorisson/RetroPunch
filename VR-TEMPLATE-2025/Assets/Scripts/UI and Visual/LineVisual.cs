using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LineEffectWaveform : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;         
    public int channel = 0;               

    [Header("Line")]
    [Tooltip("Número de pontos do line renderer")]
    public int points = 256;             
    public float lineLength = 40f;          
    public float baseHeight = 0f;           
    public float amplitude = 5f;            
    public float width = 0.08f;             
    public bool useLocalSpace = true;

    [Header("Smoothing")]
    [Range(0f, 1f)] public float smoothIn = 0.6f;   
    [Range(0f, 1f)] public float smoothOut = 0.2f;  

    private LineRenderer lr;
    private float[] audioBuffer; 
    private float[] pointsValues; 
    private float[] smooth;

    private const float AMPLITUDE_VALUE = 50;
    private const float WIDTH_VALUE = 20;

    void Awake()
    {
        if (points < 4) points = 4;

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

        if (audioSource != null)
            audioSource.spatialBlend = 0f; 
    }

    private void Start()
    {
        audioSource = SongController.Instance.audioSource;
        amplitude = AMPLITUDE_VALUE;
        width = WIDTH_VALUE;
    }
    void Update()
    {
        if (audioSource == null) return;
        if (!audioSource.isPlaying) return;

        audioSource.GetOutputData(audioBuffer, channel);
        Color startColor = ColorController.Instance.CurrentPrimary;
        Color endColor = ColorController.Instance.CurrentSecondary;

        // Força o Alpha a ser 1f (100% opaco)
        startColor.a = 1f;
        endColor.a = 1f;

        // Aplica no Line Renderer
        lr.startColor = startColor;
        lr.endColor = endColor;

        int bufferLen = audioBuffer.Length;

        for (int i = 0; i < points; i++)
        {
            float sample = audioBuffer[i];

            pointsValues[i] = sample;
        }

        for (int i = 0; i < points; i++)
        {
            float target = pointsValues[i] * amplitude;

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
}
