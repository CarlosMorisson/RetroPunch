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

        GameMode = StageLoadController.Instance.gameType;
        LoadGame=StageLoadController.Instance.CurrentColor;

        SceneLoad();

        ColorController.Instance.Initialize(LoadGame.BuildSettings);
    }

    public void LoadSong()
    {
        SongGame.LoadSong(StageLoadController.Instance.ChooseMusic);
    }

    private void SceneLoad()
    {
        SkyboxVisual.InitializeSceneMaterials(
            LoadGame.BuildSettings.BaseSceneSettings,
            LoadGame.BuildSettings.FreezeSceneSettings,
            LoadGame.BuildSettings.PowerSceneSettings
        );
    }

}
