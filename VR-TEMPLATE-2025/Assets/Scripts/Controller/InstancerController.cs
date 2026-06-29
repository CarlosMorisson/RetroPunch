using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InstancerController : MonoBehaviour
{
    [Header("Prefabs")]
    public List<PrefabSpawnData> prefabs = new();

    [Header("Grid")]
    public int columns = 3;
    public int rows = 3;

    public float horizontalSpacing = 2f;
    public float verticalSpacing = 2f;

    public float fixedZOffset = 0f;

    [Header("Spawn")]
    public Transform spawnParent;

    [Header("Intensity Thresholds")]
    [Range(0f, 1f)]
    public float mediumIntensityThreshold = 0.35f;

    [Range(0f, 1f)]
    public float highIntensityThreshold = 0.65f;

    [Header("Spawn Count")]
    public int lowIntensitySpawnCount = 1;
    public int mediumIntensitySpawnCount = 2;
    public int highIntensitySpawnCount = 3;

    [Header("Timing Calculation")]
    public Transform playerPosition;      // posição Z onde o cubo deve chegar
    public float cubeBoostSpeed = 10f;    // mesmo valor do CubeMovement
    public float cubeNormalSpeed = 2f;    // mesmo valor do CubeMovement
    public Transform boostEndPoint;       // mesmo ponto do CubeMovement
    public float timingOffset = 0f;

    private bool stop;

    private readonly List<Vector3> gridPositions = new();

    private readonly Dictionary<GameObject, int> objectCellMap = new();
    private readonly HashSet<int> usedCells = new();

    private string lastPrefabTag = "";

    private int lastCell = -1;

    private void Start()
    {
        GenerateGrid();
        if(StageLoadController.Instance != null)
            ApplyFromStage();
        else
            Debug.LogWarning("StageLoadController não encontrado no Awake do InstancerController.");
    }
    private void ApplyFromStage()
    {
        ApplyPrefabs();
    }

    private void ApplyPrefabs()
    {
        var stage = StageLoadController.Instance;

        if (stage.CurrentBeatMap == null)
        {
            Debug.LogWarning("CurrentBeatMap é nulo — prefabs não foram aplicados.");
            return;
        }

        // Busca o GameModeBeatMap correspondente ao gameType atual
        GameModeBeatMap gameModeBeatMap = stage.stageList.stages
            .FirstOrDefault(s => s.musicClip == stage.ChooseMusic)
            ?.difficulties
            .FirstOrDefault(d => d.difficulty == stage.difficulty)
            ?.gameModeBeatMaps
            .FirstOrDefault(g => g.gameType == stage.gameType);

        if (gameModeBeatMap == null)
        {
            Debug.LogWarning("GameModeBeatMap não encontrado para os parâmetros atuais.");
            return;
        }

        if (gameModeBeatMap.prefabSpawnData == null || gameModeBeatMap.prefabSpawnData.Count == 0)
        {
            Debug.LogWarning("PrefabSpawnData vazio para o GameModeBeatMap atual.");
            return;
        }

        prefabs = gameModeBeatMap.prefabSpawnData;

        foreach (var prefab in prefabs)
            prefab.ApplyRarity();

        Debug.Log($"[InstancerController] {prefabs.Count} prefab(s) carregado(s) do StageLoadController.");
    }

    private void OnEnable()
    {
        ObjectPooler.OnObjectReturned += HandleObjectReturned;
    }

    private void OnDisable()
    {
        ObjectPooler.OnObjectReturned -= HandleObjectReturned;
    }

    private void HandleObjectReturned(GameObject obj)
    {
        if (!objectCellMap.TryGetValue(obj, out int cell))
            return;

        objectCellMap.Remove(obj);
    }

    public void SetStop(bool value)
    {
        stop = value;
    }
    public float CalculateSpawnOffset()
    {
        if (playerPosition == null || boostEndPoint == null || spawnParent == null)
            return 0f;

        float spawnZ = spawnParent.position.z;
        float boostEndZ = boostEndPoint.position.z;
        float playerZ = playerPosition.position.z;

        // Distância percorrida em boost
        float boostDist = Mathf.Abs(boostEndZ - spawnZ);

        // Distância percorrida em velocidade normal após o boost
        float normalDist = Mathf.Abs(playerZ - boostEndZ);

        // Tempo total de viagem
        float boostTime = boostDist / cubeBoostSpeed;
        float normalTime = normalDist / cubeNormalSpeed;

        float totalTime = boostTime + normalTime + timingOffset;

        return totalTime;
    }

    public void SpawnBeat(BeatPoint beat)
    {
        if (stop) return;

        int spawnCount = GetSpawnCount(beat.intensity);

        spawnCount = Mathf.Min(
            spawnCount,
            StageLoadController.Instance.CurrentDifficulty.MaxSpawnPerBeat
        );

        if (usedCells.Count + spawnCount > gridPositions.Count)
            usedCells.Clear();

        for (int i = 0; i < spawnCount; i++)
            SpawnObject(beat);
    }

    private void SpawnObject(BeatPoint beat)
    {
        PrefabSpawnData prefab =
            GetRandomPrefab(beat.affinity);
        if (prefab == null)
            return;

        int cell = GetFreeCell();

        if (cell == -1)
            return;

        Vector3 spawnPosition =
            gridPositions[cell];

        GameObject obj =
            ObjectPooler.Instance.SpawnFromPool(
                prefab.prefabTag,
                spawnPosition,
                spawnParent.rotation
            );

        if (obj == null)
            return;


        objectCellMap[obj] = cell;

        obj.transform.SetParent(
            spawnParent,
            true
        );
        if (obj.TryGetComponent<ShootCube>(out var shootCube))
            shootCube.beat = beat;
    }

    private int GetFreeCell()
    {
        List<int> freeCells = new();

        for (int i = 0; i < gridPositions.Count; i++)
        {
            if (!usedCells.Contains(i))
                freeCells.Add(i);
        }

        Debug.Log($"FreeCells: {freeCells.Count} | UsedCells: {usedCells.Count} | Grid: {gridPositions.Count}");

        if (freeCells.Count == 0) return -1;

        int chosen = freeCells[Random.Range(0, freeCells.Count)];
        usedCells.Add(chosen);
        return chosen;
    }

    private PrefabSpawnData GetRandomPrefab(
        MusicAffinity affinity
    )
    {
        List<PrefabSpawnData> valid =
            GetValidPrefabs(affinity);

        if (valid.Count == 0)
            return null;

        List<PrefabSpawnData> filtered =
            new();

        foreach (var prefab in valid)
        {
            if (
                !prefab.allowConsecutiveSpawns &&
                prefab.prefabTag == lastPrefabTag
            )
            {
                continue;
            }

            filtered.Add(prefab);
        }

        if (filtered.Count == 0)
            filtered = valid;

        int totalWeight = 0;

        foreach (var prefab in filtered)
        {
            totalWeight += prefab.weight;
        }

        int random =
            Random.Range(0, totalWeight);

        int cumulative = 0;

        foreach (var prefab in filtered)
        {
            cumulative += prefab.weight;

            if (random < cumulative)
            {
                lastPrefabTag =
                    prefab.prefabTag;

                return prefab;
            }
        }

        return filtered[0];
    }

    private List<PrefabSpawnData> GetValidPrefabs(
        MusicAffinity affinity
    )
    {
        List<PrefabSpawnData> result =
            new();

        int maxDifficulty =
            StageLoadController.Instance
            .CurrentDifficulty
            .MaxPrefabDifficulty;

        foreach (var prefab in prefabs)
        {
            if (prefab.prefabDifficulty >
                maxDifficulty)
                continue;

            bool affinityMatch =
                prefab.affinity ==
                MusicAffinity.Any
                ||
                prefab.affinity ==
                affinity;

            if (!affinityMatch)
                continue;

            result.Add(prefab);
        }

        return result;
    }

    private int GetSpawnCount(
        float intensity
    )
    {
        if (intensity >= highIntensityThreshold)
            return highIntensitySpawnCount;

        if (intensity >= mediumIntensityThreshold)
            return mediumIntensitySpawnCount;

        return lowIntensitySpawnCount;
    }

    private void GenerateGrid()
    {
        gridPositions.Clear();

        float startX =
            -((columns - 1)
            * horizontalSpacing)
            * 0.5f;

        float startY =
            -((rows - 1)
            * verticalSpacing)
            * 0.5f;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector3 pos =
                    spawnParent.position +
                    new Vector3(
                        startX +
                        x * horizontalSpacing,
                        startY +
                        y * verticalSpacing,
                        fixedZOffset
                    );

                gridPositions.Add(pos);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnParent == null)
            return;

        Gizmos.color = Color.cyan;

        float startX =
            -((columns - 1)
            * horizontalSpacing)
            * 0.5f;

        float startY =
            -((rows - 1)
            * verticalSpacing)
            * 0.5f;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector3 pos =
                    spawnParent.position +
                    new Vector3(
                        startX +
                        x * horizontalSpacing,
                        startY +
                        y * verticalSpacing,
                        fixedZOffset
                    );

                Gizmos.DrawWireCube(
                    pos,
                    Vector3.one * 0.4f
                );
            }
        }
    }
}

[System.Serializable]
public class PrefabSpawnData
{
    public string prefabTag;

    public MusicAffinity affinity =
        MusicAffinity.Any;

    [Range(1, 10)]
    public int prefabDifficulty = 1;

    public SpawnRarity rarity = SpawnRarity.Common;

    [HideInInspector]
    public int weight = 60;

    public bool allowConsecutiveSpawns = true;

    public void ApplyRarity()
    {
        weight = rarity switch
        {
            SpawnRarity.Common => 80,
            SpawnRarity.Uncommon => 5,
            SpawnRarity.Rare => 4,
            SpawnRarity.Epic => 1,
            _ => 60
        };
    }
}
public enum SpawnRarity
{
    Common,    // weight: 60
    Uncommon,  // weight: 30
    Rare,      // weight: 8
    Epic       // weight: 2
}