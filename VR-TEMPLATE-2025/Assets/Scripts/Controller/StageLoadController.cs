using System.Collections.Generic;
using System.Net;
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
    }
    public void SortRandomColors()
    {
        int randomNumber=Random.Range(0, RandomColors.Count);
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
    public void HandleMusicButton(AudioClip clip)=> ChooseMusic=clip;

    public void HandleMainButtonChanged(MainButton currentMainButton)
    {
        if (currentMainButton == null)
            return;
        switch (currentMainButton.name)
        {
            case SHOOTER_NAME:
                gameType = GameType.Shoot;
                break;
            case PUNCHER_NAME:
                gameType = GameType.Punch;
                break;
            case PUSHER_NAME:
                gameType=GameType.Push;
                break;
        }
        SortRandomColors();
        print(currentMainButton.name);
    }
    public void SetEasy()
    {
        difficulty = Difficulty.Easy;
        CurrentDifficulty=Difficulties[(int)Difficulty.Easy];
    }
    public void SetNormal()
    {
        difficulty = Difficulty.Normal;
        CurrentDifficulty = Difficulties[(int)Difficulty.Normal];
    }
    public void SetHard()
    {
        difficulty = Difficulty.Hard;
        CurrentDifficulty = Difficulties[(int)Difficulty.Hard];
    }
    public void SetTutorial() =>IsTutorial=!IsTutorial;
}
public enum GameType
{
    Punch,
    Push,
    Shoot
};