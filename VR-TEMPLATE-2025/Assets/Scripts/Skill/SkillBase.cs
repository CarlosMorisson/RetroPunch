using UnityEngine;
using System;
using System.Collections;

public abstract class SkillBase : ScriptableObject
{
    [Header("Prefabs")]
    public GameObject projectilePrefab;
    public GameObject projectileFeedback;
    [Header("Base Data")]
    public string skillName;
    public float cooldown = 2f;
    public float manaCost = 10f;
    public float skillLifeTime = 5f;
    public Sprite icon;
    public float damage;

    private float _cooldownTimer = 0f; 

    public event Action<float, float> OnCooldownUpdated;

    public float CurrentCooldown => _cooldownTimer; 
    public float TotalCooldown => cooldown;

    public virtual bool CanCast(GameObject caster, float currentMana)
    {
        return _cooldownTimer <= 0 && currentMana >= manaCost;
    }

    public void TickCooldown(float deltaTime)
    {
        if (_cooldownTimer > 0)
        {
            _cooldownTimer -= deltaTime;
            if (_cooldownTimer < 0)
            {
                _cooldownTimer = 0;
            }
            OnCooldownUpdated?.Invoke(_cooldownTimer, cooldown);
        }
    }

    public bool TryCast(GameObject caster, float currentMana)
    {
        if (!CanCast(caster, currentMana))
        {
            if (_cooldownTimer > 0)
            {
                OnCooldownUpdated?.Invoke(_cooldownTimer, cooldown);
            }
            return false;
        }

        Execute(caster);
        _cooldownTimer = cooldown;
        OnCooldownUpdated?.Invoke(_cooldownTimer, cooldown);
        return true;
    }

    public virtual IEnumerator StopParticleCoroutine(GameObject projectile, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (projectile != null)
        {
            var ps = projectile.GetComponentInChildren<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop();
                yield return new WaitForSeconds(ps.main.duration);
            }
            Destroy(projectile, delay);
        }
    }
    public virtual IEnumerator WaitToPlayCoroutine(ParticleSystem projectile, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (projectile != null)
        {
            projectile.Play();
        }
    }
    public virtual IEnumerator StopProjectile(Rigidbody rig, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (rig != null)
        {
            rig.linearVelocity *= 0;
        }
    }
    protected abstract void Execute(GameObject caster);
}