using DG.Tweening;
using UnityEngine;

public class ColorController : MonoBehaviour
{
    public static ColorController Instance;

    [Header("Config")]
    [SerializeField] private float colorLerpSpeed = 6f;
    [SerializeField] private float frequencyMultiplier = 1.5f;

    [Header("Impact Settings")]
    [Tooltip("Intensidade do brilho no acerto (Valores altos como 2.0+ brilham mais)")]
    public float successFlashIntensity = 1.5f;

    [Tooltip("Intensidade do escurecimento no erro (Valores negativos como -0.8f)")]
    public float failDimIntensity = -0.7f;

    [SerializeField] private float flashReturnSpeed = 8f;

    private float currentFlashIntensity = 0f;

    [Header("Current State")]
    public Color CurrentPrimary;
    public Color CurrentSecondary;

    public Color PrimaryFreezeColor;
    public Color SecondaryFreezeColor;

    public Color PrimaryPowerColor;
    public Color SecondaryPowerColor;

    private BuildSettings settings;
    private Color _currentPrimaryColorLight;
    private Color _currentPrimaryColorDark;
    private Color _currentSecondaryColorLight;
    private Color _currentSecondaryColorDark;

    private Tween primaryTween;
    private Tween secondaryTween;
    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Metodo para chamar no evento de SUCESSO.
    /// Usa a variavel successFlashIntensity definida no Inspector.
    /// </summary>
    public void TriggerSuccessFlash()
    {
        currentFlashIntensity = successFlashIntensity;
    }

    /// <summary>
    /// Metodo para chamar no evento de FALHA.
    /// Usa a variavel failDimIntensity definida no Inspector.
    /// </summary>
    public void TriggerFailDim()
    {
        currentFlashIntensity = failDimIntensity;
    }

    public void Initialize(BuildSettings buildSettings)
    {
        settings = buildSettings;
        SetCommonColor();

        PrimaryFreezeColor = settings.PrimaryColorFreezeLight;
        SecondaryFreezeColor = settings.SecondaryColorFreezeLight;

        PrimaryPowerColor   = settings.PrimaryColorPowerLight;
        SecondaryPowerColor = settings.SecondaryColorPowerLight;
    }

    private void Update()
    {
        currentFlashIntensity = Mathf.Lerp(currentFlashIntensity, 0f, Time.deltaTime * flashReturnSpeed);

        if (isTransitioning || SongController.Instance == null || settings == null)
            return;

        float freq = SongController.Instance.GetGlobalFrequency();
        float t = Mathf.Clamp01(freq * frequencyMultiplier);

        Color targetPrimary = Color.Lerp(_currentPrimaryColorLight, _currentPrimaryColorDark, t);
        Color targetSecondary = Color.Lerp(_currentSecondaryColorLight, _currentSecondaryColorDark, t);

        Color basePrimary = Color.Lerp(CurrentPrimary, targetPrimary, Time.deltaTime * colorLerpSpeed);
        Color baseSecondary = Color.Lerp(CurrentSecondary, targetSecondary, Time.deltaTime * colorLerpSpeed);

        CurrentPrimary = basePrimary + (Color.white * currentFlashIntensity);
        CurrentSecondary = baseSecondary + (Color.white * currentFlashIntensity);
    }

    public Material GetPrimaryMaterial(Material baseMaterial)
    {
        return CreateMaterialWithColor(baseMaterial, CurrentPrimary);
    }

    private Material CreateMaterialWithColor(Material source, Color hdrColor)
    {
        if (source == null) return null;

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

    public void SetFreezeColor() => ApplyColorSet(settings.PrimaryColorFreezeLight, settings.PrimaryColorFreezeDark, settings.SecondaryColorFreezeLight, settings.SecondaryColorFreezeDark);
    public void SetPowerColor() => ApplyColorSet(settings.PrimaryColorPowerLight, settings.PrimaryColorPowerDark, settings.SecondaryColorPowerLight, settings.SecondaryColorPowerDark);
    public void SetCommonColor() => ApplyColorSet(settings.PrimaryColorLight, settings.PrimaryColorDark, settings.SecondaryColorLight, settings.SecondaryColorDark);

    void ApplyColorSet(Color pLight, Color pDark, Color sLight, Color sDark, float duration = 0.2f)
    {
        isTransitioning = true;

        _currentPrimaryColorLight = pLight;
        _currentPrimaryColorDark = pDark;
        _currentSecondaryColorLight = sLight;
        _currentSecondaryColorDark = sDark;

        primaryTween?.Kill();
        secondaryTween?.Kill();

        primaryTween = DOTween.To(() => CurrentPrimary, x => CurrentPrimary = x, pLight, duration).SetEase(Ease.InOutSine);
        secondaryTween = DOTween.To(() => CurrentSecondary, x => CurrentSecondary = x, sLight, duration).SetEase(Ease.InOutSine);

        DOVirtual.DelayedCall(duration, () => isTransitioning = false);
    }
}