using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Skills/ParticleSkillTarget")]
public class ParticleSkillTarget : SkillBase
{

    public float speed = 10f;

    private const string TAG_NAME = "Particle";
    protected override void Execute(GameObject caster)
    {
        var spawnPoint = caster.transform.position + caster.transform.forward * 1.5f;
        var projectile = Instantiate(projectilePrefab, spawnPoint, caster.transform.rotation);
        projectile.GetComponent<ProjectileBase>().SetSourceSkill(this);

        var rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = caster.transform.forward * speed;

        var ps = projectile.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play(); 
        }
        var coroutineHelper = FindAnyObjectByType<SkillController>();
        if (coroutineHelper != null)
        {
            coroutineHelper.StartCoroutine(StopParticleCoroutine(projectile, skillLifeTime));
            ParticleSystem particle = GameObject.FindGameObjectWithTag(TAG_NAME).GetComponent<ParticleSystem>();
            if (particle != null)
            {
                coroutineHelper.StartCoroutine(WaitToPlayCoroutine(particle, skillLifeTime));
            }
        }
        else
        {
            Debug.LogError("Caster does not have a CoroutineHelper to start the coroutine!");
            Destroy(projectile, skillLifeTime);
        }
        coroutineHelper.StartCoroutine(StopProjectile(rb, skillLifeTime));
        Debug.Log($"{skillName} lançada causando {damage} de dano!");
    }

}