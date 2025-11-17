using UnityEngine;
public abstract class BossBaseSkill : MonoBehaviour
{
    public BossSkillData skillData;
    public abstract void OnSkillActivated(Transform bossTransform, Transform playerTransform);
    public virtual void OnSkillEnded()
    {
        
    }
}