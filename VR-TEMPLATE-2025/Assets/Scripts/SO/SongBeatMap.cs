using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SongBeatMap",
    menuName = "Rhythm/Song Beat Map"
)]
public class SongBeatMap : ScriptableObject
{
    public AudioClip audioClip;

    public List<BeatPoint> beats = new();
}
[System.Serializable]
public class BeatPoint
{
    public float time;

    [Range(0f, 1f)]
    public float intensity;

    public MusicAffinity affinity;
}
public enum MusicAffinity
{
    Bass,
    Mid,
    Treble,
    Any
}