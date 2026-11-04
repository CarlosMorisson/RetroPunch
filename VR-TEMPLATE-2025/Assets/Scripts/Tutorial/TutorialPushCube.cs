using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

// Step do tutorial do Push: um grupo de bolas passa pelo jogador; ele precisa empurrar todas para o portal certo.
// Se alguma bola falhar (portal errado, parede, tempo esgotado ou passar sem ser tocada),
// o step reinicia depois que todas forem resolvidas e as bolas sao restauradas.
public class TutorialPushCube : TutorialCondition
{
    [Header("Movimento")]
    public Transform CubeGroup;
    public Transform StartPoint;
    public Transform EndPoint;
    public float Speed = 2f;
    public float StartDelay = 1f;

    [Header("Bolas")]
    public List<TutorialPushBall> Balls = new();

    [Header("Final")]
    public float DestroyDelay = 2f;
    [Tooltip("Espera (s) apos a ultima bola falhar antes de reiniciar o step, para o feedback aparecer.")]
    public float RestartDelay = 1f;
    [Tooltip("Som (AudioLibrary) tocado quando o step e concluido e o tutorial avanca.")]
    public string AdvanceAudio = "Firework";

    private Tween moveTween;
    private Tween restartTween;
    private int ballCount;
    private int resolvedCount;
    private bool failed;
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
        restartTween?.Kill();
        if (CubeGroup != null)
            Destroy(CubeGroup.gameObject, DestroyDelay);
        return true;
    }

    [ContextMenu("Move Cubes")]
    private void StartPass()
    {
        moveTween?.Kill();
        restartTween?.Kill();
        ballCount = 0;
        resolvedCount = 0;
        failed = false;
        completed = false;

        foreach (var ball in Balls)
        {
            if (ball == null) continue;
            ball.Owner = this;
            ball.Restore();
            ballCount++;
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

    // O grupo chegou no fim: as bolas que nao foram tocadas contam como falha.
    // As que ja foram empurradas continuam voando ate acertar um portal ou o tempo acabar.
    private void OnPassFinished()
    {
        if (completed) return;

        foreach (var ball in Balls)
        {
            if (ball != null && !ball.IsResolved)
                ball.Miss();
        }
    }

    public void NotifyBallResolved(TutorialPushBall ball, bool success)
    {
        if (completed) return;

        resolvedCount++;
        if (!success) failed = true;

        if (resolvedCount < ballCount) return;

        moveTween?.Kill();

        if (failed)
        {
            restartTween?.Kill();
            // Ignora o time scale: com o Freeze ativo o delay demoraria ate 20x mais.
            restartTween = DOVirtual.DelayedCall(RestartDelay, Restart, true);
            return;
        }

        completed = true;

        if (AudioController.Instance != null && !string.IsNullOrEmpty(AdvanceAudio))
            AudioController.Instance.Play(AdvanceAudio);

        if (Controller != null)
            Controller.AdvanceTutorial();
    }

    // Bola Power acertou um portal: destroi so as bolas desta lista que ainda estao em jogo.
    public void PowerExplosion()
    {
        foreach (var ball in Balls)
        {
            if (ball != null && !ball.IsResolved)
                ball.PowerExplode();
        }
    }

    private void Restart()
    {
        // Se o jogador errou durante o Freeze/Power, o efeito e cancelado para o step voltar em tempo normal.
        if (FreezeEffect.Instance != null)
            FreezeEffect.Instance.CancelFreeze();
        if (PowerEffect.Instance != null)
            PowerEffect.Instance.CancelPower();

        if (Controller != null)
            Controller.RestartStep();
        else
            StartPass();
    }

    private void OnDisable()
    {
        moveTween?.Kill();
        restartTween?.Kill();
    }
}
