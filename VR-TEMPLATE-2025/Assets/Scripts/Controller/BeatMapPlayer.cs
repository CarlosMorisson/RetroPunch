using UnityEngine;

public class BeatMapPlayer : MonoBehaviour
{
    public SongController songController;
    public SongBeatMap beatMap;

    public InstancerController instancer;

    public float spawnOffset = 1.5f;

    private int currentBeat;

    private void Start()
    {
        beatMap=StageLoadController.Instance.CurrentBeatMap;
    }
    void Update()
    {
        if (songController == null) return;
        if (songController.audioSource == null) return;
        if (!songController.LoadMusic) return;

        float currentTime = songController.audioSource.time;

        if (currentBeat >= beatMap.beats.Count) return;

        // Se ainda não inicializou, pula todos os beats passados
        if (currentBeat == 0 && currentTime > 0f)
        {
            while (
                currentBeat < beatMap.beats.Count &&
                beatMap.beats[currentBeat].time < currentTime
            )
            {
                currentBeat++;
            }
        }

        BeatPoint next = beatMap.beats[currentBeat];

        if (currentTime >= next.time - instancer.CalculateSpawnOffset())
        {
            instancer.SpawnBeat(next);
            currentBeat++;
        }
    }
}