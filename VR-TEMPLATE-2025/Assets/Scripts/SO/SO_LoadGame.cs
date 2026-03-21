using UnityEngine;

[CreateAssetMenu(fileName = "SO_LoadGame", menuName = "Scriptable Objects/SO_LoadGame")]
public class SO_LoadGame : ScriptableObject
{
    [Header("Game Type")]
    public GameType GameMode;
    [Space(10)]
    [Header("Game Music")]
    public AudioClip GameMusic;
    [Space(20)]
    [Header("Configuracao de Construcoes")]
    public BuildSettings BuildSettings;
}
[System.Serializable]
public class SceneSettings
{
    [Header("SceneMaterials")]
    public Material SkyboxMaterial;
    public Material GroundMaterial;
    public Material PlataformMaterial;
    public Material WallMaterial;
    
}
[System.Serializable]
public class BuildSettings
{
    [Header("Cores base da fase")]
    [ColorUsage(true, true)]
    public Color PrimaryColorLight;
    [ColorUsage(true, true)]
    public Color PrimaryColorDark;
    [Space(10)]
    [ColorUsage(true, true)]
    public Color SecondaryColorLight;
    [ColorUsage(true, true)]
    public Color SecondaryColorDark;
    [Space(20)]
    [Header("Configuracoes De Cena Base")]
    public SceneSettings BaseSceneSettings;
    [Space(10)]
    [Header("Cores do freeze time")]
    [ColorUsage(true, true)]
    public Color PrimaryColorFreezeLight;
    [ColorUsage(true, true)]
    public Color PrimaryColorFreezeDark;
    [Space(10)]
    [ColorUsage(true, true)]
    public Color SecondaryColorFreezeLight;
    [ColorUsage(true, true)]
    public Color SecondaryColorFreezeDark;
    [Space(20)]
    [Header("Configuracoes De Cena Freeze")]
    public SceneSettings FreezeSceneSettings;
    [Space(10)]
    [Header("Cores do power time")]
    [ColorUsage(true, true)]
    public Color PrimaryColorPowerLight;
    [ColorUsage(true, true)]
    public Color PrimaryColorPowerDark;
    [Space(10)]
    [ColorUsage(true, true)]
    public Color SecondaryColorPowerLight;
    [ColorUsage(true, true)]
    public Color SecondaryColorPowerDark;
    [Space(20)]
    [Header("Configuracoes De Cena Power")]
    public SceneSettings PowerSceneSettings;
}
public enum GameType
{
    Punch,
    Push,
    Put
};