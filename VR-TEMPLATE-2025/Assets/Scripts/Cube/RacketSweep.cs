using UnityEngine;

// Evita que a raquete atravesse a bola em golpes rápidos.
// A raquete não tem Rigidbody (é movida pelo tracking), então num golpe rápido ela "pula" a bola entre dois passos
// de física e o OnCollisionEnter nunca acontece. Aqui, a cada frame, é feito um BoxCast da posição anterior
// até a atual e a batida é aplicada em qualquer PushCube que estiver no caminho.
// A colisão normal continua funcionando para golpes lentos; o TryLockTouch do cubo impede batida dupla.
[RequireComponent(typeof(BoxCollider))]
public class RacketSweep : MonoBehaviour
{
    [Tooltip("Layers testadas pelo sweep. Deixe só a layer das bolas para economizar.")]
    [SerializeField] private LayerMask hitMask = ~0;
    [Tooltip("Abaixo desta distância por frame (m) o sweep não roda; a colisão normal já resolve.")]
    [SerializeField] private float minSweepDistance = 0.01f;
    [Tooltip("Margem extra (m) somada em cada lado da caixa durante o sweep.")]
    [SerializeField] private float extraPadding = 0f;

    private const int MAX_HITS = 8;
    private readonly RaycastHit[] hits = new RaycastHit[MAX_HITS];

    private BoxCollider box;
    private HandVelocityTracker velocityTracker;
    private Vector3 lastCenter;
    private bool hasLastCenter;

    private void Awake()
    {
        box = GetComponent<BoxCollider>();
        velocityTracker = GetComponentInParent<HandVelocityTracker>();
    }

    private void OnEnable()
    {
        // Evita um sweep gigante do lugar onde a raquete foi desativada até onde ela reaparece
        hasLastCenter = false;
    }

    // LateUpdate: garante que o tracking do controle já moveu a raquete neste frame
    private void LateUpdate()
    {
        Vector3 center = transform.TransformPoint(box.center);

        if (!hasLastCenter)
        {
            lastCenter = center;
            hasLastCenter = true;
            return;
        }

        Vector3 delta = center - lastCenter;
        float distance = delta.magnitude;

        if (distance >= minSweepDistance && Time.deltaTime > 0f)
            Sweep(lastCenter, delta / distance, distance, delta / Time.deltaTime);

        lastCenter = center;
    }

    private void Sweep(Vector3 from, Vector3 direction, float distance, Vector3 sweepVelocity)
    {
        Vector3 lossy = transform.lossyScale;
        Vector3 halfExtents = Vector3.Scale(box.size * 0.5f, new Vector3(Mathf.Abs(lossy.x), Mathf.Abs(lossy.y), Mathf.Abs(lossy.z)))
            + Vector3.one * extraPadding;

        // Usa a rotação atual no sweep inteiro; a translação é o que causa o túnel
        int count = Physics.BoxCastNonAlloc(from, halfExtents, direction, hits, transform.rotation,
            distance, hitMask, QueryTriggerInteraction.Ignore);

        if (count == 0)
            return;

        // Mesma velocidade usada pela colisão normal; a do sweep só entra se não houver tracker
        Vector3 handVelocity = velocityTracker != null ? velocityTracker.Velocity : sweepVelocity;

        for (int i = 0; i < count; i++)
        {
            Collider col = hits[i].collider;
            if (col == null || col == box)
                continue;

            PushCube cube = col.GetComponentInParent<PushCube>();
            if (cube == null && col.attachedRigidbody != null)
                cube = col.attachedRigidbody.GetComponentInChildren<PushCube>();
            if (cube == null)
                continue;

            // distance 0 = a caixa já começou sobrepondo o collider; nesse caso o point vem zerado
            Vector3 point = hits[i].distance > 0f ? hits[i].point : col.ClosestPoint(from);

            cube.TryPlayerHit(gameObject, point, direction, handVelocity);
        }
    }

    private void OnDrawGizmosSelected()
    {
        BoxCollider b = box != null ? box : GetComponent<BoxCollider>();
        if (b == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(b.center, b.size + Vector3.one * (extraPadding * 2f));
    }
}
