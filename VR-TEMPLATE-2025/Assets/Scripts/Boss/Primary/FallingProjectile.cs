using UnityEngine;

public class FallingProjectile : MonoBehaviour
{
    private Vector3 _targetPosition;
    private float _fallSpeed;
    private float _damage;
    private bool _isFalling;

    public GameObject impactEffectPrefab;
    public AudioClip impactSound;
    public LayerMask playerLayer; 

    public void Initialize(Vector3 targetPos, float speed, float dmg)
    {
        _targetPosition = targetPos;
        _fallSpeed = speed;
        _damage = dmg;
        _isFalling = true;

        // Opcional: Rotacionar o projétil para ele "apontar" para baixo se for um modelo 3D
        // transform.LookAt(_targetPosition);
    }

    void Update()
    {
        if (!_isFalling) return;

        transform.position = Vector3.MoveTowards(transform.position, _targetPosition, _fallSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, _targetPosition) < 0.1f)
        {
            Impact();
            _isFalling = false;
            ObjectPooler.Instance.ReturnToPool("BossRainProjectile", gameObject); 
        }
    }

    private void Impact()
    {
        Debug.Log($"Projectile impacted at {_targetPosition}. Dealing {_damage} damage!");

        if (impactEffectPrefab != null)
        {
            GameObject gameObj=Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
            Destroy(gameObj, 2f);
        }
        if (impactSound != null)
        {
            AudioSource.PlayClipAtPoint(impactSound, transform.position);
        }

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 1.5f, playerLayer);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                Debug.Log($"Rain projectile hit player for {_damage} damage!");
                // hitCollider.GetComponent<PlayerHealth>().TakeDamage(_damage);
            }
        }
    }
}