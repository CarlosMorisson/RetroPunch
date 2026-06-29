using System.Collections;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;

public enum PushType
{
    Down,
    Up,
    Left,
    Right,
    Freeze,
    Power
}

public class PushCube : CubeCollider
{
    [Header("Push Settings")]
    public PushType pushDirection;
    [Tooltip("Fator de perda de velocidade natural no rebote (ex: 0.8f = 80% da velocidade original)")]
    public float bounceRetainFactor = 0.8f;
    [Tooltip("Multiplicador de impacto baseado na velocidade da mão do jogador")]
    public float handVelocityMultiplier = 1.2f;

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
    private Rigidbody rb;

    private const string PORTAL_NAME = "Portal";

    private const float DESTROY_AFTER_TOUCH = 4f;

    #region Lifecycle

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = GetComponentInParent<Rigidbody>();

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
        feedbackRotate.localRotation = initialRotation;
        visualBoosters = Object.FindObjectsByType<BuildMovemmentVisual>(FindObjectsSortMode.None);

        foreach (var booster in visualBoosters)
        {
            if (booster == null) continue;

            OnSuccess += booster.TriggerBoost;
            OnFail += booster.TriggerDeBoost;
        }

        OnSuccess += SucessFeedback;
        OnFail += FailFeedback;

        OnSuccess += PointController.Instance.IncreasePoint;
        OnFail += PointController.Instance.IncreaseError;

        OnSuccess += ProgressEffectVisual.Instance.Success;
        OnFail += ProgressEffectVisual.Instance.Error;

        OnSuccess += ColorController.Instance.TriggerSuccessFlash;
        OnFail += ColorController.Instance.TriggerFailDim;

        OnSuccess += OnSucessLocal.Invoke;

        PowerEffect.OnPowerStarted += CheckExplosionSucess;


        StartCoroutine(LifeTime());
    }

    protected override void OnDisable()
    {
        OnSuccess -= SucessFeedback;
        OnFail -= FailFeedback;

        OnSuccess -= PointController.Instance.IncreasePoint;
        OnFail -= PointController.Instance.IncreaseError;

        OnSuccess -= ProgressEffectVisual.Instance.Success;
        OnFail -= ProgressEffectVisual.Instance.Error;

        OnSuccess -= OnSucessLocal.Invoke;

        PowerEffect.OnPowerStarted -= CheckExplosionSucess;

        visualBoosters = Object.FindObjectsByType<BuildMovemmentVisual>(FindObjectsSortMode.None);

        foreach (var booster in visualBoosters)
        {
            if (booster == null) continue;

            OnSuccess -= booster.TriggerBoost;
            OnFail -= booster.TriggerDeBoost;
        }

        OnSuccess -= ColorController.Instance.TriggerSuccessFlash;
        OnFail -= ColorController.Instance.TriggerFailDim;
    }

    public IEnumerator LifeTime()
    {
        yield return new WaitForSeconds(CubeLifeTime);
        FailFeedback();
    }

    #endregion


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
            collisionLocation = collision.contacts[0].point;

            ApplyBounceForce(collision);
            HandTouchFeedback.Instance.HandFeedback(collision.gameObject, true);
            StartCoroutine(WaitToDestroy());
        }
        if (collision.gameObject.CompareTag(PORTAL_NAME))
        {
            PortalFeedback portalGame = collision.gameObject.GetComponent<PortalFeedback>();
            if (portalGame.PushType == pushDirection || pushDirection == PushType.Freeze || pushDirection == PushType.Power)
            {
                HandleSuccess();
                portalGame.HandleSuccess();
            }
            else
            {
                HandleFail();
                portalGame.HandleFail();
            }
        }
    }
    private IEnumerator WaitToDestroy()
    {
        yield return new WaitForSeconds(DESTROY_AFTER_TOUCH);
        HandleFail();   
    }
    private void ApplyBounceForce(Collision collision)
    {
        if (rb == null) return;

        Vector3 bounceDirection = collision.contacts[0].normal;

        float currentSpeed = rb.linearVelocity.magnitude;

        if (currentSpeed < 0.1f) currentSpeed = 5f;

        float baseBounceSpeed = currentSpeed * bounceRetainFactor;

        Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
        float playerSpeed = playerRb != null ? playerRb.linearVelocity.magnitude : 1f;

        if (playerSpeed < 0.5f) playerSpeed = 0.5f;

        float finalSpeed = baseBounceSpeed * (playerSpeed * handVelocityMultiplier);

        rb.linearVelocity = Vector3.zero;
        rb.AddForce(bounceDirection * finalSpeed, ForceMode.VelocityChange);
        GetComponent<PushCubeFeedback>().HandlePushFeedback(transform.position, bounceDirection); ;
    }

    #endregion

    #region Feedback

    public void CheckExplosionSucess()
    {
        Transform parent = transform.parent;
        if (parent != null)
        {
            CubeMovemment move = parent.GetComponent<CubeMovemment>();
            if (move.boostFinished)
            {
                HandleSuccess();
                print("chamou------------");
            }
        }
    }
    public void SucessFeedback()
    {
        feedbackRotate.gameObject.SetActive(true);
        feedbackRotate.GetComponent<BreakCube>().TriggerExplosion(collisionLocation, transform);
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

    protected override bool ConcludeCondition(GameObject other)
    {
        return overlapReached;
    }

    #endregion

}