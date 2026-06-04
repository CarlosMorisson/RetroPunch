
using UnityEngine;
using System;
using UnityEngine.Events;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;

public class PointController : MonoBehaviour
{
    public static PointController Instance;
    #region Acertos
    private int _accept;
    public int Accept
    {
        get => _accept;
        set
        {
            if (_accept != value)
            {
                _accept = value;
                OnAcceptEvent();
            }
        }
    }
    public event Action<int> OnAcceptInt;
    public event Action OnAccept;
    #endregion

    #region Consecutivos
    private int _bestConsecutives;
    private int _bestConsecutiveInScene;
    private int _consecutives;
    public int Consecutives
    {
        get => _consecutives;
        set
        {
            if (_consecutives != value)
            {
                _consecutives = value;
                OnConsecutiveEvent();
            }
        }
    }
    public event Action OnConsecutive;
    public event Action<int> OnConsecutiveInt;

    #endregion

    #region TotalCube
    private string _sucessPercentage;
    private int _percentageInt;
    private int _totalCube;
    public int TotalCube
    {
        get => _totalCube;
        set
        {
            if (_totalCube != value)
            {
                _totalCube = value;
                OnTotalCubeEvent();
            }
        }
    }
    public event Action<int> OnTotalCubeInt;
    public event Action OnTotalCube;
    #endregion

    #region Erros
    private int _errors;
    public int Errors
    {
        get => _errors;
        set
        {
            if (_errors != value)
            {
                _errors = value;
                OnErrorEvent();
            }
        }
    }
    public event Action<int> OnErrorInt;
    public event Action OnError;
    #endregion

    private const string MAIN_TOTAL_SCORE = "MainTotalScore";
    private const string MAIN_TOTAL_ERRORS = "MainTotalErrors";
    private const string MAIN_BEST_CONSECUTIVE = "BestConsecutive";
    private const string MAIN_TOTAL_ACCURACY = "MainTotalAccuracy";
    [Header("Eventos ao acertar")]
    public List<EventOnConsecutives> PointsEventsTrigger = new();
    [Header("Eventos ao errar")]
    public List<EventOnConsecutives> ErrorEventsTrigger = new();
    private void Awake() => Instance = this;
    private void Start()
    {
        _bestConsecutives = PlayerPrefs.GetInt(MAIN_BEST_CONSECUTIVE, 0);
        EventOnConsecutives eventConsecutive = new EventOnConsecutives
        {
            PointTrigger = StageLoadController.Instance.CurrentDifficulty.ErrorTolerance,
            EventTrigger = new UnityEvent()
        };

        eventConsecutive.EventTrigger.AddListener(GameState.Instance.GameStateEnd);

        ErrorEventsTrigger.Add(eventConsecutive);
    }
    private void OnEnable()
    {
        OnConsecutiveInt += CheckConsecutiveEvents;
        OnConsecutiveInt += CheckBestSceneConsecutive;
        OnConsecutive += CheckBestConsecutive;
        OnTotalCube += CalculateSucessPercentage;
        OnErrorInt += CheckConsecutiveErrorEvents;
    }
    private void OnDisable()
    {
        OnConsecutiveInt -= CheckConsecutiveEvents;
        OnConsecutiveInt -= CheckBestSceneConsecutive;
        OnConsecutive -= CheckBestConsecutive;
        OnTotalCube -= CalculateSucessPercentage;
        OnErrorInt -= CheckConsecutiveErrorEvents;
    }
    public void IncreasePoint()
    {
        Accept++;
        Consecutives++;
        TotalCube++;
        CalculateSucessPercentage();
    }
    public void IncreaseError()
    {
        Errors++;
        Consecutives = 0;
        TotalCube++;
        CalculateSucessPercentage();
        TouchEffect.Instance.ResetAllColumns();
    }
    [ContextMenu("Teste Porcentagem")]
    public void CalculateSucessPercentage()
    {
        if (TotalCube <= 0)
        {
            _sucessPercentage = "0%";
            return;
        }

        float percentage = (float)Accept * 100 / TotalCube;
        _percentageInt = (int)percentage;
        _sucessPercentage = percentage.ToString("F1") + "%";

    }
    public float GetPercentageSucessInt() => _percentageInt;
    public string GetPercentageSucessString() => _sucessPercentage;
    public void CheckBestConsecutive()
    {
        if (_bestConsecutiveInScene >= _bestConsecutives)
        {
            _bestConsecutives= _bestConsecutiveInScene;
            PlayerPrefs.SetInt(MAIN_BEST_CONSECUTIVE, _bestConsecutives);
        }
    }
    public void UpdateMainValues()
    {
        int score = PlayerPrefs.GetInt(MAIN_TOTAL_SCORE)+ Accept;
        int error = PlayerPrefs.GetInt(MAIN_TOTAL_ERRORS)+Errors;
        float accuracy = PlayerPrefs.GetFloat(MAIN_TOTAL_ACCURACY);
        if (accuracy==0)
            accuracy=_percentageInt;
        else
            accuracy=(_percentageInt+ accuracy)/2;
        PlayerPrefs.SetInt(MAIN_TOTAL_SCORE, score);
        PlayerPrefs.SetInt(MAIN_TOTAL_ERRORS, error);
        PlayerPrefs.SetFloat(MAIN_TOTAL_ACCURACY, accuracy);
        PlayerPrefs.Save();
    }
    #region Events
    public void OnErrorEvent()
    {
        OnError?.Invoke();
        OnErrorInt?.Invoke(Errors);
    }
    public void OnConsecutiveEvent()
    {
        OnConsecutive?.Invoke();
        OnConsecutiveInt?.Invoke(Consecutives);
    }
    public void OnAcceptEvent()
    {
        OnAccept?.Invoke();
        OnAcceptInt?.Invoke(Accept);
    }
    public void OnTotalCubeEvent()
    {
        OnTotalCube?.Invoke();
        OnTotalCubeInt?.Invoke(TotalCube);
    }
    #endregion

    public void CheckConsecutiveEvents(int consecutives)
    {
        if (consecutives <= 0 || PointsEventsTrigger == null || PointsEventsTrigger.Count == 0)
            return;

        for (int i = 0; i < PointsEventsTrigger.Count; i++)
        {
            var evt = PointsEventsTrigger[i];

            if (evt == null || evt.PointTrigger <= 0)
                continue;

            if (consecutives >= evt.PointTrigger &&
                consecutives % evt.PointTrigger == 0)
            {
                evt.EventTrigger?.Invoke();
            }
        }
    }
    public void CheckConsecutiveErrorEvents(int consecutives)
    {
        if (consecutives <= 0 || ErrorEventsTrigger == null || ErrorEventsTrigger.Count == 0)
            return;

        for (int i = 0; i < ErrorEventsTrigger.Count; i++)
        {
            var evt = ErrorEventsTrigger[i];

            if (evt == null || evt.PointTrigger <= 0)
                continue;

            if (consecutives >= evt.PointTrigger &&
                consecutives % evt.PointTrigger == 0)
            {
                evt.EventTrigger?.Invoke();
            }
        }
    }
    public void CheckBestSceneConsecutive(int consecutives)
    {
        if (consecutives > _bestConsecutiveInScene)
        {
            _bestConsecutiveInScene = consecutives;
        }
    }
    public int GetBestConsecutiveInScene() {  return _bestConsecutiveInScene; }
    public int GetBestConsecutive() { return _bestConsecutives; }
}
[System.Serializable]
public class EventOnConsecutives
{
    [Header("Point Trigger")]
    public int PointTrigger;
    [Header("Event On Trigger")]
    public UnityEvent EventTrigger;
}