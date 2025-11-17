using UnityEngine;
using DG.Tweening;

public class BossThirdSkill : BossBaseSkill
{
    public float scaleDuration = 0.5f;
    public Vector3 targetScale = new Vector3(1.5f, 1.5f, 1.5f);
    public float followSpeed = 5f;
    public ParticleSystem specificParticleSystem;
    public Animator SkillAnimation;

    public Transform transformToAffect; 
    public Transform playerTransform;

    private const string SKILL_ANIM = "Third";
    private const float TIME_TO_FINISH = 5f;
    private Vector3 _startVector;

    public override void OnSkillActivated(Transform bossTransform, Transform playerTransform)
    {
        SkillAnimation.CrossFade(SKILL_ANIM, 0);
        _startVector = transformToAffect.localScale;
        transformToAffect.DOScale(targetScale, scaleDuration)
            .OnComplete(StartFollowingAndParticles);
    }

    private void StartFollowingAndParticles()
    {
        if (specificParticleSystem != null)
        {
            specificParticleSystem.Play();
        }

        isFollowing = true;
    }

    private bool isFollowing = false;

    void Update()
    {
        if (isFollowing && transformToAffect != null && playerTransform != null)
        {
            LookAtPlayer();
            
        }
    }
    private void LookAtPlayer()
    {
        Vector3 directionToPlayer = playerTransform.position - transform.position;

        directionToPlayer.y = 0;

        if (directionToPlayer == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, followSpeed * Time.deltaTime);
    }
    public void FinishSkill()
    {
        Debug.Log("BossThirdShoot finished, deactivating feedback.");
        isFollowing = false;
        transformToAffect.DOKill();
        if (specificParticleSystem != null)
        {
            specificParticleSystem.Stop();
        }
        transformToAffect.DOScale(_startVector, scaleDuration);
    }
    public override void OnSkillEnded()
    {
        base.OnSkillEnded();
        Debug.Log("BossThirdShoot finished, deactivating feedback.");
        isFollowing = false; 
        transformToAffect.DOKill();
        if (specificParticleSystem != null)
        {
            specificParticleSystem.Stop();
        }
    }
}