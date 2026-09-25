using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Moves this object between a list of waypoints, but only in bursts triggered by the music's
/// beat. Uses the same band auto-switching approach as AudioSpectrumScaler: constantly tracks
/// Bass/Main/Treble and follows whichever is currently the most responsive, then moves the
/// instant that band peaks (the "tum"/"bum"), with speed proportional to how strong that peak
/// is - 0 when there's no beat, up to maxSpeed on the strongest hits.
///
/// Also draws the path being trilhado through a LineRenderer along those same waypoints, thin by
/// default, thickening and thinning reactively with the same beat detection.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class AudioMoveEffect : MonoBehaviour
{
    public enum FrequencyBand
    {
        Main,
        Bass,
        Treble
    }

    [Header("Waypoints")]
    [Tooltip("Transforms que o objeto vai visitar em sequencia. Ao chegar no ultimo, faz o caminho reverso ate o primeiro, em loop.")]
    [SerializeField] private List<Transform> waypoints = new();

    [Header("Auto Band Switching")]
    [Tooltip("Tempo minimo (segundos) que uma banda fica escolhida antes de poder trocar de novo. Evita alternancia nervosa.")]
    [SerializeField] private float minBandHoldTime = 0.15f;
    [Tooltip("Quanto a nova banda precisa superar a atual (em %) para haver troca. 1.15 = precisa estar 15% mais responsiva.")]
    [Range(1f, 2f)]
    [SerializeField] private float switchHysteresis = 1.15f;

    [Header("Band Normalization")]
    [Tooltip("Velocidade de decaimento do pico adaptativo de cada banda (tipo AGC). Maior = esquece mais rapido picos antigos.")]
    [SerializeField] private float peakDecaySpeed = 1.5f;
    [Tooltip("Suavizacao usada para medir a 'atividade' (variacao) recente de cada banda.")]
    [SerializeField] private float activitySmoothing = 8f;

    [Header("Peak Detection")]
    [Tooltip("Energia normalizada minima (0-1) da banda ativa para um pico ser considerado valido (o 'tum'/'bum' da batida).")]
    [Range(0f, 1f)]
    [SerializeField] private float peakThreshold = 0.4f;
    [Tooltip("Tempo minimo (segundos) entre um pico detectado e o proximo, pra nao disparar mais de uma vez na mesma batida.")]
    [SerializeField] private float minTimeBetweenPeaks = 0.12f;

    [Header("Movement")]
    [Tooltip("Velocidade (unidades/seg) usada quando o pico detectado tem energia normalizada maxima (1).")]
    [SerializeField] private float maxSpeed = 3f;
    [Tooltip("Por quanto tempo (segundos) o objeto continua se movendo apos um pico, antes de parar (velocidade 0) ate o proximo pico.")]
    [SerializeField] private float moveDurationPerPeak = 0.25f;
    [Tooltip("Distancia minima pra considerar que chegou no waypoint atual e passar pro proximo.")]
    [SerializeField] private float waypointReachDistance = 0.05f;

    [Header("Linha do Caminho (Trilha)")]
    [Tooltip("Desenha, com o LineRenderer deste objeto, o caminho ao longo dos waypoints.")]
    [SerializeField] private bool drawPathLine = true;
    [Tooltip("Espessura base da linha (fina).")]
    [SerializeField] private float baseLineWidth = 0.03f;
    [Tooltip("Quanto a espessura pode aumentar no pico do som. 1 = pode dobrar (2x a espessura base).")]
    [SerializeField] private float maxLineWidthMultiplier = 1f;
    [SerializeField] private float lineWidthSmoothingSpeed = 10f;
    [Tooltip("Velocidade (por segundo) com que a espessura da linha relaxa de volta ao normal apos um pico.")]
    [SerializeField] private float lineWidthPulseDecaySpeed = 3f;

    private class BandState
    {
        public float peak = 0.0001f;
        public float previousNormalized;
        public float activity;
        public bool wasRising;
        public bool isPeakNow;
        public float peakNormalized;
    }

    private readonly BandState bassState = new();
    private readonly BandState mainState = new();
    private readonly BandState trebleState = new();

    private FrequencyBand activeBand;
    private float bandHoldTimer;
    private float peakCooldownTimer;

    private float currentSpeed;
    private float moveTimer;

    private int currentIndex;
    private int direction = 1;

    private LineRenderer lineRenderer;
    private float lineWidthPulseStrength;
    private float currentLineWidthFactor = 1f;

    private void Awake()
    {
        currentIndex = 0;
        direction = 1;

        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.widthMultiplier = baseLineWidth;
        SetupPathLine();
    }

    private void Update()
    {
        if (waypoints.Count < 2)
            return;

        MusicAnalysis analysis = SongController.Instance != null ? SongController.Instance.GetAnalysis() : null;

        NormalizeBand(bassState, analysis != null ? analysis.Bass : 0f);
        NormalizeBand(mainState, analysis != null ? analysis.Intensity : 0f);
        NormalizeBand(trebleState, analysis != null ? analysis.Treble : 0f);

        activeBand = PickMostResponsiveBand(bassState.activity, mainState.activity, trebleState.activity);

        BandState activeState = activeBand switch
        {
            FrequencyBand.Bass => bassState,
            FrequencyBand.Treble => trebleState,
            _ => mainState,
        };

        DetectPeak(activeState);
        UpdatePathLine(activeState);

        if (moveTimer > 0f)
        {
            moveTimer -= Time.deltaTime;
            MoveTowardsCurrentWaypoint();
        }
    }

    // Draws the LineRenderer along the waypoints and reacts to the same beat detection used for
    // movement: the line's width pops on the peak (the "tum"/"bum") and relaxes back down after.
    private void UpdatePathLine(BandState activeState)
    {
        if (lineRenderer == null || !drawPathLine)
            return;

        if (activeState.isPeakNow)
            lineWidthPulseStrength = activeState.peakNormalized;
        else
            lineWidthPulseStrength = Mathf.Max(0f, lineWidthPulseStrength - lineWidthPulseDecaySpeed * Time.deltaTime);

        float targetWidthFactor = 1f + Mathf.Clamp01(lineWidthPulseStrength) * maxLineWidthMultiplier;
        currentLineWidthFactor = Mathf.Lerp(currentLineWidthFactor, targetWidthFactor, Time.deltaTime * lineWidthSmoothingSpeed);
        lineRenderer.widthMultiplier = baseLineWidth * currentLineWidthFactor;
    }

    // (Re)builds the line's positions from the current waypoints. Call again after SetWaypoints.
    private void SetupPathLine()
    {
        if (lineRenderer == null)
            return;

        lineRenderer.enabled = drawPathLine && waypoints.Count >= 2;
        if (!lineRenderer.enabled)
            return;

        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = waypoints.Count;
        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i] != null)
                lineRenderer.SetPosition(i, waypoints[i].position);
        }
    }

    // Normalizes a band's raw energy against its own recent peak (simple AGC), so bass/mid/treble
    // - which naturally live at very different raw magnitudes - all land in a comparable 0..1 range.
    // Also tracks the band's recent activity and flags the exact instant it peaks (rising, then not).
    private void NormalizeBand(BandState state, float rawValue)
    {
        float raw = Mathf.Max(0f, rawValue);

        state.peak = Mathf.Max(raw, state.peak - state.peak * peakDecaySpeed * Time.deltaTime);
        state.peak = Mathf.Max(state.peak, 0.0001f);

        float normalized = Mathf.Clamp01(raw / state.peak);

        float delta = Mathf.Abs(normalized - state.previousNormalized);
        state.activity = Mathf.Lerp(state.activity, delta, Mathf.Clamp01(Time.deltaTime * activitySmoothing));

        bool isRising = normalized > state.previousNormalized;
        state.isPeakNow = state.wasRising && !isRising && state.previousNormalized >= peakThreshold;
        state.peakNormalized = state.previousNormalized;
        state.wasRising = isRising;

        state.previousNormalized = normalized;
    }

    // Picks whichever band is currently the most responsive, with hysteresis and a minimum hold
    // time so it settles on a band instead of flickering between near-equal candidates every frame.
    private FrequencyBand PickMostResponsiveBand(float bassActivity, float mainActivity, float trebleActivity)
    {
        bandHoldTimer -= Time.deltaTime;
        if (bandHoldTimer > 0f)
            return activeBand;

        float currentActivity = activeBand switch
        {
            FrequencyBand.Bass => bassActivity,
            FrequencyBand.Treble => trebleActivity,
            _ => mainActivity,
        };

        FrequencyBand best = activeBand;
        float bestActivity = currentActivity;

        if (bassActivity > bestActivity * switchHysteresis)
        {
            best = FrequencyBand.Bass;
            bestActivity = bassActivity;
        }
        if (mainActivity > bestActivity * switchHysteresis)
        {
            best = FrequencyBand.Main;
            bestActivity = mainActivity;
        }
        if (trebleActivity > bestActivity * switchHysteresis)
        {
            best = FrequencyBand.Treble;
        }

        if (best != activeBand)
            bandHoldTimer = minBandHoldTime;

        return best;
    }

    // Triggers a movement burst the instant the active band peaks, with speed proportional to
    // how strong that peak was, and enough cooldown to not double-trigger on the same beat.
    private void DetectPeak(BandState activeState)
    {
        peakCooldownTimer -= Time.deltaTime;

        if (activeState.isPeakNow && peakCooldownTimer <= 0f)
        {
            currentSpeed = activeState.peakNormalized * maxSpeed;
            moveTimer = moveDurationPerPeak;
            peakCooldownTimer = minTimeBetweenPeaks;
        }
    }

    private void MoveTowardsCurrentWaypoint()
    {
        Transform target = waypoints[currentIndex];
        if (target == null)
            return;

        transform.position = Vector3.MoveTowards(transform.position, target.position, currentSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) <= waypointReachDistance)
            AdvanceWaypoint();
    }

    // Reverses direction at either end of the list instead of jumping back to the first waypoint.
    private void AdvanceWaypoint()
    {
        if (currentIndex + direction < 0 || currentIndex + direction >= waypoints.Count)
            direction = -direction;

        currentIndex += direction;
    }

    public void SetWaypoints(List<Transform> newWaypoints)
    {
        waypoints = newWaypoints;
        currentIndex = 0;
        direction = 1;
        SetupPathLine();
    }

    public void SetMaxSpeed(float value) => maxSpeed = value;
}
