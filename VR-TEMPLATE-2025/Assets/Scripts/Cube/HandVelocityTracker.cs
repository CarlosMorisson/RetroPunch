using UnityEngine;

// Mede a velocidade real da mão a partir da posição (as mãos não têm Rigidbody, então não há linearVelocity).
public class HandVelocityTracker : MonoBehaviour
{
    [Tooltip("Janela de tempo (s) usada para suavizar a velocidade")]
    [SerializeField] private float smoothingWindow = 0.06f;

    private Vector3 lastPosition;

    public Vector3 Velocity { get; private set; }

    private void OnEnable()
    {
        lastPosition = transform.position;
        Velocity = Vector3.zero;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        Vector3 frameVelocity = (transform.position - lastPosition) / dt;
        lastPosition = transform.position;

        // Média móvel exponencial: remove o tremor do tracking sem atrasar demais o golpe
        float t = smoothingWindow > 0f ? Mathf.Clamp01(dt / smoothingWindow) : 1f;
        Velocity = Vector3.Lerp(Velocity, frameVelocity, t);
    }
}
