using DG.Tweening;
using UnityEngine;

public class ColorController : MonoBehaviour
{
    public static ColorController Instance;

    [Header("Config")]
    [SerializeField] private float colorLerpSpeed = 6f;
    [SerializeField] private float frequencyMultiplier = 1.5f;

    private BuildSettings settings;

    public Color CurrentPrimary;
    public Color CurrentSecondary;

    private Color _currentPrimaryColorLight;
    private Color _currentPrimaryColorDark;

    private Color _currentSecondaryColorLight;
    private Color _currentSecondaryColorDark;

    private Tween primaryTween;
    private Tween secondaryTween;

    private bool isTransitioning = false;

    private const float COLOR_VALUE = 2f;


    public Material GetPrimaryMaterial(Material baseMaterial)
    {
        return CreateMaterialWithColor(baseMaterial, CurrentPrimary);
    }

    private Material CreateMaterialWithColor(Material source, Color hdrColor)
    {
        if (source == null)
            return null;

        Material mat = new Material(source);

        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", hdrColor);

        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", hdrColor);
        }

        return mat;
    }


    private void Awake()
    {
        Instance = this;
    }

    public void Initialize(BuildSettings buildSettings)
    {
        settings = buildSettings;
        SetCommonColor();
    }


    private void Update()
    {
        if (isTransitioning)
            return;

        if (SongController.Instance == null || settings == null)
            return;

        float freq = SongController.Instance.GetGlobalFrequency();
        float t = Mathf.Clamp01(freq * frequencyMultiplier);

        Color targetPrimary = Color.Lerp(
            _currentPrimaryColorLight,
            _currentPrimaryColorDark,
            t
        );

        Color targetSecondary = Color.Lerp(
            _currentSecondaryColorLight,
            _currentSecondaryColorDark,
            t
        );

        CurrentPrimary = Color.Lerp(
            CurrentPrimary,
            targetPrimary,
            Time.deltaTime * colorLerpSpeed
        );

        CurrentSecondary = Color.Lerp(
            CurrentSecondary,
            targetSecondary,
            Time.deltaTime * colorLerpSpeed
        );
    }


    public void SetFreezeColor()
    {
        ApplyColorSet(
            settings.PrimaryColorFreezeLight,
            settings.PrimaryColorFreezeDark,
            settings.SecondaryColorFreezeLight,
            settings.SecondaryColorFreezeDark
        );
    }

    public void SetPowerColor()
    {
        ApplyColorSet(
            settings.PrimaryColorPowerLight,
            settings.PrimaryColorPowerDark,
            settings.SecondaryColorPowerLight,
            settings.SecondaryColorPowerDark
        );
    }

    public void SetCommonColor()
    {
        ApplyColorSet(
            settings.PrimaryColorLight,
            settings.PrimaryColorDark,
            settings.SecondaryColorLight,
            settings.SecondaryColorDark
        );
    }


    void ApplyColorSet(
        Color pLight, Color pDark,
        Color sLight, Color sDark,
        float duration = 0.2f)
    {
        isTransitioning = true;

        _currentPrimaryColorLight = pLight;
        _currentPrimaryColorDark = pDark;

        _currentSecondaryColorLight = sLight;
        _currentSecondaryColorDark = sDark;

        primaryTween?.Kill();
        secondaryTween?.Kill();

        primaryTween = DOTween.To(() => CurrentPrimary, x => CurrentPrimary = x, pLight, duration)
            .SetEase(Ease.InOutSine);

        secondaryTween = DOTween.To(() => CurrentSecondary, x => CurrentSecondary = x, sLight, duration)
            .SetEase(Ease.InOutSine);

        DOVirtual.DelayedCall(duration, () =>
        {
            isTransitioning = false;
        });
    }
}