using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[System.Serializable]
public class PunchPoint
{
    public Transform sphere;
    public bool isTouched;

    public ParticleSystem particle;
    public MonoBehaviour feedback;
    [HideInInspector]
    public Vector3 sphereLocalScale;
}

public class MultiPunchCube : CubeCollider
{
    [Header("Punch Points")]
    public List<PunchPoint> punchPoints = new();

    [Header("Main Feedback")]
    public ParticleSystem mainParticle;
    public MonoBehaviour mainFeedback;

    [Header("Fail Feedback")]
    public Material failMaterial;
    public float blinkInterval = 0.04f;
    public int blinkCount = 1;
    public float scaleDownDuration = 0.05f;

    [Header("General")]
    public string PrefabTag;

    private Renderer cachedRenderer;
    private Material originalMaterial;
    private Vector3 collisionLocation;

    private BuildMovemmentVisual[] visualBoosters;

    private bool hasMisstaken = false;

    private bool eventsRegistered = false; 

    #region LIFECYCLE

    protected virtual void Awake()
    {

        cachedRenderer = GetComponentInChildren<Renderer>();
        if (cachedRenderer != null)
            originalMaterial = cachedRenderer.material;
        foreach (var p in punchPoints)
        {
            p.sphereLocalScale=p.sphere.localScale;
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        hasMisstaken = false;
        ResetPoints();

        if (!eventsRegistered)
        {
            visualBoosters = Object.FindObjectsByType<BuildMovemmentVisual>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var booster in visualBoosters)
            {
                if (booster == null) continue;
                OnSuccess += booster.TriggerBoost;
                OnFail += booster.TriggerDeBoost;
            }

            OnSuccess += FinalSuccess;
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

            if (SkyboxVisual.Instance != null)
            {
                OnSuccess += SkyboxVisual.Instance.TriggerSuccessBoost;
                OnFail += SkyboxVisual.Instance.TriggerFailDeboost;
            }

            eventsRegistered = true;
        }

        StartCoroutine(LifeTime());
    }

    protected override void OnDisable()
    {
        if (eventsRegistered)
        {
            OnSuccess -= FinalSuccess;
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

            if (SkyboxVisual.Instance != null)
            {
                OnSuccess -= SkyboxVisual.Instance.TriggerSuccessBoost;
                OnFail -= SkyboxVisual.Instance.TriggerFailDeboost;
            }

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
    }

    IEnumerator LifeTime()
    {
        yield return new WaitForSeconds(CubeLifeTime);
        FailFeedback();
    }

    #endregion

    #region COLLISION

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
            Transform hit = collision.transform;

            PunchPoint point = GetClosestPoint(hit.position);
            if (PowerEffect.Instance.isPowered)
            {
                HandTouchFeedback.Instance.HandFeedback(collision.gameObject, true);
                HandleSuccess();
                collisionLocation = collision.contacts[0].point;
                return;
            }

            if (point != null && !point.isTouched && !hasMisstaken)
            {
        
                HitPoint(point);

                if (AllPointsTouched())
                {
                    HandTouchFeedback.Instance.HandFeedback(collision.gameObject, true);
                    HandleSuccess();
                    collisionLocation = collision.contacts[0].point;
                }
            }
            else
            {
                HandTouchFeedback.Instance.HandFeedback(collision.gameObject, false);
                HandleFail();
            }
        }
    }

    #endregion

    #region LOGIC

    void HitPoint(PunchPoint point)
    {
        point.isTouched = true;

        if (point.particle != null)
            point.particle.Play();

        if (point.feedback != null)
        {
            point.feedback.gameObject.SetActive(true);
            point.feedback.gameObject.GetComponent<BreakCube>().TriggerExplosion(collisionLocation, transform);
        }
            
        point.sphere.transform.DOScale(Vector3.zero, 0.5f);
    }

    bool AllPointsTouched()
    {
        foreach (var p in punchPoints)
        {
            if (!p.isTouched)
                return false;
        }

        return true;
    }

    PunchPoint GetClosestPoint(Vector3 hitPos)
    {
        float minDist = float.MaxValue;
        PunchPoint closest = null;

        foreach (var p in punchPoints)
        {
            if (p.sphere == null) continue;

            float dist = Vector3.Distance(hitPos, p.sphere.position);

            if (dist < minDist)
            {
                minDist = dist;
                closest = p;
            }
        }

        return closest;
    }

    void ResetPoints()
    {
        foreach (var p in punchPoints)
        {
            p.isTouched = false;
            p.sphere.localScale = p.sphereLocalScale;
        }
    }

    #endregion

    #region SUCCESS / FAIL

    void FinalSuccess()
    {
        if (mainParticle != null)
            mainParticle.Play();

        if (mainFeedback != null)
        {
            mainFeedback.gameObject.SetActive(true);
            mainFeedback.GetComponent<BreakCube>().TriggerExplosion(collisionLocation, transform);
        }

        ReturnToPool(PrefabTag);
        gameObject.SetActive(false);
    }

    public void FailFeedback()
    {
        hasMisstaken = true;
        StopAllCoroutines();
        StartCoroutine(FailRoutine());
    }

    IEnumerator FailRoutine()
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

    #region DEBUG

    private void OnDrawGizmosSelected()
    {
        if (punchPoints == null) return;

        foreach (var p in punchPoints)
        {
            if (p.sphere == null) continue;

            Gizmos.color = p.isTouched ? Color.green : Color.red;
            Gizmos.DrawWireSphere(p.sphere.position, 0.1f);
        }
    }

    #endregion
}