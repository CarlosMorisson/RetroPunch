using UnityEngine;

// Classe base das condicoes de avanco do tutorial.
// Herde desta classe e sobrescreva AdvanceStep para disparar eventos/animacoes
// ou bloquear o avanco (retornando false) ate a condicao ser cumprida.
public class TutorialCondition : MonoBehaviour
{
    protected TutorialController Controller { get; private set; }

    // Chamado pelo TutorialController quando o step comeca (ou e reiniciado).
    public virtual void OnStepStart(TutorialController controller)
    {
        Controller = controller;
    }

    public virtual bool AdvanceStep()
    {
        return true;
    }
}
