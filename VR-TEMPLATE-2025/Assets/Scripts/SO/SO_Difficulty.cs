using UnityEngine;

[CreateAssetMenu(fileName = "SO_Difficulty", menuName = "Scriptable Objects/SO_Difficulty")]
public class SO_Difficulty : ScriptableObject
{
    [Header("Spawn")]
    public float BaseSpawnRate = 1f;
    public float SpawnRateMultiplier = 1f;

    [Header("Gameplay")]
    public int ErrorTolerance = 3;

    [Header("Object Difficulty")]
    public int MaxPrefabDifficulty = 1;

    [Header("Grid")]
    public int MaxSimultaneousObjects = 3;

    [Header("Music")]
    public float MusicIntensityMultiplier = 1f;

    [Header("Pattern")]
    public float ChanceToSpawnMultipleObjects = 0f;

    [Header("Shoot Mode")]
    public float ShootSpeedMultiplier = 1f;

    [Header("Push Mode")]
    public float PushDistanceMultiplier = 1f;

    [Header("Punch Mode")]
    public float RotationSpeedMultiplier = 1f;

    [Header("Beat")]
    public int MaxSpawnPerBeat = 3;
}