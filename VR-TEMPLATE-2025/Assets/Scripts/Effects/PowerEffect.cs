using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PowerEffect : MonoBehaviour
{
    public float PowerDuration;

    public UnityEvent OnStartPowerTime;
    public UnityEvent OnFinishPowerTime;

    private Coroutine powerRoutine;
    [ContextMenu("teste")]
    public void TriggerPower()
    {
        if (powerRoutine != null)
            StopCoroutine(powerRoutine);

        powerRoutine = StartCoroutine(FreezeRoutine());
    }

    private IEnumerator FreezeRoutine()
    {

        OnStartPowerTime?.Invoke();

        yield return new WaitForSecondsRealtime(PowerDuration);


        OnFinishPowerTime?.Invoke();

        powerRoutine = null;
    }

}
