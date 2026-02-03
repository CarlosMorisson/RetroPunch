using UnityEngine;

public class InstancerController : MonoBehaviour
{
    [Header("References")]
    public SongController songController;

    [Header("Prefabs (Pool IDs)")]
    public string[] prefabs;

    [Header("Spawn Settings")]
    public Transform spawnParent;

    [Tooltip("Variação SOMENTE em X e Y")]
    public Vector2 randomX = new Vector2(-5f, 5f);
    public Vector2 randomY = new Vector2(-2f, 2f);

    [Tooltip("Direção fixa do spawn (lado único)")]
    public float fixedZOffset = 0f;

    public float baseSpawnRate = 0.5f;
    public float spawnRateMultiplier = 1f;

    public float baseScale = 1f;
    public float scaleMultiplier = 2f;

    private float spawnTimer;

    private const float MAX_RATE=10;
    private const float MIN_RATE = 0.05f;

    void Update()
    {
        if (prefabs == null || prefabs.Length == 0 || songController == null || spawnParent == null)
            return;

        float freq = Mathf.Clamp01(songController.GetGlobalFrequencyMultiplicative());

        float dynamicRate = baseSpawnRate - (freq * spawnRateMultiplier);
        dynamicRate = Mathf.Clamp(dynamicRate, MIN_RATE, MAX_RATE);

        spawnTimer += Time.deltaTime;

        if (spawnTimer >= dynamicRate)
        {
            spawnTimer = 0f;
            SpawnObject(freq);
        }
    }

    void SpawnObject(float freq)
    {
        string prefabId = prefabs[Random.Range(0, prefabs.Length)];

        Vector3 pos = spawnParent.position;

        pos.x += Random.Range(randomX.x, randomX.y);
        pos.y += Random.Range(randomY.x, randomY.y);

        pos.z += fixedZOffset;

        GameObject obj = ObjectPooler.Instance.SpawnFromPool(
            prefabId,
            pos,
            spawnParent.rotation
        );

        obj.transform.SetParent(spawnParent, true);

        //float scale = baseScale + freq * scaleMultiplier;
        //obj.transform.localScale = Vector3.one * scale;
    }

    void OnDrawGizmosSelected()
    {
        if (!spawnParent) return;

        Gizmos.color = Color.cyan;

        Vector3 center = spawnParent.position + new Vector3(0, 0, fixedZOffset);
        Vector3 size = new Vector3(
            randomX.y - randomX.x,
            randomY.y - randomY.x,
            0.1f
        );

        Gizmos.DrawWireCube(center, size);
    }
}
