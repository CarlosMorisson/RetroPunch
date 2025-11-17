// FireballSkill.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Skills/ProjeticleSkill")]
public class ProjectileSkill : SkillBase
{
    public GameObject projectilePrefab;

    public float speed = 10f;
    public float damage = 30f;

    protected override void Execute(GameObject caster)
    {
        var spawnPoint = caster.transform.position + caster.transform.forward * 1.5f;
        var projectile = Instantiate(projectilePrefab, spawnPoint, caster.transform.rotation, caster.transform);

        var rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = caster.transform.forward * speed;
        Destroy(projectile, skillLifeTime);
        Debug.Log($"{skillName} lan�ada causando {damage} de dano!");
    }
}
