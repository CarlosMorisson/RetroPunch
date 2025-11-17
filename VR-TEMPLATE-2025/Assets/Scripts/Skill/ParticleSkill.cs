using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Skills/ParticleSkill")]
public class ParticleSkill : SkillBase
{
    public float speed = 10f;

    protected override void Execute(GameObject caster)
    {
        var spawnPoint = caster.transform.position + caster.transform.forward * 1.5f;
        var projectile = Instantiate(projectilePrefab, spawnPoint, caster.transform.rotation, caster.transform);
        projectile.GetComponent<ProjectileBase>().SetSourceSkill(this);
        var ps = projectile.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
        }
        var coroutineHelper = FindAnyObjectByType<SkillController>();
        Debug.Log(caster.name);
        if (coroutineHelper != null)
        {
            coroutineHelper.StartCoroutine(StopParticleCoroutine(projectile, skillLifeTime));
        }
        else
        {
            Debug.LogError("Caster does not have a CoroutineHelper to start the coroutine!");
            Destroy(projectile, skillLifeTime);
        }

        Debug.Log($"{skillName} lançada causando {damage} de dano!");
    }

}