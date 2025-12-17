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
                UIResult.Instance.UpdateResult(_accept);
            }
        }
    }
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
                UIResult.Instance.UpdateConsecutive(_consecutives);
            }
        }
    }
    public event Action<int> OnConsecutive;
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
                UIResult.Instance.UpdateError(_errors);
            }
        }
    }
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
    public void OnErrorEvent() => OnError.Invoke();

    public void OnConsecutiveEvent() => OnConsecutive.Invoke(_consecutives);

    public void OnAcceptEvent() => OnAccept.Invoke();
    #endregion

}
