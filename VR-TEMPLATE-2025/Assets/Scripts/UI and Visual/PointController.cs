
using UnityEngine;
using System;
using UnityEngine.Events;
using System.Linq;
using System.Collections.Generic;

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
    private void OnEnable()
    {
        OnConsecutiveInt += CheckConsecutiveEvents;
    }
    private void OnDisable()
    {
        OnConsecutiveInt -= CheckConsecutiveEvents;
    }
    public void IncreasePoint()
    {
        Accept++;
        Consecutives++;
    }
    public void IncreaseError()
    {
        Errors++;
        Consecutives = 0;
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

            // dispara em múltiplos do PointTrigger
            if (consecutives >= evt.PointTrigger &&
                consecutives % evt.PointTrigger == 0)
            {
                evt.EventTrigger?.Invoke();
            }
        }
    }
}
[System.Serializable]
public class EventOnConsecutives
{
    [Header("Point Trigger")]
    public int PointTrigger;
    [Header("Event On Trigger")]
    public UnityEvent EventTrigger;
}