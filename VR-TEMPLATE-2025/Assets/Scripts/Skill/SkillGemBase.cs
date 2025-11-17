using UnityEngine;

public class SkillGemBase : MonoBehaviour
{
    [Header("Skill Data")]
    public SkillBase SkillBaseGem;
    [Header("Material do socket quando a skill esta On")]
    public Material MaterialRenderIsOn;
    [Header("Particula que aparece quando ele esta habilitado")]
    public ParticleSystem ParticleIsOn;
    [Header("Particula que aparece quando esta em tempo de recarga")]
    public ParticleSystem ParticleIsOff;

    [Header("Icone da Skill Gem (se diferente da SkillBase)")]
    public Sprite SkillGemIcon;

    public Material GetRenderMaterial()
    {
        return MaterialRenderIsOn;
    }
    public ParticleSystem GetIsOnParticle()
    {
        return ParticleIsOn;
    }
    public ParticleSystem GetIsOffParticle()
    {
        return ParticleIsOff;
    }
    public virtual SkillBase GetSkillBase()
    {
        return SkillBaseGem;
    }
}