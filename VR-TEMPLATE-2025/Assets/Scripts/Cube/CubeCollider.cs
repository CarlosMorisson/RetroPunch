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

    // Trava de toque: apenas um toque do jogador é registrado por ciclo de vida (reseta no OnEnable).
    protected bool IsTouchLocked { get; private set; }
    // Trava de resultado: sucesso/falha só podem ser disparados uma vez por ciclo de vida.
    protected bool IsResolved { get; private set; }

    // Identifica o ciclo de vida atual; retornos ao pool agendados em ciclos anteriores são ignorados.
    private int lifeId;
    private bool returnScheduled;

    #region Unity Lifecycle

    protected virtual void OnEnable()
    {
        lifeId++;
        returnScheduled = false;
        IsTouchLocked = false;
        IsResolved = false;

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
        if (parent != null && parent.TryGetComponent<CubeMovemment>(out var move))
            move.normalSpeed = 0;
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
    /// <summary>
    /// Registra o toque do jogador. Retorna false se o cubo já foi tocado ou já foi resolvido.
    /// </summary>
    protected bool TryLockTouch()
    {
        if (IsTouchLocked || IsResolved)
            return false;

        IsTouchLocked = true;
        return true;
    }

    /// <summary>
    /// Marca o cubo como resolvido (sucesso/falha). Retorna false se já estava resolvido.
    /// </summary>
    protected bool TryResolve()
    {
        if (IsResolved)
            return false;

        IsResolved = true;
        return true;
    }

    protected void ReturnToPool(string poolTag)
    {
        // Evita agendar o retorno mais de uma vez no mesmo ciclo de vida.
        if (returnScheduled)
            return;
        returnScheduled = true;

        // Hospedada no ObjectPooler (sempre ativo) para sobreviver caso este GameObject
        // seja desativado imediatamente pelo feedback de sucesso/falha.
        if (ObjectPooler.Instance != null)
            ObjectPooler.Instance.StartCoroutine(WaitReturnToPool(poolTag, lifeId));
        else
            StartCoroutine(WaitReturnToPool(poolTag, lifeId));
    }
    private IEnumerator WaitReturnToPool(string poolTag, int scheduledLifeId)
    {
        yield return new WaitForSeconds(WAIT_TIME);

        // O cubo já voltou ao pool e foi reutilizado: não desativar o cubo novo.
        if (scheduledLifeId != lifeId)
            yield break;
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

        // Parede é tratada nas subclasses; aqui chamava HandleFail em duplicidade.
        if (IsResolved)
            return;

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
    /// Condiï¿½ï¿½o para o sucesso da colisï¿½o (pode ser sobrescrita nas subclasses)
    /// </summary>
    protected virtual bool ConcludeCondition(GameObject other)
    {
        return false;
    }

    protected virtual void HandleSuccess()
    {
        if (!TryResolve())
            return;

        StopParentMovement();
        OnSuccess?.Invoke();

    }
    private void StopParentMovement()
    {
        Transform parent = transform.parent;
        if (parent == null) return;

        if (parent.TryGetComponent<CubeMovemment>(out var move))
            move.normalSpeed = 0f;

        if (parent.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }
    }
    // CubeCollider ï¿½ HandleFail reseta fï¿½sica imediatamente, nï¿½o espera o pool
    protected virtual void HandleFail()
    {
        if (!TryResolve())
            return;

        StopParentMovement();

        OnFail?.Invoke();
    }

    #endregion
}
