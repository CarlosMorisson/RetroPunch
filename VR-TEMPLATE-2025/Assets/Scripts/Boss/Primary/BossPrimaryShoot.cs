using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Importante para usar LINQ

public class BossPrimaryShoot : BossBaseSkill
{
    [Header("Skill Prefabs (Requires ObjectPooler setup)")]
    public string rainProjectilePoolTag = "BossRainRainProjectile";
    public string targetMarkerPoolTag = "RainTargetMarker";

    [Header("Skill Settings")]
    public Animator SkillAnimation;
    public Transform playerTransform;

    [Header("Rain Attack Settings")]
    public int numberOfProjectiles = 5;
    public float rainAreaRadius = 5f;
    public float timeBetweenProjectiles = 0.2f;
    public float projectileSpawnHeight = 20f;
    public float projectileFallSpeed = 10f;
    public float markerWarningTime = 2f;

    [Header("Rain Feedback")]
    public List<ShootFeedback> ShootParticle = new();


    private const string SKILL_ANIM = "Primary";
    private int _projectilesSpawnedCount;


    public override void OnSkillActivated(Transform bossTransform, Transform playerTransform)
    {
        this.playerTransform = playerTransform;
        SkillAnimation.CrossFade(SKILL_ANIM, 0);
        _projectilesSpawnedCount = 0;

        //ShootParticleFeedback();

        //Invoke("StartRainOfFire", 0.5f);
        DeactivateAllShootFeedback();
    }

  
    public override void OnSkillEnded()
    {
        base.OnSkillEnded(); 
        Debug.Log("BossPrimaryShoot finished, deactivating feedback.");
    }


    public void ShootParticleFeedback()
    {
        ShootFeedback feedback = ShootParticle.FirstOrDefault(fb => fb.Shoot != null && !fb.Shoot.activeInHierarchy);

        if (feedback != null)
        {

            feedback.Shoot.SetActive(true);


            if (feedback.ExplosionParticle != null)
            {
                feedback.ExplosionParticle.Play();
            }
            if (feedback.ShootParticle != null)
            {
                feedback.ShootParticle.Play();
            }
            Debug.Log($"Activated shoot feedback: {feedback.Shoot.name}");
        }
        else
        {
            Debug.LogWarning("No inactive ShootFeedback GameObject found to activate, or all are active.");
        }
    }

    private void DeactivateAllShootFeedback()
    {
        foreach (var feedback in ShootParticle)
        {
            if (feedback.Shoot != null && feedback.Shoot.activeInHierarchy)
            {
                // Para e limpa as partículas antes de desativar, se necessário
                if (feedback.ExplosionParticle != null) feedback.ExplosionParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                if (feedback.ShootParticle != null) feedback.ShootParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                feedback.Shoot.SetActive(false);
            }
        }
    }


    private void StartRainOfFire()
    {
        CancelInvoke("SpawnProjectile");

        float totalRainDuration = numberOfProjectiles * timeBetweenProjectiles;

        InvokeRepeating("SpawnProjectile", 0f, timeBetweenProjectiles);

    }

    private void SpawnProjectile()
    {
        if (_projectilesSpawnedCount >= numberOfProjectiles)
        {
            CancelInvoke("SpawnProjectile");
 
            return;
        }

        Vector3 targetPosition = GetRandomPositionInRainArea();

        GameObject markerObj = ObjectPooler.Instance.SpawnFromPool(targetMarkerPoolTag, targetPosition, Quaternion.identity);
        if (markerObj != null)
        {
            RainTargetMarker marker = markerObj.GetComponent<RainTargetMarker>();
            if (marker != null)
            {
                marker.ActivateMarker();
            }
            else
            {
                Debug.LogWarning($"Marker prefab '{targetMarkerPoolTag}' does not have a RainTargetMarker component.");
            }
        }

        Invoke("DropProjectile", markerWarningTime, targetPosition);

        _projectilesSpawnedCount++;
    }

    private void DropProjectile(Vector3 targetPosition)
    {

        Vector3 spawnPosition = new Vector3(targetPosition.x, targetPosition.y + projectileSpawnHeight, targetPosition.z);
        GameObject projectile = ObjectPooler.Instance.SpawnFromPool(rainProjectilePoolTag, spawnPosition, Quaternion.identity);
        if (projectile != null)
        {
            FallingProjectile fallingProjectile = projectile.GetComponent<FallingProjectile>();
            if (fallingProjectile == null)
            {
                fallingProjectile = projectile.AddComponent<FallingProjectile>();
            }
            fallingProjectile.Initialize(targetPosition, projectileFallSpeed, skillData.damage);
        }
    }

    private Vector3 GetRandomPositionInRainArea()
    {
        Vector3 playerPos = playerTransform.position;
        Vector2 randomOffset = Random.insideUnitCircle * rainAreaRadius;

        Vector3 targetPos = playerPos + new Vector3(randomOffset.x, 0, randomOffset.y);

        RaycastHit hit;
        if (Physics.Raycast(targetPos + Vector3.up * 10f, Vector3.down, out hit, 100f, LayerMask.GetMask("Ground")))
        {
            targetPos.y = hit.point.y + 0.1f;
        }
        else
        {
            targetPos.y = playerPos.y;
        }

        return targetPos;
    }

    public void Invoke(string methodName, float time, Vector3 param)
    {
        StartCoroutine(InvokeWithParam(methodName, time, param));
    }

    private System.Collections.IEnumerator InvokeWithParam(string methodName, float time, Vector3 param)
    {
        yield return new WaitForSeconds(time);
        if (methodName == "DropProjectile")
        {
            DropProjectile(param);
        }
    }
}

[System.Serializable]
public class ShootFeedback
{
    public GameObject Shoot; 
    public ParticleSystem ExplosionParticle; 
    public ParticleSystem ShootParticle;   
}