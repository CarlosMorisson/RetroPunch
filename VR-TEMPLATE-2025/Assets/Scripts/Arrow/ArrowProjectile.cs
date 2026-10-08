using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    [Header("Settings")]
    public float lifetime = 5f;

    [Tooltip("A flecha rotaciona para apontar na direção do movimento")]
    public bool orientToVelocity = true;

    private Rigidbody _rb;
    private bool _initialized;
    private bool _hit;
    private const string TAG_NAME = "Arrow";
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public void Initialize(Vector3 direction, float force)
    {
        _initialized = true;
        _hit = false;

        Destroy(gameObject, lifetime);
    }

    private void FixedUpdate()
    {
        if (!_initialized || _hit) return;
        if (_rb == null) return;

        // Rotaciona a flecha para apontar na direção do movimento
        if (orientToVelocity && _rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(_rb.linearVelocity);
        }
    }
    public void OnHitTarget()
    {
        _hit = true;
        ObjectPooler.Instance.ReturnToPool(TAG_NAME, this.gameObject);
    }
}