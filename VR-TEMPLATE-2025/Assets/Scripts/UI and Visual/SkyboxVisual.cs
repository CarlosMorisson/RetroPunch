using UnityEngine;
[System.Serializable]
public class SkyboxSettings
{
    [Header("Horizontal Strenght")]
    public float StartHorizontalStrenght;
    public float MultiplierHorizontalStrenght;
    [Space(15)]
    [Header("Horizon SkyHeight")]
    public float StartHorizontalSkyHeight;
    public float MultiplierHorizontalSkyHeight;
    [Space(15)]
    [Header("Start Density")]
    public float StartStarDensity;
    public float MultiplierStartDensity;
    [Space(15)]
    [Header("Sun Size")]
    public float StartDiscSize;
    public float MultiplierDiscSize;
    [Space(15)]
    [Header("Cor")]
    public float MinColor;
    public float MaxColor;
    [Space(15)]
    [Header("Ground")]
    public float StartGround;
    public float MultiplierGround;
}
public class SkyboxVisual : MonoBehaviour
{
    [Header("References")]
    public SongController songController;       
    public Material skyboxMaterial;
    public Material groundMaterial;
    public Material plataformMaterial;
    public MeshRenderer plataformRenderer;
    public MeshRenderer groundRenderer;

    [Header("DetailMaterial")]
    public MeshRenderer playerPlataformRenderer;
    public MeshRenderer wallRenderer;

    [Header("Frequency Response")]
    public float intensityMultiplier = 1f;      
    public float smoothSpeed = 5f;

    [Header("Skybox Settings")]
    public SkyboxSettings SkyboxSettings;
    private float smoothedFrequency;

    private Material skyboxInstance;
    private Material groundInstance;
    private Material plataformInstance;
    private Color baseSkyColor;

    private const string COLOR_NAME = "_SkyColor";

    /// <summary>
    /// Settar Novos Materials na ordem skybox, ground, plataform
    /// </summary>

    public void SetLoadMaterial(Material skybox, Material ground, Material plataform)
    {
        skyboxMaterial = skybox;
        groundMaterial = ground;
        plataformMaterial = plataform;
    }
    /// <summary>
    /// Settar Novos Materials na ordem plataform, wall
    /// </summary>
    public void SetObjectsMaterial(Material plataform, Material wall)
    {
        playerPlataformRenderer.material = plataform;
        wallRenderer.material = wall;
    }
    void Start()
    {
        if (skyboxMaterial != null)
        {
            skyboxInstance = new Material(skyboxMaterial); 
            RenderSettings.skybox = skyboxInstance;
            baseSkyColor = skyboxInstance.GetColor(COLOR_NAME);
            groundInstance = new Material(groundMaterial);
            groundRenderer.material = groundInstance;
            plataformInstance = new Material(plataformMaterial);
            plataformRenderer.material = plataformInstance;
        }
    }
    void Update()
    {
        if (songController == null || skyboxMaterial == null) return;

        float freq = songController.GetGlobalFrequencyMultiplicative();
        float target = freq * intensityMultiplier;

        smoothedFrequency = Mathf.Lerp(smoothedFrequency, target, Time.deltaTime * smoothSpeed);

        skyboxInstance.SetFloat("_HorizonStrength", SkyboxSettings.StartHorizontalStrenght + smoothedFrequency * SkyboxSettings.MultiplierHorizontalStrenght);

        skyboxInstance.SetFloat("_HorizonSkyHeight", SkyboxSettings.StartHorizontalSkyHeight + smoothedFrequency * SkyboxSettings.MultiplierHorizontalStrenght);

        skyboxInstance.SetFloat("_StarSize", SkyboxSettings.StartStarDensity + smoothedFrequency * SkyboxSettings.MultiplierStartDensity);

        skyboxInstance.SetFloat("_SunMaskSize", SkyboxSettings.StartDiscSize + smoothedFrequency * SkyboxSettings.MultiplierDiscSize);

        float colorT = Mathf.Clamp01(smoothedFrequency);

        float intensity = Mathf.Lerp(
            SkyboxSettings.MinColor,
            SkyboxSettings.MaxColor,
            colorT
        );

        Color reactiveColor = baseSkyColor * intensity;
        reactiveColor.a = baseSkyColor.a;

        skyboxInstance.SetColor(COLOR_NAME, reactiveColor);

        RenderSettings.skybox = skyboxInstance;

        groundInstance.SetVector("_GridSpeed", new Vector2(0, SkyboxSettings.StartGround + smoothedFrequency * SkyboxSettings.MultiplierGround) );

        groundRenderer.material = groundInstance;

        plataformInstance.SetVector("_GridSpeed", new Vector2(0, SkyboxSettings.StartGround + smoothedFrequency * SkyboxSettings.MultiplierGround));

        plataformRenderer.material = plataformInstance;
    }
}
