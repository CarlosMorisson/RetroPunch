using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance;
    public static System.Action<GameObject> OnObjectReturned;

    [System.Serializable]
    public class Pool
    {
        public Transform Parent;
        public string tag;
        public GameObject prefab;
        public int size;
    }

    public List<Pool> pools;
    public Dictionary<string, Queue<GameObject>> poolDictionary;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Another ObjectPooler instance found, destroying this one.");
            Destroy(gameObject);
        }
    }

    // ObjectPooler — inicializa o pool em chunks por frame
    void Start()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        StartCoroutine(InitializePoolsAsync());
    }

    private IEnumerator InitializePoolsAsync()
    {
        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            // Instancia só 20% do tamanho configurado no início
            int initialSize = Mathf.Max(1, pool.size / 5);

            for (int i = 0; i < initialSize; i++)
            {
                if (pool.prefab != null)
                {
                    GameObject obj = Instantiate(pool.prefab, pool.Parent);
                    obj.SetActive(false);
                    objectPool.Enqueue(obj);
                }

                yield return null; // 1 objeto por frame
            }

            poolDictionary.Add(pool.tag, objectPool);
        }

    }

    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning("Pool with tag " + tag + " doesn't exist.");
            return null;
        }

        Queue<GameObject> pool = poolDictionary[tag];

        int count = pool.Count;
        for (int i = 0; i < count; i++)
        {
            GameObject obj = pool.Dequeue();

            if (!obj.activeInHierarchy)
            {
                obj.SetActive(true);
                obj.transform.position = position;
                obj.transform.rotation = rotation;
                pool.Enqueue(obj);
                return obj;
            }

            pool.Enqueue(obj);
        }

        Pool poolData = pools.Find(p => p.tag == tag);
        if (poolData != null && poolData.prefab != null)
        {
            GameObject newObj = Instantiate(poolData.prefab, poolData.Parent);
            newObj.SetActive(true);
            newObj.transform.position = position;
            newObj.transform.rotation = rotation;
            pool.Enqueue(newObj);
            Debug.Log($"Pool '{tag}' expandido dinamicamente.");
            return newObj;
        }

        return null;
    }
    public void ReturnToPool(string tag, GameObject obj)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool '{tag}' não existe.");
            return;
        }

        DOTween.Kill(obj.transform);
        foreach (Transform child in obj.GetComponentsInChildren<Transform>())
            DOTween.Kill(child);

        ResetAllRigidbodies(obj);
        ResetScale(obj);  

        obj.SetActive(false);

        OnObjectReturned?.Invoke(obj);
    }

    private void ResetScale(GameObject obj)
    {
        obj.transform.localScale = Vector3.one;
    }
    public void ResetAllRigidbodies(GameObject obj)
    {
        Rigidbody[] rigidbodies = obj.GetComponentsInChildren<Rigidbody>();

        foreach (Rigidbody rb in rigidbodies)
        {
            rb.linearVelocity = Vector3.zero; 
            rb.angularVelocity = Vector3.zero;

            rb.Sleep();
        }
    }
}