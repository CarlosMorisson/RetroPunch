using UnityEngine;
using System.Collections.Generic;

public class LoaderController : MonoBehaviour
{
    public static LoaderController Instance;

    [Header("Load Data")]
    [SerializeField] private SO_LoadGame LoadGame;

    [Header("Runtime")]
    public GameType GameMode;

    [Header("References")]
    [SerializeField] private SongController SongGame;
    [SerializeField] private SkyboxVisual SkyboxVisual;

    private const string PRIMARY_COLOR = "PrimaryColor";
    private const string SECONDARY_COLOR = "SecondaryColor";

    private void Awake()
    {
        Instance = this;

        GameMode = LoadGame.GameMode;

        SceneLoad();
        LoadSong();

        ApplySceneColors(); 
    }

    private void LoadSong()
    {
        SongGame.LoadSong(LoadGame.GameMusic);
    }

    private void SceneLoad()
    {
        SkyboxVisual.SetLoadMaterial(
            LoadGame.SceneSettings.SkyboxMaterial,
            LoadGame.SceneSettings.GroundMaterial,
            LoadGame.SceneSettings.PlataformMaterial
        );

        SkyboxVisual.SetObjectsMaterial(
            LoadGame.SceneSettings.PlataformMaterial,
            LoadGame.SceneSettings.WallMaterial
        );
    }

    // =========================================================
    // =========================================================
    private void ApplySceneColors()
    {
        ApplyColorByTag(PRIMARY_COLOR, LoadGame.BuildSettings.PrimaryColor);
        ApplyColorByTag(SECONDARY_COLOR, LoadGame.BuildSettings.SecondaryColor);
    }

    private void ApplyColorByTag(string tag, Color hdrColor)
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag(tag);

        foreach (GameObject obj in objs)
        {
            Renderer r = obj.GetComponent<Renderer>();
            if (r == null) continue;

            Material instancedMat = CreateMaterialWithColor(r.sharedMaterial, hdrColor);
            r.material = instancedMat;
        }
    }

    // =========================================================
    // =========================================================
    public Material GetPrimaryMaterial(Material baseMaterial)
    {
        return CreateMaterialWithColor(
            baseMaterial,
            LoadGame.BuildSettings.PrimaryColor
        );
    }

    // =========================================================
    // =========================================================
    public Material GetSecondaryMaterial(Material baseMaterial)
    {
        return CreateMaterialWithColor(
            baseMaterial,
            LoadGame.BuildSettings.SecondaryColor
        );
    }

    // =========================================================
    // =========================================================
    private Material CreateMaterialWithColor(Material source, Color hdrColor)
    {
        if (source == null) return null;

        Material mat = new Material(source);

        // Cor base
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", hdrColor);

        // Emission (HDR / Bloom)
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", hdrColor);
        }

        return mat;
    }
}
