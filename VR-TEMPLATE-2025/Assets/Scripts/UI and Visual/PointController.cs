
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
    private const string CONSECUTIVES_SAVE_NAME = "BestConsecutive";
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

    public List<EventOnConsecutives> EventsTrigger = new();

    private void Awake() => Instance = this;
    private void Start()
    {
        _bestConsecutives = PlayerPrefs.GetInt(CONSECUTIVES_SAVE_NAME, 0);
    }
    private void OnEnable()
    {
        OnError += TouchEffect.Instance.ResetAllColumns;
        OnConsecutiveInt += CheckConsecutiveEvents;
        OnConsecutiveInt += CheckBestSceneConsecutive;
        OnConsecutive += CheckBestConsecutive;
        OnTotalCube += CalculateSucessPercentage;
    }
    private void OnDisable()
    {
        OnError -= TouchEffect.Instance.ResetAllColumns;
        OnConsecutiveInt -= CheckConsecutiveEvents;
        OnConsecutiveInt -= CheckBestSceneConsecutive;
        OnConsecutive -= CheckBestConsecutive;
        OnTotalCube -= CalculateSucessPercentage;
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
        _sucessPercentage = percentage.ToString("F2") + "%";

    }
    public float GetPercentageSucessInt() => _percentageInt;
    public string GetPercentageSucessString() => _sucessPercentage;
    public void CheckBestConsecutive()
    {
        if (_bestConsecutiveInScene >= _bestConsecutives)
        {
            _bestConsecutives= _bestConsecutiveInScene;
            PlayerPrefs.SetInt(CONSECUTIVES_SAVE_NAME, _bestConsecutives);
        }
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
        if (consecutives <= 0 || EventsTrigger == null || EventsTrigger.Count == 0)
            return;

        for (int i = 0; i < EventsTrigger.Count; i++)
        {
            var evt = EventsTrigger[i];

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