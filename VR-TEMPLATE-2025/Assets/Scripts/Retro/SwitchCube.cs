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

        LoadCube();
    }
    private void Start()=> GameMode = LoaderController.Instance.GameMode;
    private void OnEnable() => LoadCube();
    private void LoadCube()
    {
        foreach (var obj in modeDictionary.Values)
        {
            if (obj != null)
                obj.SetActive(false);
        }

        var match = modeDictionary
            .Where(kv => kv.Key == GameMode)
            .Select(kv => kv.Value)
            .FirstOrDefault();

        if (match != null)
        {
            match.SetActive(true);
        }
    }
}
