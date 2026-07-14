using DG.Tweening;
using System;
using System.Collections;
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
    private const float WAIT_TIME=2f;

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
        Transform parent = transform.parent;
        if (parent != null)
        {
            CubeMovemment move = parent.GetComponent<CubeMovemment>();
            if (move.boostFinished && GameState.Instance.SceneState==State.Pause)
                gameObject.SetActive(false);
        }
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
                print("chamou");
                break;
        }
    }
    protected void ReturnToPool(string poolTag)
    {
        StartCoroutine(WaitReturnToPool(poolTag));  
    }
    private IEnumerator WaitReturnToPool(string poolTag)
    {
        yield return new WaitForSeconds(WAIT_TIME);
        ObjectPooler.Instance.ReturnToPool(
            poolTag,
            transform.parent.gameObject
        );
        transform.parent.gameObject.SetActive(false);
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
        AudioController.Instance.Play("Hit");
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

    // CubeCollider — HandleFail reseta física imediatamente, não espera o pool
    protected virtual void HandleFail()
    {
        Rigidbody rb = GetComponentInParent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;     
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }

        OnFail?.Invoke();
    }

    #endregion
}
