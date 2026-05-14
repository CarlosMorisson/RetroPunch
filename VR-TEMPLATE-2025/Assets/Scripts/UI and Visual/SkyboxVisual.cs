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

    [Header("Start Density")]
    public float StartStarDensity;
    public float MultiplierStartDensity;

    [Header("Sun Size")]
    public float StartDiscSize;
    public float MultiplierDiscSize;

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
    public MeshRenderer groundRenderer;
    public MeshRenderer playerPlataformRenderer;
    public MeshRenderer wallRenderer;

    [Header("Frequency Response")]
    public float intensityMultiplier = 1f;
    public float smoothSpeed = 5f;

    [Header("Impact Settings")]
    public float successBoostIntensity = 0.5f; 
    public float failDeboostIntensity = -0.3f;
    public float impactReturnSpeed = 8f;
    private float currentImpactBoost = 0f;

    [Header("Skybox Settings")]
    public SkyboxSettings SkyboxSettings;

    private float smoothedFrequency;

    private SceneSettings baseScene;
    private SceneSettings freezeScene;
    private SceneSettings powerScene;

    private Material skyboxInstance;
    private Material groundInstance;
    private Material plataformInstance;
    private Material wallInstance;

    private Color baseSkyColor;

    private const string COLOR_NAME = "_SkyColor";

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

        ApplySceneInstant(baseScene);
    }

    void ApplySceneInstant(SceneSettings scene)
    {
        skyboxInstance = new Material(scene.SkyboxMaterial);
        groundInstance = new Material(scene.GroundMaterial);
        plataformInstance = new Material(scene.PlataformMaterial);
        wallInstance = new Material(scene.WallMaterial);

        RenderSettings.skybox = skyboxInstance;

        groundRenderer.material = groundInstance;
        plataformRenderer.material = plataformInstance;
        playerPlataformRenderer.material = plataformInstance;
        wallRenderer.material = wallInstance;

        baseSkyColor = skyboxInstance.GetColor(COLOR_NAME);
    }

    public void SetBaseSkybox() => SmoothTransition(baseScene);
    public void SetFreezeSkybox() => SmoothTransition(freezeScene);
    public void SetPowerSkybox() => SmoothTransition(powerScene);


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

        seq.Join(TweenMaterialColor(skyboxInstance, targetSky, "_SkyColor", duration));
        seq.Join(TweenMaterialColor(groundInstance, targetGround, "_GridColor", duration));
        seq.Join(TweenMaterialColor(groundInstance, targetGround, "_GroundColor", duration));
        seq.Join(TweenMaterialColor(plataformInstance, targetPlatform, "_GridColor", duration));
        seq.Join(TweenMaterialColor(plataformInstance, targetPlatform, "_GroundColor", duration));
        seq.Join(TweenMaterialColor(wallInstance, targetWall, "_Color", duration));

        seq.Join(TweenFloat(skyboxInstance, targetSky, "_HorizonStrength", duration));
        seq.Join(TweenFloat(skyboxInstance, targetSky, "_HorizonSkyHeight", duration));
        seq.Join(TweenFloat(skyboxInstance, targetSky, "_StarSize", duration));
        seq.Join(TweenFloat(skyboxInstance, targetSky, "_SunMaskSize", duration));

        seq.OnComplete(() =>
        {
            baseSkyColor = skyboxInstance.GetColor(COLOR_NAME);
            isTransitioning = false;
        });

        RenderSettings.skybox = skyboxInstance;
    }

    Tween TweenMaterialColor(Material current, Material target, string property, float duration)
    {
        if (!current.HasProperty(property) || !target.HasProperty(property))
            return null;

        Color start = current.GetColor(property);
        Color end = target.GetColor(property);

        return DOTween.To(() => start, x =>
        {
            start = x;
            current.SetColor(property, x);
        }, end, duration).SetEase(Ease.InOutSine);
    }

    Tween TweenFloat(Material current, Material target, string property, float duration)
    {
        if (!current.HasProperty(property) || !target.HasProperty(property))
            return null;

        float start = current.GetFloat(property);
        float end = target.GetFloat(property);

        return DOTween.To(() => start, x =>
        {
            start = x;
            current.SetFloat(property, x);
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

        float freq = songController.GetPeakFrequency();
        float target = freq * intensityMultiplier;

        smoothedFrequency = Mathf.Lerp(
            smoothedFrequency,
            target,
            Time.deltaTime * smoothSpeed
        );

        float finalReaction = smoothedFrequency + currentImpactBoost;

        skyboxInstance.SetFloat("_HorizonStrength",
            SkyboxSettings.StartHorizontalStrenght +
            finalReaction * SkyboxSettings.MultiplierHorizontalStrenght);

        skyboxInstance.SetFloat("_HorizonSkyHeight",
            SkyboxSettings.StartHorizontalSkyHeight +
            finalReaction * SkyboxSettings.MultiplierHorizontalSkyHeight);

        skyboxInstance.SetFloat("_StarSize",
            SkyboxSettings.StartStarDensity +
            finalReaction * SkyboxSettings.MultiplierStartDensity);

        skyboxInstance.SetFloat("_SunMaskSize",
            SkyboxSettings.StartDiscSize +
            finalReaction * SkyboxSettings.MultiplierDiscSize);

        float colorT = Mathf.Clamp01(finalReaction);

        float intensity = Mathf.Lerp(
            SkyboxSettings.MinColor,
            SkyboxSettings.MaxColor,
            colorT
        );

        Color reactiveColor = baseSkyColor * intensity;
        reactiveColor.a = baseSkyColor.a;

        skyboxInstance.SetColor(COLOR_NAME, reactiveColor);

        Vector2 gridSpeed = new Vector2(
            0,
            SkyboxSettings.StartGround +
            finalReaction * SkyboxSettings.MultiplierGround
        );

        groundInstance.SetVector("_GridSpeed", gridSpeed);
        plataformInstance.SetVector("_GridSpeed", gridSpeed);
    }
}