using System.Collections;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;

public class ShootCube : CubeCollider
{
    [Header("Prefab")]
    public string PrefabTag;

    [Header("Hit FX")]
    public Transform feedbackRotate;

    [Header("Fail Feedback")]
    public Material failMaterial;
    public float blinkInterval = 0.1f;
    public int blinkCount = 3;
    public float scaleDownDuration = 0.4f;

    public UnityEvent OnSuccessLocal;

    // BeatPoint atribuído pelo InstancerController no spawn
    [HideInInspector]
    public BeatPoint beat;

    private Renderer cachedRenderer;
    private Material originalMaterial;
    private Vector3 collisionLocation;
    private Quaternion initialRotation;
    private Vector3 initialScale;
    private BuildMovemmentVisual[] visualBoosters;

    private bool hasMisstaken = false;
    private bool eventsRegistered = false;
    private bool wasHit = false;

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
        wasHit = false;

        transform.localRotation = initialRotation;
        transform.localScale = initialScale;

        if (feedbackRotate != null)
            feedbackRotate.localRotation = initialRotation;

        if (!eventsRegistered)
        {
            visualBoosters = Object.FindObjectsByType<BuildMovemmentVisual>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            foreach (var booster in visualBoosters)
            {
                if (booster == null) continue;
                OnSuccess += booster.TriggerBoost;
                OnFail += booster.TriggerDeBoost;
            }

            OnSuccess += SuccessFeedback;
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

            if (OnSuccessLocal != null)
                OnSuccess += OnSuccessLocal.Invoke;

            eventsRegistered = true;
        }

        StartCoroutine(LifeTime());
    }

    protected override void OnDisable()
    {
        if (eventsRegistered)
        {
            OnSuccess -= SuccessFeedback;
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

            if (ColorController.Instance != null)
            {
                OnSuccess -= ColorController.Instance.TriggerSuccessFlash;
                OnFail -= ColorController.Instance.TriggerFailDim;
            }

            if (OnSuccessLocal != null)
                OnSuccess -= OnSuccessLocal.Invoke;

            if (visualBoosters != null)
            {
                foreach (var booster in visualBoosters)
                {
                    if (booster == null) continue;
                    OnSuccess -= booster.TriggerBoost;
                    OnFail -= booster.TriggerDeBoost;
                }
            }

            eventsRegistered = false;
        }

        base.OnDisable();
    }

    public IEnumerator LifeTime()
    {
        yield return new WaitForSeconds(CubeLifeTime);
        if (!wasHit)
            HandleFail();
    }

    #endregion

    #region Collision

    protected override void OnCollisionEnter(Collision collision)
    {
        // Parede ? fail
        if (collision.gameObject.CompareTag(WALL_TAG))
        {
            HandleFail();
            return;
        }
    }

    // Flecha é trigger — detecta aqui
    protected override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);

        if (wasHit || hasMisstaken) return;

        ArrowProjectile arrow = other.GetComponent<ArrowProjectile>();
        if (arrow == null) return;

        wasHit = true;
        collisionLocation = other.ClosestPoint(transform.position);

        // Impact boost na música proporcional à intensidade do beat
        SongController.Instance.ImpactBoost(
            beat != null ? beat.intensity : 0.6f,
            beat != null ? 0.05f + beat.intensity * 0.05f : 0.05f
        );

        // Destrói a flecha
        arrow.OnHitTarget();

        HandleSuccess();
    }

    #endregion

    #region Feedback

    public void SuccessFeedback()
    {
        if (feedbackRotate != null)
        {
            feedbackRotate.gameObject.SetActive(true);

            if (feedbackRotate.TryGetComponent<BreakCube>(out var breakCube))
                breakCube.TriggerExplosion(collisionLocation, transform);
        }

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
            .OnComplete(() => ReturnToPool(PrefabTag));
    }

    #endregion

    #region Condition

    protected override bool ConcludeCondition(GameObject other)
    {
        // Sucesso é determinado pelo OnTriggerEnter da flecha
        return wasHit;
    }

    #endregion
}