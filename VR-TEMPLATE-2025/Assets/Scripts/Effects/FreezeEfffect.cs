using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;
using NUnit.Framework.Constraints; // Importante para o DOTween funcionar

public class FreezeEffect : MonoBehaviour
{
    [Header("Time Scale")]
    [SerializeField] private float defaultTimeScale = 1f;
    [SerializeField] private float freezeTimeScale = 0.05f;

    [Header("Timing")]
    [SerializeField] private float slowDownDuration = 1f;
    [SerializeField] private float freezeDuration = 7f;
    [SerializeField] private float restoreDuration = 0.5f;

    [Header("UI")]
    [SerializeField] private GameObject FreezeTimeObject;
    [SerializeField] private Image FreezeClookImage; 

    public UnityEvent OnStartFreezeTime;
    public UnityEvent OnFinishFreezeTime;

    private Coroutine freezeRoutine;
    private Tween objectScaleTween;
    private Tween clockFillTween;

    private const string TRANSITION_AUDIO = "TransitionEffect";

    private void Start()
    {
        Time.timeScale = defaultTimeScale;

        if (FreezeTimeObject != null)
        {
            FreezeTimeObject.transform.localScale = Vector3.zero;
        }
        if (FreezeClookImage != null)
        {
            FreezeClookImage.fillAmount = 0f;
        }
    }

    [ContextMenu("teste")]
    public void TriggerFreeze()
    {
        if (freezeRoutine != null)
            StopCoroutine(freezeRoutine);

        objectScaleTween?.Kill();
        clockFillTween?.Kill();

        freezeRoutine = StartCoroutine(FreezeRoutine());
    }

    private IEnumerator FreezeRoutine()
    {
        yield return LerpTimeScale(defaultTimeScale, freezeTimeScale, slowDownDuration);
        AudioController.Instance.Play(TRANSITION_AUDIO);
        if (AudioEffect.Instance != null)
        {
            AudioEffect.Instance.ChangeAudioPreset(AudioEffect.EffectType.DistortedSlow);
        }

        if (FreezeTimeObject != null)
        {
            FreezeTimeObject.SetActive(true);
            objectScaleTween = FreezeTimeObject.transform.DOScale(Vector3.one, 0.3f)
                .SetUpdate(true)
                .SetEase(Ease.OutBack);
        }

        if (FreezeClookImage != null)
        {
            FreezeClookImage.fillAmount = 0f;
            clockFillTween = FreezeClookImage.DOFillAmount(1f, freezeDuration)
                .SetUpdate(true) 
                .SetEase(Ease.Linear); 
        }

        OnStartFreezeTime?.Invoke();

        yield return new WaitForSecondsRealtime(freezeDuration);

        if (FreezeTimeObject != null)
        {
            objectScaleTween = FreezeTimeObject.transform.DOScale(Vector3.zero, restoreDuration)
                .SetUpdate(true)
                .SetEase(Ease.InBack)
                .OnComplete(() => FreezeTimeObject.SetActive(false));
        }

        if (AudioEffect.Instance != null)
        {
            AudioEffect.Instance.ChangeAudioPreset(AudioEffect.EffectType.Normal);
        }

        yield return LerpTimeScale(freezeTimeScale, defaultTimeScale, restoreDuration);

        OnFinishFreezeTime?.Invoke();
        AudioController.Instance.Play(TRANSITION_AUDIO);
        freezeRoutine = null;
    }

    private IEnumerator LerpTimeScale(float from, float to, float duration)
    {
        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;

            float t = time / duration;
            t = t * t * (3f - 2f * t);

            Time.timeScale = Mathf.Lerp(from, to, t);

            yield return null;
        }

        Time.timeScale = to;
    }

    private void OnDestroy()
    {
        objectScaleTween?.Kill();
        clockFillTween?.Kill();
    }
}