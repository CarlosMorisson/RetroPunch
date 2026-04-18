using System;
using UnityEngine;
using UnityEngine.Events;

public class GameState : MonoBehaviour
{
    public static GameState Instance;
    public enum State
    {
        Game,
        Menu,
        Pause,
        End
    };
    [Header("State da cena")]
    public State SceneState;
    public static event Action<State> OnGameStateChanged;
    [Header("State Atual")]
    [SerializeField]
    private State currentState;
    public State CurrentState
    {
        get => currentState;
        set
        {
            if (currentState != value)
            {
                currentState = value;
                OnGameStateChanged?.Invoke(currentState);
            }
        }
    }
    public bool _gameStarted { private get; set; }
    public enum Difficulty
    {
        Easy,
        Hard
    };
    public Difficulty difficulty;
    [Space(10f)]
    public UnityEvent OnMenuEvent;
    public UnityEvent OnGameEvent;
    public UnityEvent OnPauseEvent;
    public UnityEvent OnEndEvent;


    void Awake()
    {
        Instance = this;

    }
    void OnEnable()
    {
        OnGameStateChanged += GameStateChanged;
    }

    void OnDisable()
    {
        OnGameStateChanged -= GameStateChanged;
    }
    //Metodo de seleção de dificuldade
    #region Seleção de dificuldade
    public void SetEasy() => difficulty = Difficulty.Easy;
    public void SetHard() => difficulty = Difficulty.Hard;
    #endregion
    //Metodo De troca de State
    #region Metodo de troca Game State
    public void GameStateGame() => CurrentState = State.Game;

    public void GameStateMenu() => CurrentState = State.Menu;

    public void GameStatePause() => CurrentState = State.Pause;
    public void GameStateEnd() => CurrentState = State.End;

    #endregion
    void GameStateChanged(State newState)
    {
        Debug.Log("Novo estado do jogo: " + newState);
        switch (newState)
        {
            case State.Game:
                OnGameEvent.Invoke();
                break;
            case State.Pause:
                OnPauseEvent.Invoke();
                break;
            case State.Menu:
                OnMenuEvent.Invoke();
                break;
            case State.End:
                OnEndEvent.Invoke();
                break;
        }
    }
}
