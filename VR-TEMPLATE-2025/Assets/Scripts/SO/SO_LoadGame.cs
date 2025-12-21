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
    [Header("Configuracoes De Cena")]
    public SceneSettings SceneSettings;
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
    [ColorUsage(true, true)]
    public Color PrimaryColor;
    [ColorUsage(true, true)]
    public Color SecondaryColor;
}
public enum GameType
{
    Punch,
    Push,
    Put
};