using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class TutorialController : MonoBehaviour
{
    [Header("Configuracoes de Tutorial")]
    public List<TutorialList> TutorialSteps = new();
    public Transform TutorialTransform;
    private int _currentIndex = -1;
    private int tutorialType;

    private const float ANIM_DURATION = 0.5f;
    private const Ease DEFAULT_EASE = Ease.OutBack;

    private List<TutorialStep> CurrentSteps => TutorialSteps[tutorialType].steps;
    private TutorialStep CurrentStep => CurrentSteps[_currentIndex];

    // Todas as condicoes (de todos os game modes) comecam inativas; cada uma so e ativada na sua vez.
    private void Awake()
    {
        foreach (TutorialList list in TutorialSteps)
        {
            if (list == null) continue;
            foreach (TutorialStep step in list.steps)
                SetConditionActive(step?.Condition, false);
        }
    }

    public void StartTutorial()
    {
        tutorialType = (int)StageLoadController.Instance.gameType;
        if (tutorialType < 0 || tutorialType >= TutorialSteps.Count || CurrentSteps.Count == 0) return;

        _currentIndex = 0;
        PlayStepAnimation(CurrentStep);
    }

    [ContextMenu("Advance")]
    public void AdvanceTutorial()
    {
        if (_currentIndex < 0 || _currentIndex >= CurrentSteps.Count) return;

        TutorialStep step = CurrentStep;
        if (step.Condition != null && !step.Condition.AdvanceStep()) return;

        step.StepGameObject.SetActive(false);
        SetConditionActive(step.Condition, false);
        _currentIndex++;

        if (_currentIndex < CurrentSteps.Count)
        {
            PlayStepAnimation(CurrentStep);
        }
        else
        {
            Debug.Log("Tutorial Concluido!");
            GameState.Instance.GameStateGame();
        }
    }

    // Reinicia a condicao do step atual (ex: cubos do tutorial voltam e passam de novo).
    public void RestartStep()
    {
        if (_currentIndex < 0 || _currentIndex >= CurrentSteps.Count) return;

        TutorialStep step = CurrentStep;
        if (step.Condition != null)
        {
            SetConditionActive(step.Condition, true);
            step.Condition.OnStepStart(this);
        }
    }

    private void SetConditionActive(TutorialCondition condition, bool active)
    {
        if (condition == null) return;

        // Nao desativa o proprio controller caso a condicao esteja no mesmo objeto (ou num pai dele).
        if (!active && transform.IsChildOf(condition.transform))
        {
            condition.enabled = false;
            return;
        }

        condition.gameObject.SetActive(active);
        condition.enabled = active;
    }

    private void PlayStepAnimation(TutorialStep step)
    {
        step.StepGameObject.SetActive(true);

        if (step.Condition != null)
        {
            SetConditionActive(step.Condition, true);
            step.Condition.OnStepStart(this);
        }

        if (step.TextPanel == null) return;

        Vector3 textScale = step.TextPanel.transform.localScale;
        step.TextPanel.transform.localScale = Vector3.zero;
        step.TextPanel.transform.DOScale(textScale, ANIM_DURATION).SetEase(DEFAULT_EASE);
    }
}
[System.Serializable]
public class TutorialList
{
    public string GameType;
    public List<TutorialStep> steps = new List<TutorialStep>();
}
[System.Serializable]
public class TutorialStep
{
    public GameObject StepGameObject;
    public GameObject TextPanel;
    public TutorialCondition Condition;
}
