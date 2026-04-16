using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SwitchCube : MonoBehaviour
{
    [System.Serializable]
    public class ModeObject
    {
        public GameType gameType;
        public GameObject gameObject;
    }

    [Header("Game Mode Mapping")]
    [SerializeField] private List<ModeObject> modeObjects = new();

    private Dictionary<GameType, GameObject> modeDictionary;

    // Dicionário para salvar posição e rotação iniciais
    private Dictionary<GameObject, (Vector3 position, Quaternion rotation)> initialTransforms;

    [Header("Game Mode")]
    [SerializeField] private GameType _gameMode;
    public GameType GameMode
    {
        get => _gameMode;
        set
        {
            _gameMode = value;
            LoadCube();
        }
    }

    void Awake()
    {
        modeDictionary = modeObjects
            .Where(m => m.gameObject != null)
            .GroupBy(m => m.gameType)
            .ToDictionary(g => g.Key, g => g.First().gameObject);

        initialTransforms = new Dictionary<GameObject, (Vector3, Quaternion)>();
        foreach (var obj in modeDictionary.Values)
        {
            if (obj != null && !initialTransforms.ContainsKey(obj))
            {
                initialTransforms.Add(obj, (obj.transform.localPosition, obj.transform.localRotation));
            }
        }

        LoadCube();
    }

    private void Start() => GameMode = LoaderController.Instance.GameMode;

    private void OnEnable()
    {
        ResetObjectsToInitialState();
        LoadCube();
    }

    /// <summary>
    /// Retorna todos os objetos para suas posições e rotações originais
    /// </summary>
    private void ResetObjectsToInitialState()
    {
        if (initialTransforms == null) return;

        foreach (var item in initialTransforms)
        {
            GameObject obj = item.Key;
            var transformData = item.Value;

            if (obj != null)
            {
                obj.transform.localPosition = transformData.position;
                obj.transform.localRotation = transformData.rotation;

                if (obj.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }
    }

    private void LoadCube()
    {
        foreach (var obj in modeDictionary.Values)
        {
            if (obj != null)
                obj.SetActive(false);
        }

        if (modeDictionary.TryGetValue(GameMode, out GameObject match))
        {
            match.SetActive(true);
        }
    }
}