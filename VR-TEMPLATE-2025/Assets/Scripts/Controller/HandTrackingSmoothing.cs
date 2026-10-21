using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.Processing;

// Suavização da posição das mãos do hand tracking (filtro One Euro de verdade, Casiez et al. 2012).
// Substitui o HandsOneEuroFilterPostProcessor do sample da XRI, que num soco rápido fazia a mão atrasar,
// disparar ou congelar: ele usava a velocidade com sinal por eixo (alpha podia passar de 1 ou ir a 0),
// ignorava o deltaTime no alpha e usava Time.deltaTime, que muda com o Freeze/slow motion.
// Aqui: mão parada = bem suavizada (sem tremer); mão rápida = quase sem atraso.
public class HandTrackingSmoothing : MonoBehaviour, IXRHandProcessor
{
    [Tooltip("Desligue para comparar com o tracking cru.")]
    [SerializeField] private bool smoothingEnabled = true;
    [Tooltip("Frequência de corte (Hz) com a mão parada. Menor = mais suave e com mais atraso. Suba se a mão parada parecer \"mole\"; desça se tremer.")]
    [SerializeField] private float minCutoff = 1.5f;
    [Tooltip("Quanto o corte sobe por m/s de velocidade. Maior = menos atraso em golpes rápidos.")]
    [SerializeField] private float beta = 5f;
    [Tooltip("Frequência de corte (Hz) usada para suavizar a velocidade medida.")]
    [SerializeField] private float derivativeCutoff = 1f;
    [Tooltip("Salto maior que isso (m) entre amostras é teleporte do tracking: o filtro reinicia em vez de arrastar a mão.")]
    [SerializeField] private float resetDistance = 0.5f;

    public int callbackOrder => 0;

    private readonly OneEuroVector3 leftFilter = new OneEuroVector3();
    private readonly OneEuroVector3 rightFilter = new OneEuroVector3();

    private XRHandSubsystem subsystem;
    private static readonly List<XRHandSubsystem> SubsystemsReuse = new List<XRHandSubsystem>();

    private void OnDisable()
    {
        if (subsystem != null)
        {
            subsystem.UnregisterProcessor(this);
            subsystem = null;
        }
    }

    private void Update()
    {
        if (subsystem != null && subsystem.running)
            return;

        SubsystemManager.GetSubsystems(SubsystemsReuse);
        for (int i = 0; i < SubsystemsReuse.Count; i++)
        {
            XRHandSubsystem handSubsystem = SubsystemsReuse[i];
            if (!handSubsystem.running)
                continue;

            subsystem?.UnregisterProcessor(this);
            subsystem = handSubsystem;
            leftFilter.Reset();
            rightFilter.Reset();
            subsystem.RegisterProcessor(this);
            return;
        }
    }

    // Chamado no update normal e de novo antes do render; o dt real entre as duas amostras é usado.
    public void ProcessJoints(XRHandSubsystem handSubsystem, XRHandSubsystem.UpdateSuccessFlags successFlags, XRHandSubsystem.UpdateType updateType)
    {
        if (!smoothingEnabled)
            return;

        double now = Time.realtimeSinceStartupAsDouble;

        XRHand left = handSubsystem.leftHand;
        if (FilterHand(ref left, leftFilter, now))
            handSubsystem.SetCorrespondingHand(left);

        XRHand right = handSubsystem.rightHand;
        if (FilterHand(ref right, rightFilter, now))
            handSubsystem.SetCorrespondingHand(right);
    }

    private bool FilterHand(ref XRHand hand, OneEuroVector3 filter, double now)
    {
        if (!hand.isTracked)
        {
            filter.Reset();
            return false;
        }

        Pose rootPose = hand.rootPose;
        Vector3 filtered = filter.Filter(rootPose.position, now, minCutoff, beta, derivativeCutoff, resetDistance);

        // SetRootPose move todas as juntas junto com a raiz, mantendo o formato da mão
        hand.SetRootPose(new Pose(filtered, rootPose.rotation));
        return true;
    }

    private class OneEuroVector3
    {
        private Vector3 lastFiltered;
        private Vector3 lastRaw;
        private Vector3 lastVelocity;
        private double lastTime;
        private bool initialized;

        public void Reset() => initialized = false;

        public Vector3 Filter(Vector3 raw, double time, float minCutoff, float beta, float derivativeCutoff, float resetDistance)
        {
            if (!initialized || (raw - lastFiltered).sqrMagnitude > resetDistance * resetDistance)
            {
                initialized = true;
                lastRaw = raw;
                lastFiltered = raw;
                lastVelocity = Vector3.zero;
                lastTime = time;
                return raw;
            }

            float dt = (float)(time - lastTime);
            // Mesma amostra (ou relógio parado): mantém o valor filtrado
            if (dt <= 1e-5f)
                return lastFiltered;

            Vector3 velocity = (raw - lastRaw) / dt;
            lastVelocity = Vector3.Lerp(lastVelocity, velocity, Alpha(derivativeCutoff, dt));

            // Velocidade em módulo: a suavização é igual em qualquer direção do soco
            float cutoff = minCutoff + beta * lastVelocity.magnitude;
            lastFiltered = Vector3.Lerp(lastFiltered, raw, Alpha(cutoff, dt));

            lastRaw = raw;
            lastTime = time;
            return lastFiltered;
        }

        private static float Alpha(float cutoff, float dt)
        {
            float tau = 1f / (2f * Mathf.PI * Mathf.Max(cutoff, 0.0001f));
            return 1f / (1f + tau / dt);
        }
    }
}
