using System.Collections;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;

public class PunchCube : CubeCollider
{
    [Header("Overlap Sphere Settings")]
    public Transform overlapCenter;
    public float overlapRadius = 0.5f;
    public LayerMask overlapMask;
    public string PrefabTag;

    [Header("Random Rotation")]
    public Transform feedbackRotate;
    public Vector2 minRotation;
    public Vector2 maxRotation;

    [Header("Fail Feedback")]
    public Material failMaterial;
    public float blinkInterval = 0.1f;
    public int blinkCount = 3;
    public float scaleDownDuration = 0.4f;

    public UnityEvent OnSucessLocal;

    private bool overlapReached;
    private Vector3 collisionLocation;

    private Quaternion initialRotation;
    private Vector3 initialScale;

    private BuildMovemmentVisual[] visualBoosters;

    private Renderer cachedRenderer;
    private Material originalMaterial;

    private bool hasMisstaken = false;
    private bool eventsRegistered=false;
    #region Lifecycle

    protected virtual void Awake()
    {
        initialRotation = transform.localRotation;
        initialScale = transform.localScale;

        cachedRenderer = GetComponentInChildren<Renderer>();
        if (cachedRenderer != null)
            originalMaterial = cachedRenderer.material;
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        hasMisstaken = false;
        transform.localRotation = initialRotation;
        transform.localScale = initialScale;

        if (feedbackRotate != null)
            feedbackRotate.localRotation = initialRotation;

        if (!eventsRegistered)
        {
            visualBoosters = Object.FindObjectsByType<BuildMovemmentVisual>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var booster in visualBoosters)
            {
                if (booster == null) continue;
                OnSuccess += booster.TriggerBoost;
                OnFail += booster.TriggerDeBoost;
            }

            OnSuccess += SucessFeedback;
            OnFail += FailFeedback;

            if (PointController.Instance != null)
            {
                OnSuccess += PointController.Instance.IncreasePoint;
                OnFail += PointController.Instance.IncreaseError;
            }

            if (ProgressEffectVisual.Instance != null)
            {
                OnSuccess += ProgressEffectVisual.Instance.Success;
                OnFail += ProgressEffectVisual.Instance.Error;
            }

            if (ColorController.Instance != null)
            {
                OnSuccess += ColorController.Instance.TriggerSuccessFlash;
                OnFail += ColorController.Instance.TriggerFailDim;
            }

            if (OnSucessLocal != null)
                OnSuccess += OnSucessLocal.Invoke;

            eventsRegistered = true;
        }

        RandomRotation();
        StartCoroutine(LifeTime());
    }

    protected override void OnDisable()
    {
        if (eventsRegistered)
        {
            OnSuccess -= SucessFeedback;
            OnFail -= FailFeedback;

            if (PointController.Instance != null)
            {
                OnSuccess -= PointController.Instance.IncreasePoint;
                OnFail -= PointController.Instance.IncreaseError;
            }

            if (ProgressEffectVisual.Instance != null)
            {
                OnSuccess -= ProgressEffectVisual.Instance.Success;
                OnFail -= ProgressEffectVisual.Instance.Error;
            }

            if (OnSucessLocal != null)
                OnSuccess -= OnSucessLocal.Invoke;
            if (visualBoosters != null)
            {
                foreach (var booster in visualBoosters)
                {
                    if (booster == null) continue;
                    OnSuccess -= booster.TriggerBoost;
                    OnFail -= booster.TriggerDeBoost;
                }
            }

            if (ColorController.Instance != null)
            {
                OnSuccess -= ColorController.Instance.TriggerSuccessFlash;
                OnFail -= ColorController.Instance.TriggerFailDim;
            }

            eventsRegistered = false;
        }
    }


    public IEnumerator LifeTime()
    {
        yield return new WaitForSeconds(CubeLifeTime);
        FailFeedback();
    }

    #endregion

    protected virtual void Update()
    {
        CheckOverlap();
    }

    #region Collision

    protected override void OnCollisionEnter(Collision collision)
    {
        base.OnCollisionEnter(collision);

        if (collision.gameObject.CompareTag(WALL_TAG))
        {
            HandleFail();
            return;
        }

        if (collision.gameObject.CompareTag(PLAYER_TAG))
        {
            if (PowerEffect.Instance.isPowered)
            {
                collisionLocation = collision.contacts[0].point;
                HandTouchFeedback.Instance.HandFeedback(collision.gameObject, true);
                print(collision.gameObject.name);
                HandleSuccess();
                return;
            }
            if (ConcludeCondition(collision.gameObject) && !hasMisstaken)
            {
                collisionLocation = collision.contacts[0].point;
                HandTouchFeedback.Instance.HandFeedback(collision.gameObject, true);
                print(collision.gameObject.name);
                HandleSuccess();
            }
            else
            {
                HandTouchFeedback.Instance.HandFeedback(collision.gameObject, false);
                HandleFail();
            }
        }
    }

    #endregion

    #region Feedback

    public void SucessFeedback()
    {
        feedbackRotate.gameObject.SetActive(true);
        feedbackRotate.GetComponent<BreakCube>().TriggerExplosion(collisionLocation, transform);
        ReturnToPool(PrefabTag);
        gameObject.SetActive(false);
    }

    public void FailFeedback()
    {
        hasMisstaken = true;
        StopAllCoroutines();
        StartCoroutine(FailRoutine());
    }

    private IEnumerator FailRoutine()
    {
        if (cachedRenderer != null && failMaterial != null)
        {
            for (int i = 0; i < blinkCount; i++)
            {
                cachedRenderer.material = failMaterial;
                yield return new WaitForSeconds(blinkInterval);

                cachedRenderer.material = originalMaterial;
                yield return new WaitForSeconds(blinkInterval);
            }
        }

        transform.DOScale(Vector3.zero, scaleDownDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                ReturnToPool(PrefabTag);
            });
    }

    #endregion

    #region Helpers

    protected virtual void RandomRotation()
    {
        float rotX = Random.Range(minRotation.x, maxRotation.x);
        float rotY = Random.Range(minRotation.y, maxRotation.y);

        Quaternion rot = Quaternion.Euler(rotX, rotY, 0f);
        transform.localRotation = rot;
        feedbackRotate.localRotation = rot;
    }

    protected virtual void CheckOverlap()
    {
        if (overlapCenter == null)
            return;

        overlapReached = Physics.CheckSphere(
            overlapCenter.position,
            overlapRadius,
            overlapMask,
            QueryTriggerInteraction.Ignore
        );
    }

    protected override bool ConcludeCondition(GameObject other)
    {
        return overlapReached;
    }

    #endregion

    #region Debug

    protected virtual void OnDrawGizmosSelected()
    {
        if (overlapCenter == null)
            return;

        Gizmos.color = overlapReached ? Color.green : Color.red;
        Gizmos.DrawWireSphere(overlapCenter.position, overlapRadius);
    }

    #endregion
}
