using UnityEngine;

public class ColorReactiveRenderer : MonoBehaviour
{
    public enum ColorType
    {
        Primary,
        Secondary,
        PrimaryFreeze,
        SecondaryFreeze,
        PrimaryPower,
        SecondaryPower
    }

    [Header("Config")]
    public ColorType colorType;
    public bool isOpositeColor;

    [SerializeField] private string colorProperty = "_Color";
    [SerializeField] private string emissionProperty = "_EmissionColor";

    private Renderer r;
    private MaterialPropertyBlock mpb;

    private Material lastResolvedMaterial;
    private string resolvedColorProperty;
    private string resolvedEmissionProperty;

    private void Awake()
    {
        r = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();
        ResolveShaderProperties();
    }

    // O material do renderer pode ser trocado em runtime depois do Awake
    // (ex: SkyboxVisual.ApplySceneInstant troca o material do chao/parede
    // por uma instancia nova). Reavaliar aqui evita ficar com o mapeamento
    // de propriedades resolvido contra o shader antigo.
    private void ResolveShaderProperties()
    {
        Material material = r.sharedMaterial;
        lastResolvedMaterial = material;

        resolvedColorProperty = colorProperty;
        resolvedEmissionProperty = emissionProperty;

        Shader shader = material != null ? material.shader : null;
        if (shader == null)
            return;

        switch (shader.name)
        {
            case ColorController.SHADER_NAME_PROCEDURAL:
                resolvedColorProperty = "_BaseColor";
                resolvedEmissionProperty = "_LineEmissionColor";
                break;
            case ColorController.SHADER_NAME_URP:
                resolvedColorProperty = "_PrimaryColor";
                resolvedEmissionProperty = "_SecondaryEmissionColor";
                break;
        }
    }

    private void LateUpdate()
    {
        if (ColorController.Instance == null || r == null)
            return;

        if (r.sharedMaterial != lastResolvedMaterial)
            ResolveShaderProperties();

        Color reactiveColor = GetReactiveColor();

        if (isOpositeColor)
        {
            reactiveColor = GetComplementaryColor(reactiveColor);
        }

        ApplyColor(reactiveColor);
    }

    private Color GetReactiveColor()
    {
        switch (colorType)
        {
            case ColorType.Primary:
                return ColorController.Instance.CurrentPrimary;
            case ColorType.Secondary:
                return PowerEffect.Instance.isPowered
                    ? ColorController.Instance.CurrentPrimary
                    : ColorController.Instance.CurrentSecondary;
            case ColorType.PrimaryFreeze:
                return ColorController.Instance.PrimaryFreezeColor;
            case ColorType.SecondaryFreeze:
                return ColorController.Instance.SecondaryFreezeColor;
            case ColorType.PrimaryPower:
                return ColorController.Instance.PrimaryPowerColor;
            case ColorType.SecondaryPower:
                return ColorController.Instance.SecondaryPowerColor;
            default:
                return Color.black;
        }
    }

    private void ApplyColor(Color reactiveColor)
    {
        r.GetPropertyBlock(mpb);

        Color baseColor = GetBaseColor();

        if (r.sharedMaterial.HasProperty(resolvedColorProperty))
            mpb.SetColor(resolvedColorProperty, baseColor);

        if (r.sharedMaterial.HasProperty(resolvedEmissionProperty))
            mpb.SetColor(resolvedEmissionProperty, reactiveColor);

        r.SetPropertyBlock(mpb);
    }

    private Color GetBaseColor()
    {
        switch (colorType)
        {
            case ColorType.Primary:
                return ColorController.Instance.PrimaryBaseColor;
            case ColorType.Secondary:
                return ColorController.Instance.SecondaryBaseColor;
            case ColorType.PrimaryFreeze:
                return ColorController.Instance.PrimaryBaseFreezeColor;
            case ColorType.SecondaryFreeze:
                return ColorController.Instance.SecondaryBaseFreezeColor;
            case ColorType.PrimaryPower:
                return ColorController.Instance.PrimaryBasePowerColor;
            case ColorType.SecondaryPower:
                return ColorController.Instance.SecondaryBasePowerColor;
            default:
                return Color.black;
        }
    }

    private Color GetComplementaryColor(Color source)
    {
        float h, s, v;
        Color.RGBToHSV(source, out h, out s, out v);
        h = (h + 0.5f) % 1f;

        return Color.HSVToRGB(h, s, v);
    }
}