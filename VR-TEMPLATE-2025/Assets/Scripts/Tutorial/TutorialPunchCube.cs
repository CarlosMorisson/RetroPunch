using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

// Step do tutorial: um grupo de cubos passa pelo jogador; ele precisa destruir todos.
// Se algum cubo passar sem ser destruido, o step reinicia e os cubos sao restaurados.
public class TutorialPunchCube : TutorialCondition
{
    [Header("Movimento")]
    public Transform CubeGroup;
    public Transform StartPoint;
    public Transform EndPoint;
    public float Speed = 2f;
    public float StartDelay = 1f;

    [Header("Cubos")]
    public List<TutorialCube> Cubes = new();

    [Header("Final")]
    public float DestroyDelay = 2f;

    private Tween moveTween;
    private int hitCount;
    private bool completed;

    public override void OnStepStart(TutorialController controller)
    {
        base.OnStepStart(controller);
        StartPass();
    }

    public override bool AdvanceStep()
    {
        if (!completed) return false;

        moveTween?.Kill();
        if (CubeGroup != null)
            Destroy(CubeGroup.gameObject, DestroyDelay);
        return true;
    }
    [ContextMenu("Move Cubes")]
    private void StartPass()
    {
        moveTween?.Kill();
        hitCount = 0;
        completed = false;

        foreach (var cube in Cubes)
        {
            if (cube == null) continue;
            cube.Owner = this;
            cube.Restore();
        }

        if (CubeGroup == null || StartPoint == null || EndPoint == null) return;

        CubeGroup.position = StartPoint.position;
        CubeGroup.gameObject.SetActive(true);

        moveTween = CubeGroup.DOMove(EndPoint.position, Speed)
            .SetSpeedBased()
            .SetEase(Ease.Linear)
            .SetDelay(StartDelay)
            .OnComplete(OnPassFinished);
    }

    // O grupo chegou no fim e ainda sobrou cubo: reinicia o step.
    private void OnPassFinished()
    {
        if (completed) return;

        if (Controller != null)
            Controller.RestartStep();
        else
            StartPass();
    }

    public virtual void NotifyCubeHit(TutorialCube cube)
    {
        if (completed) return;

        hitCount++;
        if (hitCount < Cubes.Count) return;

        completed = true;
        moveTween?.Kill();

        if (Controller != null)
            Controller.AdvanceTutorial();
    }

    protected virtual void OnDisable()
    {
        moveTween?.Kill();
    }
}
