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

    private void Awake()
    {
        Instance = this;

        GameMode = LoadGame.GameMode;

        SceneLoad();
        LoadSong();

        ColorController.Instance.Initialize(LoadGame.BuildSettings);
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

}
