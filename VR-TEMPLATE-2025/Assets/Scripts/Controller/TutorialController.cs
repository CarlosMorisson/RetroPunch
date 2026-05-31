using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using static UnityEngine.Rendering.DebugUI;

public class TutorialController : MonoBehaviour
{
    [Header("Configurações de Tutorial")]
    public List<TutorialList> TutorialSteps = new();
    private int _currentIndex = -1;
    private int tutorialType;

    private const float ANIM_DURATION = 0.5f;
    private const float PUNCH_ELASTICITY = 0.5f;
    private const float SHAKE_STRENGTH = 0.2f;
    private const Ease DEFAULT_EASE = Ease.OutBack;

    public void StartTutorial()
    {
        if (TutorialSteps.Count == 0) return;
        tutorialType = (int)StageLoadController.Instance.gameType;
        _currentIndex = 0;
        PlayStepAnimation(_currentIndex);
    }
    [ContextMenu("Advance")]
    public void AdvanceTutorial()
    {
        if (_currentIndex >= 0 && _currentIndex < TutorialSteps.Count)
        {
            TutorialSteps[_currentIndex].steps[tutorialType].StepGameObject.SetActive(false);
        }

        _currentIndex++;

        if (_currentIndex < TutorialSteps.Count)
        {
            PlayStepAnimation(_currentIndex);
        }
        else
        {
            Debug.Log("Tutorial Concluído!");
            GameState.Instance.GameStateGame();
        }
    }

    private void PlayStepAnimation(int index)
    {
        var step = TutorialSteps[index];

        Vector3 panelScale = step.steps[tutorialType].Panel.localScale;
        Vector3 videoScale = step.steps[tutorialType].VideoPanel.transform.localScale;
        Vector3 textScale = step.steps[tutorialType].TextPanel.transform.localScale;
        Vector3 buttonScale = step.steps[tutorialType].Button.transform.localScale;

        step.steps[tutorialType].Panel.localScale = Vector3.zero;
        step.steps[tutorialType].VideoPanel.transform.localScale = Vector3.zero;
        step.steps[tutorialType].TextPanel.transform.localScale = Vector3.zero;
        step.steps[tutorialType].Button.transform.localScale = Vector3.zero;

        step.steps[tutorialType].StepGameObject.SetActive(true);

        Sequence seq = DOTween.Sequence();

        seq.Append(step.steps[tutorialType].Panel.DOScale(panelScale, ANIM_DURATION).SetEase(DEFAULT_EASE));

        seq.Append(step.steps[tutorialType].VideoPanel.transform.DOScale(videoScale, ANIM_DURATION).SetEase(DEFAULT_EASE)
            .OnComplete(() => {
                if (step.steps[tutorialType].Clip != null) step.steps[tutorialType].Clip.Play();
            }));

        seq.Join(step.steps[tutorialType].TextPanel.transform.DOScale(textScale, ANIM_DURATION).SetEase(DEFAULT_EASE));

        seq.Append(step.steps[tutorialType].Button.transform.DOScale(buttonScale, ANIM_DURATION).SetEase(DEFAULT_EASE)
            .OnComplete(() => {
                step.steps[tutorialType].Button.transform.DOShakeScale(0.3f, SHAKE_STRENGTH)
                .OnComplete(() => step.steps[tutorialType].Button.transform.localScale = buttonScale);
            }));
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
    public Transform Panel;
    public VideoPlayer Clip;
    public GameObject VideoPanel;
    public GameObject TextPanel;
    public GameObject Button;
}