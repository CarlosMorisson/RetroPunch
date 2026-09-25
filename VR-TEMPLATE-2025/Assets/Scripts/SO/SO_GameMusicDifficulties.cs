using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "StageList",
    menuName = "Rhythm/Stage List"
)]
public class StageList : ScriptableObject
{
    public List<StageData> stages = new();
}

[System.Serializable]
public class StageData
{
    public string stageName;
    public AudioClip musicClip;
    public List<StageDifficultyEntry> difficulties = new();
}

[System.Serializable]
public class StageDifficultyEntry
{
    public Difficulty difficulty;
    public List<GameModeBeatMap> gameModeBeatMaps = new();

}
[System.Serializable]
public class GameModeBeatMap
{
    public GameType gameType;
    public SongBeatMap beatMap;
    public List<PrefabSpawnData> prefabSpawnData;
}