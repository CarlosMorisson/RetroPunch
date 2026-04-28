using UnityEngine;
using DG.Tweening;

public class FadeIEffect : MonoBehaviour
{
    public GameObject RetroCanvas;
    public Material FadeMaterial;

    [Header("Config")]
    public float punchScale = 0.3f;
    public float punchDuration = 0.2f;
    public float scaleDuration = 0.3f;
    public float fadeDuration = 0.5f;

    private void Start()
    {
        RetroCanvas.transform.localScale = Vector3.one;
        FadeOut();
    }
    [ContextMenu("FadeOut")]
    public void FadeOut()
    {
        Sequence seq = DOTween.Sequence();

        RetroCanvas.transform.localScale = Vector3.one;

        seq.Append(
            RetroCanvas.transform
                .DOPunchScale(Vector3.one * punchScale, punchDuration, 10, 1)
        );

        seq.Append(
            RetroCanvas.transform
                .DOScale(Vector3.zero, scaleDuration)
                .SetEase(Ease.InBack)
        );

        seq.OnComplete(() =>
        {
            FadeMaterial.DOFade(0f, fadeDuration);
        });
    }
    [ContextMenu("FadeIn")]
    public void FadeIn()
    {
        Sequence seq = DOTween.Sequence();

        seq.Append(
            FadeMaterial.DOFade(1f, fadeDuration)
        );

        RetroCanvas.transform.localScale = Vector3.zero;

        seq.Append(
            RetroCanvas.transform
                .DOScale(Vector3.one, scaleDuration)
                .SetEase(Ease.OutBack)
        );

        seq.Append(
            FadeMaterial.DOFade(1f, fadeDuration)
        );
    }

    private void SetAlpha(float value)
    {
        Color c = FadeMaterial.color;
        c.a = value;
        FadeMaterial.color = c;
    }
}