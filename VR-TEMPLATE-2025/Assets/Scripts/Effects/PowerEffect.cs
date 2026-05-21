using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;

public class PowerEffect : MonoBehaviour
{
    public static PowerEffect Instance { get; private set; }

    [Header("Timing")]
    public float PowerDuration = 7f;
    [SerializeField] private float restoreDuration = 0.5f;

    [Header("UI")]
    [SerializeField] private GameObject FreezeTimeObject;
    [SerializeField] private Image FreezeClookImage;

    [HideInInspector] public bool isPowered = false;

    [Header("Events")]
    public UnityEvent OnStartPowerTime;
    public UnityEvent OnFinishPowerTime;

    private Coroutine powerRoutine;
    private Tween objectScaleTween;
    private Tween clockFillTween;

    private const string TRANSITION_AUDIO = "TransitionEffect";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
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
    public void TriggerPower()
    {
        if (powerRoutine != null)
            StopCoroutine(powerRoutine);

        objectScaleTween?.Kill();
        clockFillTween?.Kill();

        powerRoutine = StartCoroutine(FreezeRoutine());
    }

    private IEnumerator FreezeRoutine()
    {
        isPowered = true;

        if (AudioController.Instance != null)
        {
            AudioController.Instance.Play(TRANSITION_AUDIO);
        }

        if (AudioEffect.Instance != null)
        {
            AudioEffect.Instance.ChangeAudioPreset(AudioEffect.EffectType.DarkSpace);
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
            clockFillTween = FreezeClookImage.DOFillAmount(1f, PowerDuration)
                .SetUpdate(true)
                .SetEase(Ease.Linear);
        }

        OnStartPowerTime?.Invoke();

        yield return new WaitForSecondsRealtime(PowerDuration);

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

        OnFinishPowerTime?.Invoke();

        if (AudioController.Instance != null)
        {
            AudioController.Instance.Play(TRANSITION_AUDIO);
        }

        powerRoutine = null;
        isPowered = false;
    }

    private void OnDestroy()
    {
        objectScaleTween?.Kill();
        clockFillTween?.Kill();
    }
}