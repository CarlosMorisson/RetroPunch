using UnityEngine;

public class ProgressEffectVisual : MonoBehaviour
{
    public static ProgressEffectVisual Instance;

    [Header("Audio Impact")]
    [SerializeField] private float initialIntensity = 0.3f;
    [SerializeField] private float initialDuration = 0.08f;
    [SerializeField] private float intensityStep = 0.05f;
    [SerializeField] private float durationStep = 0.01f;
    [SerializeField] private float maxIntensity = 1f;
    [SerializeField] private float maxDuration = 0.2f;

    [Header("Particles")]
    [SerializeField] private ParticleSystem impactParticles;
    [SerializeField] private float initialEmission = 100f;
    [SerializeField] private float maxEmission = 500f;
    [SerializeField] private float emissionStep = 25f;


    [Header("Runtime (Debug)")]
    [SerializeField] private float currentIntensity;
    [SerializeField] private float currentDuration;
    [SerializeField] private float currentEmission;

    private ParticleSystem.EmissionModule emissionModule;
    private void Awake() => Instance = this;

    private void Start()
    {
        if (impactParticles != null)
        {
            emissionModule = impactParticles.emission;
            emissionModule.rateOverTime = initialEmission;
        }

        ResetValues();
        LoaderController.Instance.GetPrimaryMaterial(impactParticles.GetComponent<ParticleSystemRenderer>().material);
    }

    public void Success()
    {
        currentIntensity = Mathf.Min(currentIntensity + intensityStep, maxIntensity);
        currentDuration = Mathf.Min(currentDuration + durationStep, maxDuration);
        SongController.Instance.ImpactBoost(currentIntensity, currentDuration);

        currentEmission = Mathf.Min(currentEmission + emissionStep, maxEmission);
        emissionModule.rateOverTime = currentEmission;

        impactParticles.Play();
    }

    public void Error()
    {
        ResetValues();
    }
    private void ResetValues()
    {
        currentIntensity = initialIntensity;
        currentDuration = initialDuration;

        currentEmission = initialEmission;
    }
}
