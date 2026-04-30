using DG.Tweening;
using System;
using System.Collections;
using Unity.Tutorials.Core.Editor;
using UnityEngine;

public class CubeCollider : MonoBehaviour
{
    #region Lifecycle Events
    public event Action OnEnabled;
    public event Action OnDisabled;

    #endregion

    #region Collision Events

    public event Action<Collision> OnCollisionEnterEvent;
    public event Action<Collision> OnCollisionStayEvent;
    public event Action<Collision> OnCollisionExitEvent;

    public event Action<Collider> OnTriggerEnterEvent;
    public event Action<Collider> OnTriggerStayEvent;
    public event Action<Collider> OnTriggerExitEvent;

    #endregion

    #region Condition Events

    public event Action OnSuccess;
    public event Action OnFail;

    #endregion

    [Header("Tempo de vida do cubo")]
    [Range(5, 30)]
    public float CubeLifeTime;

    protected const string WALL_TAG="Wall";
    protected const string PLAYER_TAG = "Player";

    #region Unity Lifecycle

    protected virtual void OnEnable()
    {
        OnEnabled?.Invoke();
        GameState.OnGameStateChanged += GameStateChanged;
        StartRun();
    }
    public void StartRun()
    {
        Transform parent = transform.parent;
        if (parent != null)
        {
            CubeMovemment move = parent.GetComponent<CubeMovemment>();
            if (move != null && move.enableSpeed != 0)
            {
                move.normalSpeed = move.enableSpeed;
            }
        }
    }
    public void StopRun()
    {
        GameObject parent = transform.parent.gameObject;
        parent.GetComponent<CubeMovemment>().normalSpeed = 0;
    }
    public void FinishRun()
    {
        StopRun();
        transform.DOScale(Vector3.zero, 1f)
            .SetEase(Ease.InBack);
    }
    void GameStateChanged(State newState)
    {
        Debug.Log("Novo estado do jogo: " + newState);
        switch (newState)
        {
            case State.Game:
                StartRun();
                break;
            case State.Pause:
                StopRun();
                break;
            case State.End:
                FinishRun();
                break;
        }
    }
    protected virtual void OnDisable()
    {
        GameState.OnGameStateChanged -= GameStateChanged;
        OnDisabled?.Invoke();
    }

    #endregion

    #region Collision

    protected virtual void OnCollisionEnter(Collision collision)
    {
        OnCollisionEnterEvent?.Invoke(collision);
        if (collision.gameObject.CompareTag(WALL_TAG)){

            HandleFail();
        }
        StopRun();
    }

    protected virtual void OnCollisionStay(Collision collision)
    {
        OnCollisionStayEvent?.Invoke(collision);
    }

    protected virtual void OnCollisionExit(Collision collision)
    {
        OnCollisionExitEvent?.Invoke(collision);
    }

    #endregion

    #region Trigger

    protected virtual void OnTriggerEnter(Collider other)
    {
        OnTriggerEnterEvent?.Invoke(other);

    }

    protected virtual void OnTriggerStay(Collider other)
    {
        OnTriggerStayEvent?.Invoke(other);
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        OnTriggerExitEvent?.Invoke(other);
    }

    #endregion

    #region Condition Logic

    /// <summary>
    /// Condição para o sucesso da colisão (pode ser sobrescrita nas subclasses)
    /// </summary>
    protected virtual bool ConcludeCondition(GameObject other)
    {
        return false;
    }

    protected virtual void HandleSuccess()
    {
        OnSuccess?.Invoke();

    }

    protected virtual void HandleFail()
    {
        OnFail?.Invoke();
    }

    #endregion
}
