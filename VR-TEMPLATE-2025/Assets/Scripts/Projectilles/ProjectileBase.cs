using UnityEngine;
using System.Collections;

public abstract class ProjectileBase : MonoBehaviour
{
    public LayerMask collisionLayers;

    protected SkillBase _sourceSkill;
    protected Rigidbody _rb;
    protected Collider _collider;
    protected SkillController _skillController;
    protected GameObject _feedback;

    protected float _damage=>_sourceSkill.damage;

    public const string CASTER_TAG = "Player";

    
    public void SetSourceSkill(SkillBase skill)
    {
        _sourceSkill = skill;
    }
    public void InstantiateFeedback(Transform collideTransform, Vector3 collideVector)
    {
        if (_feedback == null)
        {
            _feedback = Instantiate(_sourceSkill.projectileFeedback, collideVector, Quaternion.identity, collideTransform);

            Destroy(_feedback, _feedback.GetComponent<ParticleSystem>().main.duration);
        }

    }
    protected virtual void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        _skillController = FindAnyObjectByType<SkillController>();
    }

    protected abstract void HandleHit(GameObject hitTarget, Vector3 hitPoint);
    protected abstract void OnProjectileDestroy();

    protected virtual void OnCollisionEnter(Collision collision)
    {

        if (collision.gameObject.CompareTag(CASTER_TAG))
        {
            return;
        }
        if (((1 << collision.gameObject.layer) & collisionLayers) != 0)
        {
            HandleHit(collision.gameObject, collision.contacts[0].point);
            InstantiateFeedback(collision.gameObject.transform, collision.contacts[0].point);
            _skillController.StartCoroutine(_sourceSkill.StopProjectile(_rb, 0f));
            _skillController.StartCoroutine(_sourceSkill.StopParticleCoroutine(this.gameObject, 0f));
        }
    }
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag(CASTER_TAG))
            return;

        if (((1 << other.gameObject.layer) & collisionLayers) != 0)
        {
            HandleHit(other.gameObject, other.ClosestPoint(transform.position));
            _skillController.StartCoroutine(_sourceSkill.StopProjectile(_rb, 0f));
            InstantiateFeedback(other.gameObject.transform, other.ClosestPoint(transform.position));
            _skillController.StartCoroutine(_sourceSkill.StopParticleCoroutine(this.gameObject, 0f));
        }
    }
    protected virtual void OnParticleCollision(GameObject Collider)
    {
        if (Collider.gameObject.CompareTag(CASTER_TAG))
        {
            return;
        }
        if (((1 << Collider.gameObject.layer) & collisionLayers) != 0)
        {
            print("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
            HandleHit(Collider.gameObject, Collider.transform.position);
            InstantiateFeedback(Collider.gameObject.transform, Collider.transform.position);
            _skillController.StartCoroutine(_sourceSkill.StopProjectile(_rb, 0f));
            _skillController.StartCoroutine(_sourceSkill.StopParticleCoroutine(this.gameObject, 0f));
        }
    }

    public void DeactivateProjectile()
    {
        OnProjectileDestroy();
    }

    protected virtual void OnDestroy()
    {
    }
}