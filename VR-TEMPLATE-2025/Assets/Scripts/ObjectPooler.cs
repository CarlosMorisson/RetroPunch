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

    [Header("Watchdog")]
    [Tooltip("Objeto ativo há mais que isso (s, tempo de jogo) é considerado preso: loga o estado e força a volta ao pool. 0 = desligado. Deve ser maior que o CubeLifeTime + tempo de viagem.")]
    [SerializeField] private float maxAliveTime = 45f;
    private const float WATCHDOG_INTERVAL = 1f;

    // Objetos fora do pool: tag e tempo de jogo acumulado desde o spawn (pausa não conta)
    private readonly Dictionary<GameObject, (string tag, float alive)> spawnedAt = new();
    private readonly List<GameObject> watchdogBuffer = new();
    private float lastWatchdogCheck;

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
            return;
        }

        DisableSceneTemplates();
    }

    // Os "prefabs" dos pools sao objetos da cena. Se ficarem ativos eles jogam como um cubo normal
    // (andam, falham, encolhem o filho pra scale 0 e sao desativados), e todo Instantiate feito
    // depois clona esse estado quebrado: o cubo nasce com o filho em scale 0 (objeto "vazio").
    // Desativando aqui, o template fica intacto e serve apenas de molde.
    private void DisableSceneTemplates()
    {
        foreach (Pool pool in pools)
        {
            if (pool.prefab != null && pool.prefab.scene.IsValid())
                pool.prefab.SetActive(false);
        }
    }

    // ObjectPooler � inicializa o pool em chunks por frame
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

            // Instancia s� 20% do tamanho configurado no in�cio
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

    // Rede de segurança contra cubos que nunca voltam ao pool. Loga o estado do objeto preso
    // (para achar a causa) e o devolve, para ele não ficar ocupando o pool até o fim da música.
    private void Update()
    {
        if (maxAliveTime <= 0f || Time.time - lastWatchdogCheck < WATCHDOG_INTERVAL)
            return;

        float elapsed = Time.time - lastWatchdogCheck;
        lastWatchdogCheck = Time.time;

        // Pausado/fim de jogo: os cubos param de propósito, então o tempo não conta
        if (GameState.Instance != null && GameState.Instance.CurrentState != State.Game)
            return;

        watchdogBuffer.Clear();
        watchdogBuffer.AddRange(spawnedAt.Keys);

        foreach (GameObject obj in watchdogBuffer)
        {
            // Destruído ou desativado por fora do ReturnToPool: já está livre no pool
            if (obj == null || !obj.activeSelf)
            {
                spawnedAt.Remove(obj);
                continue;
            }

            (string tag, float alive) = spawnedAt[obj];
            alive += elapsed;
            spawnedAt[obj] = (tag, alive);

            if (alive < maxAliveTime)
                continue;

            Debug.LogWarning($"[ObjectPooler] '{obj.name}' (pool '{tag}') preso há {alive:F0}s. Forçando volta ao pool.\n{DescribeState(obj)}", obj);
            ReturnToPool(tag, obj);
        }
    }

    private static string DescribeState(GameObject obj)
    {
        System.Text.StringBuilder sb = new();
        sb.Append($"root pos={obj.transform.position} scale={obj.transform.localScale}");

        if (obj.TryGetComponent<CubeMovemment>(out var move))
            sb.Append($" normalSpeed={move.normalSpeed} enableSpeed={move.enableSpeed} boostFinished={move.boostFinished}");

        foreach (Transform child in obj.transform)
            sb.Append($"\n  {child.name}: activeSelf={child.gameObject.activeSelf} scale={child.localScale} tweens={DOTween.TweensByTarget(child)?.Count ?? 0}");

        return sb.ToString();
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
                DisableBreakCubes(obj);
                obj.SetActive(true);
                obj.transform.position = position;
                obj.transform.rotation = rotation;
                pool.Enqueue(obj);
                spawnedAt[obj] = (tag, 0f);
                return obj;
            }

            pool.Enqueue(obj);
        }

        Pool poolData = pools.Find(p => p.tag == tag);
        if (poolData != null && poolData.prefab != null)
        {
            GameObject newObj = Instantiate(poolData.prefab, poolData.Parent);
            DisableBreakCubes(newObj);
            newObj.SetActive(true);
            newObj.transform.position = position;
            newObj.transform.rotation = rotation;
            pool.Enqueue(newObj);
            spawnedAt[newObj] = (tag, 0f);
            Debug.Log($"Pool '{tag}' expandido dinamicamente.");
            return newObj;
        }

        return null;
    }
    public void ReturnToPool(string tag, GameObject obj)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool '{tag}' n�o existe.");
            return;
        }

        // Ja esta no pool: evita liberar de novo a celula de grid (OnObjectReturned) de outro cubo.
        if (!obj.activeSelf)
            return;

        // Inclui filhos inativos: o cubo de sucesso já está desativado aqui, e tweens nele/nos fragmentos
        // continuariam rodando por cima do próximo uso do objeto.
        DOTween.Kill(obj.transform);
        foreach (Transform child in obj.GetComponentsInChildren<Transform>(true))
            DOTween.Kill(child);

        spawnedAt.Remove(obj);

        ResetAllRigidbodies(obj);
        ResetScale(obj);
        DisableBreakCubes(obj);

        obj.SetActive(false);

        OnObjectReturned?.Invoke(obj);
    }

    // O BreakCube é irmão do cubo e só se desativa sozinho ao fim da própria coroutine (~6s).
    // Se o cubo volta ao pool antes disso, a coroutine morre e ele fica com activeSelf = true,
    // reaparecendo no próximo spawn. Desativar aqui (com o root inativo) é seguro e não depende
    // da ordem dos OnEnable dos filhos.
    private void DisableBreakCubes(GameObject obj)
    {
        foreach (BreakCube breakCube in obj.GetComponentsInChildren<BreakCube>(true))
        {
            if (breakCube.gameObject.activeSelf)
                breakCube.gameObject.SetActive(false);
        }
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