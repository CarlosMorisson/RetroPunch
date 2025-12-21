using UnityEngine;
using System;
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

    private void Awake() => Instance = this;
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

}
