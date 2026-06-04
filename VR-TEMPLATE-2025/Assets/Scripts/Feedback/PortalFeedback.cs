using UnityEngine;
using DG.Tweening;

public class PortalFeedback : MonoBehaviour
{
    public Transform PortalTransform;
    public Transform IconTransform;

    [Tooltip("Se deixado em branco, o script pegará o material do MeshRenderer automaticamente.")]
    public Material PortalMaterial;
    public PushType PushType;

    private Vector3 initialPortalScale;
    private Vector3 initialIconScale;
    private const float FAIL_SCALE = 0.8f;
    private const float SUCESS_SCALE = 1.5f;

    [Header("Animation Settings")]
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private float colorFadeBackDuration = 0.4f;

    private Color origOuterColor;
    private Color origOuterGlowColor;
    private Color origInnerFrameColor;
    private Color origInnerColorA;
    private Color origInnerColorB;

    // Definição das strings exatas das propriedades para o DOTween mapear sem erros
    private const string OuterColorProp = "_OuterColor";
    private const string OuterGlowColorProp = "_OuterGlowColor";
    private const string InnerFrameColorProp = "_InnerFrameColor";
    private const string InnerColorAProp = "_InnerColorA";
    private const string InnerColorBProp = "_InnerColorB";

    private void Start()
    {
        if (PortalTransform != null)
        {
            initialPortalScale = PortalTransform.localScale;
            MeshRenderer meshRenderer = PortalTransform.GetComponent<MeshRenderer>();

            if (meshRenderer != null)
            {
                if (PortalMaterial != null)
                {
                    PortalMaterial = new Material(PortalMaterial);
                    meshRenderer.material = PortalMaterial;
                }
                else
                {
                    PortalMaterial = meshRenderer.material;
                }

                CacheOriginalColors();
            }
        }

        if (IconTransform != null)
            initialIconScale = IconTransform.localScale;
    }

    private void CacheOriginalColors()
    {
        if (PortalMaterial == null) return;

        origOuterColor = PortalMaterial.GetColor(OuterColorProp);
        origOuterGlowColor = PortalMaterial.GetColor(OuterGlowColorProp);
        origInnerFrameColor = PortalMaterial.GetColor(InnerFrameColorProp);
        origInnerColorA = PortalMaterial.GetColor(InnerColorAProp);
        origInnerColorB = PortalMaterial.GetColor(InnerColorBProp);
    }

    [ContextMenu("Handle Success")]
    public void HandleSuccess()
    {
        if (PortalTransform == null || IconTransform == null) return;

        PortalTransform.DOKill(false);
        IconTransform.DOKill(false);
        if (PortalMaterial != null) PortalMaterial.DOKill(false);

        ResetScales();

        // Aplica o flash e inicia a transição controlada via Sequence
        ApplyInstantColorFlash(Color.white);
        AnimateColorBack();

        float halfDuration = duration * 0.5f;

        PortalTransform.DOScale(initialPortalScale * SUCESS_SCALE, halfDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                PortalTransform.DOScale(initialPortalScale, halfDuration).SetEase(Ease.OutBack);

                IconTransform.DOScale(initialIconScale * SUCESS_SCALE, halfDuration)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        IconTransform.DOScale(initialIconScale, halfDuration).SetEase(Ease.OutBack)
                            .OnComplete(() => ResetScales());
                    });
            });
    }

    [ContextMenu("Handle Fail")]
    public void HandleFail()
    {
        if (PortalTransform == null || IconTransform == null) return;

        PortalTransform.DOKill(false);
        IconTransform.DOKill(false);
        if (PortalMaterial != null) PortalMaterial.DOKill(false);

        ResetScales();

        // Aplica o flash e inicia a transição controlada via Sequence
        ApplyInstantColorFlash(Color.black);
        AnimateColorBack();

        float halfDuration = duration * 0.5f;

        PortalTransform.DOScale(initialPortalScale * FAIL_SCALE, halfDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                PortalTransform.DOScale(initialPortalScale, halfDuration).SetEase(Ease.OutBack);

                IconTransform.DOScale(initialIconScale * FAIL_SCALE, halfDuration)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        IconTransform.DOScale(initialIconScale, halfDuration).SetEase(Ease.OutBack)
                            .OnComplete(() => ResetScales());
                    });
            });
    }

    private void ApplyInstantColorFlash(Color targetColor)
    {
        if (PortalMaterial == null) return;

        PortalMaterial.SetColor(OuterColorProp, targetColor);
        PortalMaterial.SetColor(OuterGlowColorProp, targetColor);
        PortalMaterial.SetColor(InnerFrameColorProp, targetColor);
        PortalMaterial.SetColor(InnerColorAProp, targetColor);
        PortalMaterial.SetColor(InnerColorBProp, targetColor);
    }

    private void AnimateColorBack()
    {
        if (PortalMaterial == null) return;

        Sequence colorSequence = DOTween.Sequence();

        // Passando a string exata da propriedade do Shader no segundo parâmetro,
        // o DOTween garante que a interpolação funcione sem resetar instantaneamente.
        colorSequence.Join(PortalMaterial.DOColor(origOuterColor, OuterColorProp, colorFadeBackDuration));
        colorSequence.Join(PortalMaterial.DOColor(origOuterGlowColor, OuterGlowColorProp, colorFadeBackDuration));
        colorSequence.Join(PortalMaterial.DOColor(origInnerFrameColor, InnerFrameColorProp, colorFadeBackDuration));
        colorSequence.Join(PortalMaterial.DOColor(origInnerColorA, InnerColorAProp, colorFadeBackDuration));
        colorSequence.Join(PortalMaterial.DOColor(origInnerColorB, InnerColorBProp, colorFadeBackDuration));

        colorSequence.SetEase(Ease.OutQuad);
        colorSequence.Play();
    }

    private void ResetScales()
    {
        if (PortalTransform != null) PortalTransform.localScale = initialPortalScale;
        if (IconTransform != null) IconTransform.localScale = initialIconScale;
    }

    private void OnDestroy()
    {
        if (PortalMaterial != null)
        {
            Destroy(PortalMaterial);
        }
    }
}