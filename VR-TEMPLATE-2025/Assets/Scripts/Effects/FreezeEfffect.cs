using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class FreezeEffect : MonoBehaviour
{

    private const float DEFAULT_TIME_SCALE = 1f;
    private const float FREEZE_TIME_SCALE = 0.05f;
    private const float FREEZE_DURATION = 20f;


    public UnityEvent OnStartFreezeTime;
    public UnityEvent OnFinishFreezeTime;
    private Coroutine freezeRoutine;

    private void Start()
    {
        Time.timeScale = DEFAULT_TIME_SCALE;    
    }


    [ContextMenu("teste")]
    public void TriggerFreeze()
    {
        if (freezeRoutine != null)
            StopCoroutine(freezeRoutine);

        freezeRoutine = StartCoroutine(FreezeRoutine());
    }

    private IEnumerator FreezeRoutine()
    {
        Time.timeScale = FREEZE_TIME_SCALE;
        OnStartFreezeTime.Invoke();
        yield return new WaitForSecondsRealtime(FREEZE_DURATION);
        Time.timeScale = DEFAULT_TIME_SCALE;
        OnFinishFreezeTime.Invoke();
        freezeRoutine = null;
    }

}