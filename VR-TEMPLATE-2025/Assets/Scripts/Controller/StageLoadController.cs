using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StageLoadController : MonoBehaviour
{
    public static StageLoadController Instance;
    public GameType gameType;

    private const string SHOOTER_NAME = "shooter";
    private const string PUNCHER_NAME = "puncher";
    private const string PUSHER_NAME = "pusher";

    public AudioClip ChooseMusic;
    public Difficulty difficulty;
    public bool IsTutorial;

    [Header("Stage")]
    public StageList stageList;
    public SongBeatMap CurrentBeatMap { get; private set; }

    [Header("Colors")]
    public SO_LoadGame CurrentColor;
    public List<SO_LoadGame> RandomColors;

    [Header("Difficult")]
    public SO_Difficulty CurrentDifficulty;
    public List<SO_Difficulty> Difficulties;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        //SortRandomColors();
        ResolveCurrentBeatMap();
    }

    private void ResolveCurrentBeatMap()
    {
        CurrentBeatMap = null;

        if (stageList == null)
        {
            Debug.LogError("StageList não atribuída no StageLoadController.");
            return;
        }

        if (ChooseMusic == null)
        {
            Debug.LogWarning("Nenhuma música selecionada.");
            return;
        }

        StageData stage = stageList.stages
            .FirstOrDefault(s => s.musicClip == ChooseMusic);

        if (stage == null)
        {
            Debug.LogError($"Nenhuma stage encontrada para a música '{ChooseMusic.name}'.");
            return;
        }

        StageDifficultyEntry entry = stage.difficulties
            .FirstOrDefault(d => d.difficulty == difficulty);

        if (entry == null)
        {
            Debug.LogError($"Dificuldade '{difficulty}' não encontrada em '{stage.stageName}'.");
            return;
        }

        GameModeBeatMap gameModeBeatMap = entry.gameModeBeatMaps
            .FirstOrDefault(g => g.gameType == gameType);

        if (gameModeBeatMap == null)
        {
            Debug.LogError($"GameType '{gameType}' não encontrado em '{stage.stageName}' / '{difficulty}'.");
            return;
        }

        CurrentBeatMap = gameModeBeatMap.beatMap;

        Debug.Log($"BeatMap resolvido: {stage.stageName} | {difficulty} | {gameType}");
    }

    public void HandleMusicButton(AudioClip clip)
    {
        ChooseMusic = clip;
        ResolveCurrentBeatMap();
    }

    public void SetEasy()
    {
        difficulty = Difficulty.Easy;
        CurrentDifficulty = Difficulties[(int)Difficulty.Easy];
        ResolveCurrentBeatMap();
    }

    public void SetNormal()
    {
        difficulty = Difficulty.Normal;
        CurrentDifficulty = Difficulties[(int)Difficulty.Normal];
        ResolveCurrentBeatMap();
    }

    public void SetHard()
    {
        difficulty = Difficulty.Hard;
        CurrentDifficulty = Difficulties[(int)Difficulty.Hard];
        ResolveCurrentBeatMap();
    }

    public void SortRandomColors()
    {
        int randomNumber = Random.Range(0, RandomColors.Count);
        CurrentColor = RandomColors[randomNumber];
    }

    private void OnEnable()
    {
        MenuMainButton.OnMainButtonChanged += HandleMainButtonChanged;
    }

    private void OnDisable()
    {
        MenuMainButton.OnMainButtonChanged -= HandleMainButtonChanged;
    }

    public void HandleMainButtonChanged(MainButton currentMainButton)
    {
        if (currentMainButton == null)
            return;

        switch (currentMainButton.name)
        {
            case SHOOTER_NAME: gameType = GameType.Shoot; break;
            case PUNCHER_NAME: gameType = GameType.Punch; break;
            case PUSHER_NAME: gameType = GameType.Push; break;
        }

        SortRandomColors();
    }

    public void SetTutorial() => IsTutorial = !IsTutorial;
}

public enum GameType
{
    Punch,
    Push,
    Shoot
}