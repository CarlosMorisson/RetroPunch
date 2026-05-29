using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_Difficulty", menuName = "Scriptable Objects/SO_Difficulty")]
public class SO_Difficulty : ScriptableObject
{
    public float BaseSpawnRate;
    public float SpawnRateMultiplier;
    public int ErrorTolerance;
}
