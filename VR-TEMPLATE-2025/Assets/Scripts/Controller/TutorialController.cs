using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using DG.Tweening;

public class TutorialController : MonoBehaviour
{
    [Header("Configurações de Tutorial")]
    public List<TutorialStep> TutorialSteps = new();
    private int _currentIndex = -1;

    private const float ANIM_DURATION = 0.5f;
    private const float PUNCH_ELASTICITY = 0.5f;
    private const float SHAKE_STRENGTH = 0.2f;
    private const Ease DEFAULT_EASE = Ease.OutBack;

    public void StartTutorial()
    {
        if (TutorialSteps.Count == 0) return;

        _currentIndex = 0;
        PlayStepAnimation(_currentIndex);
    }

    public void AdvanceTutorial()
    {
        if (_currentIndex >= 0 && _currentIndex < TutorialSteps.Count)
        {
            TutorialSteps[_currentIndex].StepGameObject.SetActive(false);
        }

        _currentIndex++;

        if (_currentIndex < TutorialSteps.Count)
        {
            PlayStepAnimation(_currentIndex);
        }
        else
        {
            Debug.Log("Tutorial Concluído!");
        }
    }

    private void PlayStepAnimation(int index)
    {
        var step = TutorialSteps[index];

        Vector3 panelScale = step.Panel.localScale;
        Vector3 videoScale = step.VideoPanel.transform.localScale;
        Vector3 textScale = step.TextPanel.transform.localScale;
        Vector3 buttonScale = step.Button.transform.localScale;

        step.Panel.localScale = Vector3.zero;
        step.VideoPanel.transform.localScale = Vector3.zero;
        step.TextPanel.transform.localScale = Vector3.zero;
        step.Button.transform.localScale = Vector3.zero;

        step.StepGameObject.SetActive(true);

        Sequence seq = DOTween.Sequence();

        seq.Append(step.Panel.DOScale(panelScale, ANIM_DURATION).SetEase(DEFAULT_EASE));

        seq.Append(step.VideoPanel.transform.DOScale(videoScale, ANIM_DURATION).SetEase(DEFAULT_EASE)
            .OnComplete(() => {
                if (step.Clip != null) step.Clip.Play();
            }));

        seq.Join(step.TextPanel.transform.DOScale(textScale, ANIM_DURATION).SetEase(DEFAULT_EASE));

        seq.Append(step.Button.transform.DOScale(buttonScale, ANIM_DURATION).SetEase(DEFAULT_EASE)
            .OnComplete(() => {
                step.Button.transform.DOShakeScale(0.3f, SHAKE_STRENGTH)
                .OnComplete(() => step.Button.transform.localScale = buttonScale);
            }));
    }
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