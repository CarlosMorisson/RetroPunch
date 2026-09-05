using UnityEngine;

/// <summary>
/// Animates this object's local scale based on the energy of a
/// selected frequency band, using SongController's existing analysis.
/// </summary>
public class AudioSpectrumScaler : MonoBehaviour
{
    public enum FrequencyBand
    {
        Main,
        Bass,
        Treble
    }

    [Header("Frequency Band")]
    [SerializeField] private FrequencyBand frequencyBand = FrequencyBand.Main;

    [Header("Scale Animation")]
    [Tooltip("0.5 means the object can grow up to 1.5x its original scale.")]
    [SerializeField] private float maxScaleMultiplier = 0.5f;
    [SerializeField] private float intensityMultiplier = 1f;
    [SerializeField] private float smoothingSpeed = 10f;

    private Vector3 initialScale;
    private float currentScaleFactor = 1f;

    private void Awake()
    {
        initialScale = transform.localScale;
    }

    private void Update()
    {
        float rawBandEnergy = GetSelectedBandEnergy();
        float scaledEnergy = ApplyIntensityMultiplier(rawBandEnergy);

        UpdateScale(scaledEnergy);
    }

    // Pulls the already-computed band energy from SongController.GetAnalysis().
    private float GetSelectedBandEnergy()
    {
        if (SongController.Instance == null)
        {
            return 0f;
        }

        MusicAnalysis analysis = SongController.Instance.GetAnalysis();
        if (analysis == null)
        {
            return 0f;
        }

        switch (frequencyBand)
        {
            case FrequencyBand.Bass:
                return analysis.Bass;

            case FrequencyBand.Treble:
                return analysis.Treble;

            case FrequencyBand.Main:
            default:
                return analysis.Intensity;
        }
    }

    // Extra multiplier applied to the raw band energy before it drives the scale.
    private float ApplyIntensityMultiplier(float rawValue)
    {
        return rawValue * intensityMultiplier;
    }

    // Smoothly interpolates towards the target scale and applies it uniformly to X, Y and Z.
    private void UpdateScale(float bandEnergy)
    {
        float targetScaleFactor = 1f + Mathf.Clamp01(bandEnergy) * maxScaleMultiplier;

        currentScaleFactor = Mathf.Lerp(
            currentScaleFactor,
            targetScaleFactor,
            Time.deltaTime * smoothingSpeed
        );

        transform.localScale = initialScale * currentScaleFactor;
    }

    public void SetFrequencyBand(FrequencyBand band) => frequencyBand = band;

    public void SetIntensityMultiplier(float value) => intensityMultiplier = value;

    public void SetMaxScaleMultiplier(float value) => maxScaleMultiplier = value;
}