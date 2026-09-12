using UnityEngine;

/// <summary>
/// Pulses this object's local scale on the beat, using SongController's existing analysis.
/// Auto-switches between Bass/Main/Treble to always watch whichever band is currently the
/// most responsive, then pops the scale the instant that band peaks (the "tum"/"bum"),
/// relaxing back down until the next hit - instead of following the energy continuously.
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
    [Tooltip("Banda usada quando Auto Switch Band estiver desligado.")]
    [SerializeField] private FrequencyBand frequencyBand = FrequencyBand.Main;

    [Header("Auto Band Switching")]
    [Tooltip("Quando ativo, ignora o campo acima e escolhe a cada momento a banda (Bass/Main/Treble) que estiver reagindo mais, para nunca ficar 'estatico' quando a batida forte cai numa banda diferente da escolhida.")]
    [SerializeField] private bool autoSwitchBand = true;
    [Tooltip("Tempo minimo (segundos) que uma banda fica ativa antes de poder trocar de novo. Evita alternancia nervosa.")]
    [SerializeField] private float minBandHoldTime = 0.15f;
    [Tooltip("Quanto a nova banda precisa superar a atual (em %) para haver troca. 1.15 = precisa estar 15% mais responsiva.")]
    [Range(1f, 2f)]
    [SerializeField] private float switchHysteresis = 1.15f;
    [Tooltip("Variacao minima (0-1) no valor normalizado da banda ativa, entre um frame e outro, para ser considerada 'em movimento'.")]
    [SerializeField] private float staticDeltaThreshold = 0.01f;
    [Tooltip("Tempo (segundos) que a banda ativa pode ficar praticamente parada antes de ser forcada a trocar para a banda mais responsiva no momento, mesmo sem superar a histerese.")]
    [SerializeField] private float maxStaticTime = 1f;

    [Header("Band Normalization")]
    [Tooltip("Velocidade de decaimento do pico adaptativo de cada banda (tipo AGC). Maior = o pico esquece mais rapido os picos antigos, mantendo sensibilidade a novas batidas.")]
    [SerializeField] private float peakDecaySpeed = 1.5f;
    [Tooltip("Suavizacao usada para medir a 'atividade' (variacao) recente de cada banda.")]
    [SerializeField] private float activitySmoothing = 8f;

    [Header("Peak Pulse")]
    [Tooltip("Energia normalizada minima (0-1) da banda ativa para um pico ser considerado valido (o 'tum'/'bum' da batida).")]
    [Range(0f, 1f)]
    [SerializeField] private float peakThreshold = 0.4f;
    [Tooltip("Tempo minimo (segundos) entre pulsos, para nao disparar 2x na mesma batida.")]
    [SerializeField] private float minTimeBetweenPeaks = 0.1f;
    [Tooltip("Velocidade (por segundo) com que o pulso da escala relaxa de volta ao normal entre um pico e outro.")]
    [SerializeField] private float pulseDecaySpeed = 3f;

    [Header("Scale Animation")]
    [Tooltip("0.5 means the object can grow up to 1.5x its original scale.")]
    [SerializeField] private float maxScaleMultiplier = 0.5f;
    [SerializeField] private float intensityMultiplier = 1f;
    [SerializeField] private float smoothingSpeed = 10f;

    private class BandState
    {
        public float peak = 0.0001f;
        public float previousNormalized;
        public float activity;
        public float lastDelta;
        public bool wasRising;
        public bool isPeakNow;
        public float peakNormalized;
    }

    private readonly BandState bassState = new();
    private readonly BandState mainState = new();
    private readonly BandState trebleState = new();

    private Vector3 initialScale;
    private float currentScaleFactor = 1f;

    private FrequencyBand activeBand;
    private float bandHoldTimer;
    private float staticTimer;

    private float pulseStrength;
    private float peakCooldownTimer;

    private void Awake()
    {
        initialScale = transform.localScale;
        activeBand = frequencyBand;
    }

    private void Update()
    {
        MusicAnalysis analysis = SongController.Instance != null ? SongController.Instance.GetAnalysis() : null;

        NormalizeBand(bassState, analysis != null ? analysis.Bass : 0f);
        NormalizeBand(mainState, analysis != null ? analysis.Intensity : 0f);
        NormalizeBand(trebleState, analysis != null ? analysis.Treble : 0f);

        float activeDelta = activeBand switch
        {
            FrequencyBand.Bass => bassState.lastDelta,
            FrequencyBand.Treble => trebleState.lastDelta,
            _ => mainState.lastDelta,
        };

        staticTimer = activeDelta < staticDeltaThreshold ? staticTimer + Time.deltaTime : 0f;
        bool forceSwitch = staticTimer >= maxStaticTime;

        FrequencyBand selectedBand = autoSwitchBand
            ? PickMostResponsiveBand(bassState.activity, mainState.activity, trebleState.activity, forceSwitch)
            : frequencyBand;

        if (selectedBand != activeBand)
            staticTimer = 0f;

        activeBand = selectedBand;

        BandState activeState = selectedBand switch
        {
            FrequencyBand.Bass => bassState,
            FrequencyBand.Treble => trebleState,
            _ => mainState,
        };

        UpdatePulse(activeState);

        float scaledEnergy = ApplyIntensityMultiplier(pulseStrength);

        UpdateScale(scaledEnergy);
    }

    // Drives the "pop" feeling: the pulse jumps to the peak's strength the instant the active
    // band hits its strongest point (the "tum"/"bum"), then relaxes back down until the next one.
    private void UpdatePulse(BandState activeState)
    {
        peakCooldownTimer -= Time.deltaTime;

        if (activeState.isPeakNow && peakCooldownTimer <= 0f)
        {
            pulseStrength = activeState.peakNormalized;
            peakCooldownTimer = minTimeBetweenPeaks;
        }
        else
        {
            pulseStrength = Mathf.Max(0f, pulseStrength - pulseDecaySpeed * Time.deltaTime);
        }
    }

    // Normalizes a band's raw energy against its own recent peak (simple AGC), so bass/mid/treble
    // - which naturally live at very different raw magnitudes - all land in a comparable 0..1 range.
    // Also tracks how much that normalized value is currently changing ("activity").
    private float NormalizeBand(BandState state, float rawValue)
    {
        float raw = Mathf.Max(0f, rawValue);

        state.peak = Mathf.Max(raw, state.peak - state.peak * peakDecaySpeed * Time.deltaTime);
        state.peak = Mathf.Max(state.peak, 0.0001f);

        float normalized = Mathf.Clamp01(raw / state.peak);

        float delta = Mathf.Abs(normalized - state.previousNormalized);
        state.lastDelta = delta;
        state.activity = Mathf.Lerp(state.activity, delta, Mathf.Clamp01(Time.deltaTime * activitySmoothing));

        // A peak is the instant this band's normalized energy stops rising - the top of a hit -
        // as long as it's strong enough (peakThreshold) to count as a real "tum"/"bum".
        bool isRising = normalized > state.previousNormalized;
        state.isPeakNow = state.wasRising && !isRising && state.previousNormalized >= peakThreshold;
        state.peakNormalized = state.previousNormalized;
        state.wasRising = isRising;

        state.previousNormalized = normalized;

        return normalized;
    }

    // Picks whichever band is currently the most responsive, with hysteresis and a minimum hold
    // time so it settles on a band instead of flickering between near-equal candidates every frame.
    // When forceSwitch is true (the active band has been static too long), the hold time and
    // hysteresis are bypassed so it jumps straight to whichever band has the most activity now.
    private FrequencyBand PickMostResponsiveBand(float bassActivity, float mainActivity, float trebleActivity, bool forceSwitch)
    {
        bandHoldTimer -= Time.deltaTime;
        if (bandHoldTimer > 0f && !forceSwitch)
            return activeBand;

        float currentActivity = activeBand switch
        {
            FrequencyBand.Bass => bassActivity,
            FrequencyBand.Treble => trebleActivity,
            _ => mainActivity,
        };

        float hysteresis = forceSwitch ? 1f : switchHysteresis;

        FrequencyBand best = activeBand;
        float bestActivity = currentActivity;

        if (bassActivity > bestActivity * hysteresis)
        {
            best = FrequencyBand.Bass;
            bestActivity = bassActivity;
        }
        if (mainActivity > bestActivity * hysteresis)
        {
            best = FrequencyBand.Main;
            bestActivity = mainActivity;
        }
        if (trebleActivity > bestActivity * hysteresis)
        {
            best = FrequencyBand.Treble;
        }

        if (best != activeBand)
            bandHoldTimer = minBandHoldTime;

        return best;
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

    public void SetAutoSwitchBand(bool value) => autoSwitchBand = value;

    public void SetIntensityMultiplier(float value) => intensityMultiplier = value;

    public void SetMaxScaleMultiplier(float value) => maxScaleMultiplier = value;
}
