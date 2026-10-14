using System;
using System.Collections.Generic;
using UnityEngine;

// Ajuda na colisão das mãos do hand tracking com os cubos de soco (PunchCube / MultiPunchCube).
// O tracking de mão atualiza em saltos e às vezes congela por alguns frames; num soco rápido a mão "pula" o cubo
// entre dois passos de física e o OnCollisionEnter nunca acontece. Aqui, a cada frame, é feito um SphereCast da
// posição anterior até a atual (com um raio de tolerância) e o golpe é aplicado em qualquer cubo no caminho.
// A colisão normal continua funcionando; as travas dos cubos impedem golpe duplo.
// Coloque este componente só nas mãos do hand tracking, no objeto que tem o collider com a tag Player.
public class HandHitAssist : MonoBehaviour
{
    [Header("Sweep")]
    [Tooltip("Layers testadas. Deixe só a layer dos cubos para economizar.")]
    [SerializeField] private LayerMask hitMask = ~0;
    [Tooltip("Centro da esfera de detecção (ex: nós dos dedos). Vazio = este transform.")]
    [SerializeField] private Transform center;
    [Tooltip("Raio base (m) da esfera de detecção. Um pouco maior que a mão = tolerância.")]
    [SerializeField] private float assistRadius = 0.06f;
    [Tooltip("Raio extra (m) por m/s de velocidade da mão. Golpes rápidos ficam mais tolerantes.")]
    [SerializeField] private float radiusPerSpeed = 0.01f;
    [Tooltip("Limite do raio extra (m) somado pela velocidade.")]
    [SerializeField] private float maxSpeedRadius = 0.04f;

    [Header("Filtros")]
    [Tooltip("Abaixo desta velocidade (m/s) a ajuda não age; toques lentos já são resolvidos pela colisão normal.")]
    [SerializeField] private float minAssistSpeed = 0.5f;
    [Tooltip("Se a mão andar mais que isso (m) num único frame, é salto do tracking: o sweep não roda.")]
    [SerializeField] private float maxStepDistance = 0.35f;

    [Header("Perda de Tracking")]
    [Tooltip("Quando o tracking congela no meio de um golpe rápido, continua a trajetória por alguns ms.")]
    [SerializeField] private bool usePrediction = false;
    [Tooltip("Tempo máximo (s) de trajetória prevista enquanto o tracking está congelado.")]
    [SerializeField] private float predictionTime = 0.08f;
    [Tooltip("Velocidade mínima (m/s) para prever a trajetória.")]
    [SerializeField] private float minPredictionSpeed = 1.5f;

    private const int MAX_HITS = 8;
    private const float FROZEN_EPSILON = 0.0001f;

    private readonly RaycastHit[] hits = new RaycastHit[MAX_HITS];
    private static readonly IComparer<RaycastHit> ByDistance = Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));

    // Cubos tocados no frame anterior: só conta quando a mão "entra" no cubo, igual ao OnCollisionEnter
    private HashSet<IHandHittable> previousContacts = new HashSet<IHandHittable>();
    private HashSet<IHandHittable> currentContacts = new HashSet<IHandHittable>();

    private HandVelocityTracker velocityTracker;
    private Vector3 lastCenter;
    private bool hasLastCenter;

    // Estado da previsão durante o congelamento do tracking
    private Vector3 frozenOrigin;
    private Vector3 frozenVelocity;
    private Vector3 lastVirtualCenter;
    private float frozenTime;
    private bool isFrozen;
    private Vector3 lastVelocity;

    private Vector3 CenterPosition => center != null ? center.position : transform.position;

    private void Awake()
    {
        velocityTracker = GetComponentInParent<HandVelocityTracker>();
    }

    private void OnEnable()
    {
        // Evita um sweep gigante do lugar onde a mão sumiu até onde ela reaparece
        hasLastCenter = false;
        isFrozen = false;
        previousContacts.Clear();
        currentContacts.Clear();
    }

    // LateUpdate: garante que o tracking já moveu a mão e que os cubos já atualizaram o overlap no Update
    private void LateUpdate()
    {
        Vector3 position = CenterPosition;
        float dt = Time.deltaTime;

        if (!hasLastCenter || dt <= 0f)
        {
            lastCenter = position;
            hasLastCenter = true;
            return;
        }

        // A ajuda é só para o hand tracking; nos controles a colisão normal já funciona
        if (SwitchHandsController.Instance != null && SwitchHandsController.Instance.UsingController)
        {
            lastCenter = position;
            previousContacts.Clear();
            return;
        }

        currentContacts.Clear();

        Vector3 delta = position - lastCenter;
        float distance = delta.magnitude;

        if (distance < FROZEN_EPSILON)
            HandleFrozenFrame(dt);
        else
            HandleTrackedFrame(position, delta, distance, dt);

        lastCenter = position;

        // Troca os conjuntos sem alocar
        (previousContacts, currentContacts) = (currentContacts, previousContacts);
    }

    private void HandleTrackedFrame(Vector3 position, Vector3 delta, float distance, float dt)
    {
        Vector3 from = lastCenter;

        // Tracking voltou depois de congelar: continua de onde a previsão parou
        if (isFrozen)
        {
            isFrozen = false;
            from = lastVirtualCenter;
            delta = position - from;
            distance = delta.magnitude;
        }

        Vector3 velocity = velocityTracker != null ? velocityTracker.Velocity : delta / dt;
        lastVelocity = velocity;

        // Salto do tracking (teleporte): não varre um caminho que a mão não fez
        if (distance > maxStepDistance || distance < FROZEN_EPSILON)
            return;

        Sweep(from, delta / distance, distance, velocity.magnitude);
    }

    private void HandleFrozenFrame(float dt)
    {
        if (!usePrediction)
            return;

        if (!isFrozen)
        {
            if (lastVelocity.magnitude < minPredictionSpeed)
                return;

            isFrozen = true;
            frozenTime = 0f;
            frozenOrigin = lastCenter;
            frozenVelocity = lastVelocity;
            lastVirtualCenter = lastCenter;
        }

        if (frozenTime >= predictionTime)
            return;

        frozenTime = Mathf.Min(frozenTime + dt, predictionTime);
        Vector3 virtualCenter = frozenOrigin + frozenVelocity * frozenTime;

        Vector3 delta = virtualCenter - lastVirtualCenter;
        float distance = delta.magnitude;
        if (distance > FROZEN_EPSILON)
            Sweep(lastVirtualCenter, delta / distance, distance, frozenVelocity.magnitude);

        lastVirtualCenter = virtualCenter;
    }

    private void Sweep(Vector3 from, Vector3 direction, float distance, float speed)
    {
        // Mão lenta: deixa para a colisão normal e não marca contato, assim um soco logo depois ainda conta
        if (speed < minAssistSpeed)
            return;

        float radius = assistRadius + Mathf.Min(speed * radiusPerSpeed, maxSpeedRadius);

        int count = Physics.SphereCastNonAlloc(from, radius, direction, hits, distance, hitMask, QueryTriggerInteraction.Ignore);
        if (count == 0)
            return;

        // Do mais próximo ao mais distante: o primeiro cubo no caminho recebe o golpe primeiro
        Array.Sort(hits, 0, count, ByDistance);

        for (int i = 0; i < count; i++)
        {
            Collider col = hits[i].collider;
            if (col == null)
                continue;

            IHandHittable cube = col.GetComponentInParent<IHandHittable>();
            if (cube == null && col.attachedRigidbody != null)
                cube = col.attachedRigidbody.GetComponentInChildren<IHandHittable>();
            if (cube == null || !currentContacts.Add(cube))
                continue;

            // Já estava em contato no frame anterior: não é uma nova entrada
            if (previousContacts.Contains(cube))
                continue;

            Vector3 handPosition = from + direction * hits[i].distance;
            // distance 0 = a esfera já começou sobrepondo o collider; nesse caso o point vem zerado
            Vector3 point = hits[i].distance > 0f ? hits[i].point : col.ClosestPoint(from);

            cube.TryHandAssistHit(gameObject, handPosition, point);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(CenterPosition, assistRadius);
        Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
        Gizmos.DrawWireSphere(CenterPosition, assistRadius + maxSpeedRadius);
    }
}
