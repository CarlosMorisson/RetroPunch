using UnityEngine;

public class BuildEnviriomments : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject prefabA;
    public GameObject prefabB;

    [Header("Parents")]
    public Transform parentA;
    public Transform parentB;

    [Header("Spawn Settings")]
    public int countA = 10;
    public int countB = 10;

    [Header("References")]
    public BuildMovemmentVisual visualScript;

    public Vector2 scaleX = new Vector2(1f, 3f);
    public Vector2 scaleY = new Vector2(1f, 3f);
    public Vector2 scaleZ = new Vector2(1f, 3f);

    public float spacing = 0f; 

    void Start()
    {
        if (prefabA) BuildRow(prefabA, parentA, countA, "Right");
        if (prefabB) BuildRow(prefabB, parentB, countB, "Left");
        prefabA.SetActive(false);
        prefabB.SetActive(false);
    }

    void BuildRow(GameObject prefab, Transform parent, int count, string side)
    {
        if (prefab == null || parent == null || count <= 0) return;

        float currentX = 0f;  

        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(prefab, parent);

            float sx = Random.Range(scaleX.x, scaleX.y);
            float sy = Random.Range(scaleY.x, scaleY.y);
            float sz = Random.Range(scaleZ.x, scaleZ.y);

            obj.transform.localScale = new Vector3(sx, sy, sz);

            float objectWidth = GetObjectWidth(obj);

            obj.transform.localPosition = new Vector3(currentX + objectWidth * 0.5f, 0, 0);

            currentX += objectWidth + spacing;
            if(side=="Right")
                visualScript.RightList.Add(obj.transform);
            if(side=="Left")
                visualScript.LeftList.Add(obj.transform);
        }
    }
    /// <summary>
    /// Coloca o material nos prefabs
    /// </summary>
    /// <param name="material"></param>
    public void SetPrefabsMaterial(Material material)
    {
        prefabA.GetComponentInChildren<MeshRenderer>().material = material;
        prefabB.GetComponentInChildren<MeshRenderer>().material = material;
    }
    float GetObjectWidth(GameObject obj)
    {
        Renderer[] rends = obj.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return 1f;

        Bounds bounds = rends[0].bounds;
        foreach (Renderer r in rends)
            bounds.Encapsulate(r.bounds);

        return bounds.size.x;
    }
}
