using System.Linq;
using UnityEngine;

public class ColorController : MonoBehaviour
{
    public static ColorController Instance;

    [Header("Config")]
    [SerializeField] private float colorLerpSpeed = 6f;
    [SerializeField] private float frequencyMultiplier = 1.5f;

    private ColorReactiveRenderer[] reactives;
    private BuildSettings settings;

    public Color CurrentPrimary;
    public Color CurrentSecondary;

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

        CurrentPrimary = settings.PrimaryColorLight;
        CurrentSecondary = settings.SecondaryColorLight;
    }

    private void Update()
    {
        if (SongController.Instance == null || settings == null)
            return;

        float freq = SongController.Instance.GetGlobalFrequency();
        float t = Mathf.Clamp01(freq * frequencyMultiplier);

        Color targetPrimary = Color.Lerp(
            settings.PrimaryColorLight,
            settings.PrimaryColorDark,
            t
        );

        Color targetSecondary = Color.Lerp(
            settings.SecondaryColorLight,
            settings.SecondaryColorDark,
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
}
