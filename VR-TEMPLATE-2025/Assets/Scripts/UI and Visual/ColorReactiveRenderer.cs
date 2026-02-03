using UnityEngine;

public class ColorReactiveRenderer : MonoBehaviour
{
    public enum ColorType
    {
        Primary,
        Secondary
    }

    [Header("Config")]
    public ColorType colorType;

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

        Color color =
            colorType == ColorType.Primary
                ? ColorController.Instance.CurrentPrimary
                : ColorController.Instance.CurrentSecondary;

        ApplyColor(color);
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
}
