using UnityEngine;

public class BossSecondaryShoot : BossBaseSkill
{
    public Animator SkillAnimation;
    private const string SKILL_ANIM = "Secondary";

    [SerializeField]
    private HandEnergy RightHand, LeftHand;

    private bool _isAnimating = false;

    public override void OnSkillActivated(Transform bossTransform, Transform playerTransform)
    {
        SkillAnimation.CrossFade(SKILL_ANIM, 0);
        
    }

    public void DesactiveLine()
    {
        if (RightHand != null)
        {
            if (RightHand.ThrowParticle != null)
            {
                RightHand.ThrowParticle.Play();
            }
            if (RightHand.LineEnergy != null && RightHand.StartPoint != null && RightHand.FinalPoint != null)
            {
                RightHand.LineEnergy.enabled = false;
                _isAnimating = false;
            }
        }

        if (LeftHand != null)
        {
            if (LeftHand.ThrowParticle != null)
            {
                LeftHand.ThrowParticle.Play(); // Assume que ParticleCollision tem um método Play()
            }
            if (LeftHand.LineEnergy != null && LeftHand.StartPoint != null && LeftHand.FinalPoint != null)
            {
                LeftHand.LineEnergy.enabled = false;
                _isAnimating = false;
            }
        }
    }

    public void ActivateHandEnergy()
    {
        if (RightHand != null)
        {
            if (RightHand.ThrowParticle != null)
            {
                RightHand.ThrowParticle.Play(); 
            }
            if (RightHand.LineEnergy != null && RightHand.StartPoint != null && RightHand.FinalPoint != null)
            {
                RightHand.LineEnergy.enabled = true;
                _isAnimating = true;
            }
        }

        if (LeftHand != null)
        {
            if (LeftHand.ThrowParticle != null)
            {
                LeftHand.ThrowParticle.Play(); // Assume que ParticleCollision tem um método Play()
            }
            if (LeftHand.LineEnergy != null && LeftHand.StartPoint != null && LeftHand.FinalPoint != null)
            {
                LeftHand.LineEnergy.enabled = true;
                _isAnimating = true;
            }
        }
    }
    public void Update()
    {
        if (_isAnimating)
        {
            LeftHand.LineEnergy.SetPosition(0, LeftHand.StartPoint.position);
            LeftHand.LineEnergy.SetPosition(1, LeftHand.FinalPoint.position);
            RightHand.LineEnergy.SetPosition(0, RightHand.StartPoint.position);
            RightHand.LineEnergy.SetPosition(1, RightHand.FinalPoint.position);
        }
        else
        {
            LeftHand.LineEnergy.enabled = false;
            RightHand.LineEnergy.enabled = false;
        }
    }
}

[System.Serializable]
public class HandEnergy
{
    public ParticleSystem ThrowParticle; // Alterado para ParticleSystem, que é o tipo comum para partículas
    public LineRenderer LineEnergy;
    public Transform StartPoint;
    public Transform FinalPoint;
}