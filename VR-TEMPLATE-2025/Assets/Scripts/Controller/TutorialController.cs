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
            TutorialSteps[tutorialType].steps[_currentIndex].StepGameObject.SetActive(false);
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
        var step = TutorialSteps[tutorialType];

        Vector3 panelScale = step.steps[_currentIndex].Panel.localScale;
        Vector3 videoScale = step.steps[_currentIndex].VideoPanel.transform.localScale;
        Vector3 textScale = step.steps[_currentIndex].TextPanel.transform.localScale;
        Vector3 buttonScale = step.steps[_currentIndex].Button.transform.localScale;

        step.steps[_currentIndex].Panel.localScale = Vector3.zero;
        step.steps[_currentIndex].VideoPanel.transform.localScale = Vector3.zero;
        step.steps[_currentIndex].TextPanel.transform.localScale = Vector3.zero;
        step.steps[_currentIndex].Button.transform.localScale = Vector3.zero;

        step.steps[_currentIndex].StepGameObject.SetActive(true);

        Sequence seq = DOTween.Sequence();

        seq.Append(step.steps[_currentIndex].Panel.DOScale(panelScale, ANIM_DURATION).SetEase(DEFAULT_EASE));

        seq.Append(step.steps[_currentIndex].VideoPanel.transform.DOScale(videoScale, ANIM_DURATION).SetEase(DEFAULT_EASE)
            .OnComplete(() => {
                if (step.steps[_currentIndex].Clip != null) step.steps[_currentIndex].Clip.Play();
            }));

        seq.Join(step.steps[_currentIndex].TextPanel.transform.DOScale(textScale, ANIM_DURATION).SetEase(DEFAULT_EASE));

        seq.Append(step.steps[_currentIndex].Button.transform.DOScale(buttonScale, ANIM_DURATION).SetEase(DEFAULT_EASE)
            .OnComplete(() => {
                step.steps[_currentIndex].Button.transform.DOShakeScale(0.3f, SHAKE_STRENGTH)
                .OnComplete(() => step.steps[_currentIndex].Button.transform.localScale = buttonScale);
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