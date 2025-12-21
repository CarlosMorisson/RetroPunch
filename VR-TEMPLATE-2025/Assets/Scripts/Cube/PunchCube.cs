using System.Collections;
using UnityEngine;
using DG.Tweening;

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

    private bool overlapReached;
    private Vector3 collisionLocation;

    private Quaternion initialRotation;
    private Vector3 initialScale;

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
        feedbackRotate.localRotation = initialRotation;

        OnSuccess += SucessFeedback;
        OnFail += FailFeedback;

        OnSuccess += PointController.Instance.IncreasePoint;
        OnFail += PointController.Instance.IncreaseError;

        OnSuccess += ProgressEffectVisual.Instance.Success;
        OnFail += ProgressEffectVisual.Instance.Error;

        RandomRotation();
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
            if (ConcludeCondition(collision.gameObject) && !hasMisstaken)
            {
                collisionLocation = collision.contacts[0].point;
                print(collisionLocation);
                HandleSuccess();
            }
            else
            {
                FailFeedback();
            }
        }
    }

    #endregion

    #region Feedback

    public void SucessFeedback()
    {
        feedbackRotate.gameObject.SetActive(true);
        feedbackRotate.GetComponent<BreakCube>().TriggerExplosion(collisionLocation);
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
                GameObject parent = transform.parent.gameObject;
                ObjectPooler.Instance.ReturnToPool(PrefabTag, parent);
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
