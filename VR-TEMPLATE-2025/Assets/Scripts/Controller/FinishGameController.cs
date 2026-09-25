using System;
using UnityEngine;
using UnityEngine.Events;

public class FinishGameController : MonoBehaviour
{
    public event Action OnSuccess;
    public event Action OnFailure;

    [Header("Fim com sucesso (musica terminou)")]
    public UnityEvent OnSuccessEvent;
    [Header("Fim com fracasso (errou demais)")]
    public UnityEvent OnFailureEvent;

    private bool _finished;

    // Chamar no GameState.OnEndEvent
    public void Finish()
    {
        if (_finished)
            return;
        _finished = true;

        if (IsFailure())
        {
            OnFailure?.Invoke();
            OnFailureEvent?.Invoke();
        }
        else
        {
            OnSuccess?.Invoke();
            OnSuccessEvent?.Invoke();
        }
    }

    // Fracasso = atingiu a tolerancia de erros consecutivos da dificuldade
    private bool IsFailure()
    {
        if (PointController.Instance == null || StageLoadController.Instance == null)
            return false;

        int tolerance = StageLoadController.Instance.CurrentDifficulty.ErrorTolerance;
        return tolerance > 0 && PointController.Instance.GetConsecutiveErrors() >= tolerance;
    }
}
