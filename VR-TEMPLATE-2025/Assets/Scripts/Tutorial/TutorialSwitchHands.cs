using UnityEngine;

// Step do tutorial da troca de maos: igual ao TutorialPunchCube, mas cada cubo destruido conta para a troca
// do SwitchHandsController usando uma tolerancia propria do tutorial (TutorialTolerance).
// Quando o step termina a tolerancia original volta, a troca reinicia do zero e este script se desliga.
public class TutorialSwitchHands : TutorialPunchCube
{
    [Header("Troca de Maos")]
    [Tooltip("Tolerancia do SwitchHandsController enquanto o tutorial roda (quantos cubos sao ignorados antes de trocar).")]
    [Min(0)] public int TutorialTolerance = 2;

    private int originalTolerance;
    private bool overriding;

    public override void OnStepStart(TutorialController controller)
    {
        enabled = true;
        ApplyTutorialTolerance();
        base.OnStepStart(controller);
    }

    public override bool AdvanceStep()
    {
        if (!base.AdvanceStep()) return false;

        RestoreSwitch();
        enabled = false;
        return true;
    }

    public override void NotifyCubeHit(TutorialCube cube)
    {
        // Conta antes do base: o ultimo cubo avanca o tutorial, que ja restaura e zera a troca.
        if (overriding && SwitchHandsController.Instance != null)
            SwitchHandsController.Instance.RandomizeCurrentBothHands();

        base.NotifyCubeHit(cube);
    }

    // Tambem e chamado quando o step reinicia: a troca volta do zero a cada tentativa.
    private void ApplyTutorialTolerance()
    {
        SwitchHandsController switchHands = SwitchHandsController.Instance;
        if (switchHands == null) return;

        if (!overriding)
        {
            originalTolerance = switchHands.randomizeTolerance;
            overriding = true;
        }

        switchHands.randomizeTolerance = TutorialTolerance;
        switchHands.ResetSwitch();
    }

    private void RestoreSwitch()
    {
        if (!overriding) return;
        overriding = false;

        SwitchHandsController switchHands = SwitchHandsController.Instance;
        if (switchHands == null) return;

        switchHands.randomizeTolerance = originalTolerance;
        switchHands.ResetSwitch();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        RestoreSwitch();
    }
}
