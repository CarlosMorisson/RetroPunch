using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class FreezeEffect : MonoBehaviour
{
    [Header("Time Scale")]
    [SerializeField] private float defaultTimeScale = 1f;
    [SerializeField] private float freezeTimeScale = 0.05f;

    [Header("Timing")]
    [SerializeField] private float slowDownDuration = 1f;   
    [SerializeField] private float freezeDuration = 7f;     
    [SerializeField] private float restoreDuration = 0.5f;   


    public UnityEvent OnStartFreezeTime;
    public UnityEvent OnFinishFreezeTime;

    private Coroutine freezeRoutine;

    private void Start()
    {
        Time.timeScale = defaultTimeScale;
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
        yield return LerpTimeScale(defaultTimeScale, freezeTimeScale, slowDownDuration);

        OnStartFreezeTime?.Invoke();

        yield return new WaitForSecondsRealtime(freezeDuration);

        yield return LerpTimeScale(freezeTimeScale, defaultTimeScale, restoreDuration);

        OnFinishFreezeTime?.Invoke();

        freezeRoutine = null;
    }

    private IEnumerator LerpTimeScale(float from, float to, float duration)
    {
        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;

            float t = time / duration;

            t = t * t * (3f - 2f * t); 

            Time.timeScale = Mathf.Lerp(from, to, t);

            yield return null;
        }

        Time.timeScale = to;
    }
}