using UnityEngine;

public class BeatMapPlayer : MonoBehaviour
{
    public SongController songController;
    public SongBeatMap beatMap;
    public InstancerController instancer;

    private int currentBeat;
    private bool skipDone;

    private void Start()
    {
        currentBeat = 0;
        skipDone = false;
        beatMap = StageLoadController.Instance.CurrentBeatMap;
    }

    private void Update()
    {
        if (songController == null) return;
        if (songController.audioSource == null) return;
        if (beatMap == null) return;
        if (currentBeat >= beatMap.beats.Count) return;

        if (!skipDone)
        {
            if (songController.audioSource.clip == null) return;

            skipDone = true;
            float offset = instancer.CalculateSpawnOffset();

            int skipped = 0;
            while (
                currentBeat < beatMap.beats.Count &&
                beatMap.beats[currentBeat].time - offset <= 0f
            )
            {
                currentBeat++;
                skipped++;
            }

            if (skipped > 0)
                Debug.Log($"[BeatMapPlayer] {skipped} beat(s) ignorados (time - offset <= 0).");

            return;
        }

        if (!songController.LoadMusic) return;

        float currentTime = songController.audioSource.time;
        float spawnOffset = instancer.CalculateSpawnOffset();

        BeatPoint next = beatMap.beats[currentBeat];

        if (currentTime >= next.time - spawnOffset)
        {
            Debug.Log($"[SPAWN] frame={Time.frameCount} | currentTime={currentTime:F3} | beatTime={next.time:F3} | offset={spawnOffset:F3} | beat={currentBeat}");
            instancer.SpawnBeat(next);
            currentBeat++;
        }
    }

    public void Restart()
    {
        currentBeat = 0;
        skipDone = false;
        beatMap = StageLoadController.Instance.CurrentBeatMap;
    }
}