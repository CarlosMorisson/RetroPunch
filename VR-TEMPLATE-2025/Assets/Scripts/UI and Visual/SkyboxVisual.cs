using System.Collections;
using UnityEngine;
using DG.Tweening;

[System.Serializable]
public class SkyboxSettings
{
    [Header("Horizontal Strenght")]
    public float StartHorizontalStrenght;
    public float MultiplierHorizontalStrenght;

    [Header("Horizon SkyHeight")]
    public float StartHorizontalSkyHeight;
    public float MultiplierHorizontalSkyHeight;

    [Header("Star Size")]
    public float StartStarDensity;
    public float MultiplierStartDensity;

    [Header("Star Density (_StarDensity)")]
    public float StartStarDensityAmount = 15f;
    public float MultiplierStarDensityAmount = 5f;

    [Header("Sun Mask Size")]
    public float StartDiscSize;
    public float MultiplierDiscSize;

    [Header("Sun Size (_SunSize)")]
    public float StartSunSize = 0.5f;
    public float MultiplierSunSize = 0.15f;

    [Header("Cor")]
    public float MinColor;
    public float MaxColor;

    [Header("Ground")]
    public float StartGround;
    public float MultiplierGround;
}

public class SkyboxVisual : MonoBehaviour
{
    public static SkyboxVisual Instance;
    [Header("References")]
    public SongController songController;

    [Header("Renderers")]
    public MeshRenderer plataformRenderer;

    [Header("Ground Renderers")]
    public MeshRenderer groundRenderer1; // Antigo groundRenderer
    public MeshRenderer groundRenderer2; // Adicionado
    public MeshRenderer groundRenderer3; // Adicionado

    [Header("Other Renderers")]
    public MeshRenderer playerPlataformRenderer;
    public MeshRenderer wallRenderer;

    [Header("Frequency Response")]
    public float intensityMultiplier = 1f;
    public float smoothSpeed = 5f;

    [Header("Reaction Limit")]
    [Tooltip("Limita o quanto a reação da música (grave/agudo) pode empurrar todos os parâmetros do shader do skybox, evitando deformação excessiva em músicas muito intensas.")]
    [Range(0f, 5f)] public float maxReactionIntensity = 2f;

    [Header("Auto Normalization (AGC)")]
    [Tooltip("Velocidade com que o pico recente de cada banda (grave/médio/agudo) decai, permitindo que a reação sempre acompanhe a dinâmica da música atual em vez de saturar no limite. Valores maiores fazem a normalização se readaptar mais rápido a mudanças de volume/intensidade da música.")]
    public float agcDecayRate = 0.3f;

    [Header("Impact Settings")]
    public float successBoostIntensity = 0.5f;
    public float failDeboostIntensity = -0.3f;
    public float impactReturnSpeed = 8f;
    private float currentImpactBoost = 0f;

    [Header("Skybox Settings")]
    public SkyboxSettings SkyboxSettings;

    private float smoothedBass;
    private float smoothedMid;
    private float smoothedTreble;

    private float bassPeak = 0.0001f;
    private float midPeak = 0.0001f;
    private float treblePeak = 0.0001f;

    private SceneSettings baseScene;
    private SceneSettings freezeScene;
    private SceneSettings powerScene;
    private SceneSettings opposideScene;

    private Material skyboxInstance;
    private Material groundInstance; 
    private Material plataformInstance;
    private Material wallInstance;

    private Color baseSkyColor;

    private static readonly int ID_SkyColor = Shader.PropertyToID("_SkyColor");
    private static readonly int ID_GridColor = Shader.PropertyToID("_GridColor");
    private static readonly int ID_GroundColor = Shader.PropertyToID("_GroundColor");
    private static readonly int ID_HorizonStrength = Shader.PropertyToID("_HorizonStrength");
    private static readonly int ID_HorizonHeight = Shader.PropertyToID("_HorizonHeight");
    private static readonly int ID_StarSize = Shader.PropertyToID("_StarSize");
    private static readonly int ID_StarDensity = Shader.PropertyToID("_StarDensity");
    private static readonly int ID_SunMaskSize = Shader.PropertyToID("_SunMaskSize");
    private static readonly int ID_SunSize = Shader.PropertyToID("_SunSize");
    private static readonly int ID_GridSpeed = Shader.PropertyToID("_GridSpeed");

    private bool isTransitioning = false;

    private void Awake()
    {
        Instance = this;
    }

    public void TriggerSuccessBoost()
    {
        if (isTransitioning) return;
        currentImpactBoost = successBoostIntensity;
    }

    public void TriggerFailDeboost()
    {
        if (isTransitioning) return;
        currentImpactBoost = failDeboostIntensity;
    }

    public void InitializeSceneMaterials(
        SceneSettings baseSceneSettings,
        SceneSettings freezeSceneSettings,
        SceneSettings powerSceneSettings)
    {
        baseScene = baseSceneSettings;
        freezeScene = freezeSceneSettings;
        powerScene = powerSceneSettings;
        opposideScene = CreateOpposideScene(baseScene);

        ApplySceneInstant(baseScene);
    }

    void ApplySceneInstant(SceneSettings scene)
    {
        skyboxInstance = new Material(scene.SkyboxMaterial);
        groundInstance = new Material(scene.GroundMaterial);
        plataformInstance = new Material(scene.PlataformMaterial);
        wallInstance = new Material(scene.WallMaterial);

        RenderSettings.skybox = skyboxInstance;

        if (groundRenderer1 != null) groundRenderer1.material = groundInstance;
        if (groundRenderer2 != null) groundRenderer2.material = groundInstance;
        if (groundRenderer3 != null) groundRenderer3.material = groundInstance;

        if (plataformRenderer != null) plataformRenderer.material = plataformInstance;
        if (playerPlataformRenderer != null) playerPlataformRenderer.material = plataformInstance;
        if (wallRenderer != null) wallRenderer.material = wallInstance;

        baseSkyColor = skyboxInstance.GetColor(ID_SkyColor);
    }

    public void SetBaseSkybox() => SmoothTransition(baseScene);
    public void SetFreezeSkybox() => SmoothTransition(freezeScene);
    public void SetPowerSkybox() => SmoothTransition(powerScene);
    [ContextMenu("Opposide Color")]
    public void SetOpposideSkybox() => SmoothTransition(opposideScene);

    // Cria uma copia da cena base com as cores invertidas no espectro (matiz + 180 graus),
    // igual ao isOpositeColor do ColorReactiveRenderer
    SceneSettings CreateOpposideScene(SceneSettings source)
    {
        SceneSettings opposide = new SceneSettings
        {
            SkyboxMaterial = CreateOpposideMaterial(source.SkyboxMaterial),
            GroundMaterial = CreateOpposideMaterial(source.GroundMaterial),
            PlataformMaterial = CreateOpposideMaterial(source.PlataformMaterial),
            WallMaterial = CreateOpposideMaterial(source.WallMaterial)
        };
        return opposide;
    }

    Material CreateOpposideMaterial(Material source)
    {
        Material mat = new Material(source);

        InvertColorProperty(mat, ID_SkyColor);
        InvertColorProperty(mat, ID_GridColor);
        InvertColorProperty(mat, ID_GroundColor);

        return mat;
    }

    void InvertColorProperty(Material mat, int propertyId)
    {
        if (mat.HasProperty(propertyId))
            mat.SetColor(propertyId, ColorController.GetComplementaryColor(mat.GetColor(propertyId)));
    }

    void SmoothTransition(SceneSettings target, float duration = 0.2f)
    {
        DOTween.Kill(this);

        isTransitioning = true;
        currentImpactBoost = 0;

        Material targetSky = target.SkyboxMaterial;
        Material targetGround = target.GroundMaterial;
        Material targetPlatform = target.PlataformMaterial;
        Material targetWall = target.WallMaterial;

        CopyTextures(targetSky, skyboxInstance);
        CopyTextures(targetGround, groundInstance);
        CopyTextures(targetPlatform, plataformInstance);
        CopyTextures(targetWall, wallInstance);

        Sequence seq = DOTween.Sequence().SetTarget(this);

        seq.Join(TweenMaterialColor(skyboxInstance, targetSky, ID_SkyColor, duration));
        seq.Join(TweenMaterialColor(groundInstance, targetGround, ID_GridColor, duration));
        seq.Join(TweenMaterialColor(groundInstance, targetGround, ID_GroundColor, duration));
        seq.Join(TweenMaterialColor(plataformInstance, targetPlatform, ID_GridColor, duration));
        seq.Join(TweenMaterialColor(plataformInstance, targetPlatform, ID_GroundColor, duration));

        seq.Join(TweenFloat(skyboxInstance, targetSky, ID_HorizonStrength, duration));
        seq.Join(TweenFloat(skyboxInstance, targetSky, ID_HorizonHeight, duration));
        seq.Join(TweenFloat(skyboxInstance, targetSky, ID_StarSize, duration));
        seq.Join(TweenFloat(skyboxInstance, targetSky, ID_StarDensity, duration));
        seq.Join(TweenFloat(skyboxInstance, targetSky, ID_SunMaskSize, duration));
        seq.Join(TweenFloat(skyboxInstance, targetSky, ID_SunSize, duration));

        seq.OnComplete(() =>
        {
            baseSkyColor = skyboxInstance.GetColor(ID_SkyColor);
            isTransitioning = false;
        });

        RenderSettings.skybox = skyboxInstance;
    }

    Tween TweenMaterialColor(Material current, Material target, int propertyId, float duration)
    {
        if (!current.HasProperty(propertyId) || !target.HasProperty(propertyId))
            return null;

        Color start = current.GetColor(propertyId);
        Color end = target.GetColor(propertyId);

        return DOTween.To(() => start, x =>
        {
            start = x;
            current.SetColor(propertyId, x);
        }, end, duration).SetEase(Ease.InOutSine);
    }

    Tween TweenFloat(Material current, Material target, int propertyId, float duration)
    {
        if (!current.HasProperty(propertyId) || !target.HasProperty(propertyId))
            return null;

        float start = current.GetFloat(propertyId);
        float end = target.GetFloat(propertyId);

        return DOTween.To(() => start, x =>
        {
            start = x;
            current.SetFloat(propertyId, x);
        }, end, duration).SetEase(Ease.InOutSine);
    }

    void CopyTextures(Material from, Material to)
    {
        foreach (var name in from.GetTexturePropertyNames())
        {
            to.SetTexture(name, from.GetTexture(name));
        }
    }

    void Update()
    {
        currentImpactBoost = Mathf.Lerp(currentImpactBoost, 0f, Time.deltaTime * impactReturnSpeed);

        if (isTransitioning)
            return;

        if (songController == null || skyboxInstance == null)
            return;

        MusicAnalysis analysis = songController.GetSongAnalysis();
        if (analysis == null)
            return;

        smoothedBass = Mathf.Lerp(
            smoothedBass,
            analysis.Bass * intensityMultiplier,
            Time.deltaTime * smoothSpeed
        );

        smoothedMid = Mathf.Lerp(
            smoothedMid,
            analysis.Mid * intensityMultiplier,
            Time.deltaTime * smoothSpeed
        );

        smoothedTreble = Mathf.Lerp(
            smoothedTreble,
            analysis.Treble * intensityMultiplier,
            Time.deltaTime * smoothSpeed
        );

        float decay = Mathf.Exp(-agcDecayRate * Time.deltaTime);
        bassPeak = Mathf.Max(smoothedBass, Mathf.Max(bassPeak * decay, 0.0001f));
        midPeak = Mathf.Max(smoothedMid, Mathf.Max(midPeak * decay, 0.0001f));
        treblePeak = Mathf.Max(smoothedTreble, Mathf.Max(treblePeak * decay, 0.0001f));

        float bassNorm = smoothedBass / bassPeak;
        float midNorm = smoothedMid / midPeak;
        float trebleNorm = smoothedTreble / treblePeak;

        float bassReaction = Mathf.Clamp(bassNorm * maxReactionIntensity + currentImpactBoost, -maxReactionIntensity, maxReactionIntensity);
        float midReaction = Mathf.Clamp(midNorm * maxReactionIntensity + currentImpactBoost, -maxReactionIntensity, maxReactionIntensity);
        float trebleReaction = Mathf.Clamp(trebleNorm * maxReactionIntensity + currentImpactBoost, -maxReactionIntensity, maxReactionIntensity);

        skyboxInstance.SetFloat(ID_HorizonStrength,
            SkyboxSettings.StartHorizontalStrenght +
            bassReaction * SkyboxSettings.MultiplierHorizontalStrenght);

        skyboxInstance.SetFloat(ID_HorizonHeight,
            SkyboxSettings.StartHorizontalSkyHeight +
            midReaction * SkyboxSettings.MultiplierHorizontalSkyHeight);

        skyboxInstance.SetFloat(ID_StarSize,
            SkyboxSettings.StartStarDensity +
            trebleReaction * SkyboxSettings.MultiplierStartDensity);

        skyboxInstance.SetFloat(ID_StarDensity,
            SkyboxSettings.StartStarDensityAmount +
            trebleReaction * SkyboxSettings.MultiplierStarDensityAmount);

        skyboxInstance.SetFloat(ID_SunMaskSize,
            SkyboxSettings.StartDiscSize +
            trebleReaction * SkyboxSettings.MultiplierDiscSize);

        skyboxInstance.SetFloat(ID_SunSize,
            SkyboxSettings.StartSunSize +
            trebleReaction * SkyboxSettings.MultiplierSunSize);

        float colorT = Mathf.Clamp01(midReaction);

        float intensity = Mathf.Lerp(
            SkyboxSettings.MinColor,
            SkyboxSettings.MaxColor,
            colorT
        );

        Color reactiveColor = baseSkyColor * intensity;
        reactiveColor.a = baseSkyColor.a;

        skyboxInstance.SetColor(ID_SkyColor, reactiveColor);

        Vector2 gridSpeed = new Vector2(
            0,
            SkyboxSettings.StartGround +
            bassReaction * SkyboxSettings.MultiplierGround
        );

        if (groundInstance != null)
        {
            groundInstance.SetVector(ID_GridSpeed, gridSpeed);
        }

        if (plataformInstance != null)
        {
            plataformInstance.SetVector(ID_GridSpeed, gridSpeed);
        }
    }
}