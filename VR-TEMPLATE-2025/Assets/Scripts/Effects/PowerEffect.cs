using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PowerEffect : MonoBehaviour
{
    public static PowerEffect Instance { get; private set; }
    public float PowerDuration;

    public bool isPowered=false;
    public UnityEvent OnStartPowerTime;
    public UnityEvent OnFinishPowerTime;

    private Coroutine powerRoutine;
    private void Awake()
    {
        Instance = this;
    }
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
        isPowered = true;

        yield return new WaitForSecondsRealtime(PowerDuration);


        OnFinishPowerTime?.Invoke();

        powerRoutine = null;
        isPowered = false;
    }

}
