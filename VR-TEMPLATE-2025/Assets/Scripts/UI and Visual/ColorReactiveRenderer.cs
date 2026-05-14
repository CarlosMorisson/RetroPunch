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

    private void Awake()
    {
        r = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();
    }

    private void LateUpdate()
    {
        if (ColorController.Instance == null || r == null)
            return;
        Color targetColor=Color.black;
        switch (colorType)
        {
            case ColorType.Primary:
                targetColor = ColorController.Instance.CurrentPrimary;
                break;
            case ColorType.Secondary:
                targetColor = ColorController.Instance.CurrentSecondary;
                break;
            case ColorType.PrimaryFreeze:
                targetColor = ColorController.Instance.PrimaryFreezeColor;
                break;
            case ColorType.SecondaryFreeze:
                targetColor = ColorController.Instance.SecondaryFreezeColor;
                break;
            case ColorType.PrimaryPower:
                targetColor = ColorController.Instance.PrimaryPowerColor;
                break;
            case ColorType.SecondaryPower:
                targetColor = ColorController.Instance.SecondaryPowerColor;
                break;
        }

        if (isOpositeColor)
        {
            targetColor = GetComplementaryColor(targetColor);
        }

        ApplyColor(targetColor);
    }

    private void ApplyColor(Color color)
    {
        r.GetPropertyBlock(mpb);

        if (r.sharedMaterial.HasProperty(colorProperty))
            mpb.SetColor(colorProperty, color);

        if (r.sharedMaterial.HasProperty(emissionProperty))
            mpb.SetColor(emissionProperty, color);

        r.SetPropertyBlock(mpb);
    }

    private Color GetComplementaryColor(Color source)
    {
        float h, s, v;
        Color.RGBToHSV(source, out h, out s, out v);
        h = (h + 0.5f) % 1f;

        return Color.HSVToRGB(h, s, v);
    }
}