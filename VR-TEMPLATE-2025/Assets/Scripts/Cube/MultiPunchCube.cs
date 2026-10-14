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

public class MultiPunchCube : CubeCollider, IHandHittable
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
    private Vector3 originalLocalScale;

    private BuildMovemmentVisual[] visualBoosters;
    private MeshTrailEmitter meshTrailEmitter;

    private bool hasMisstaken = false;

    private bool eventsRegistered = false;

    // Janela (s) em que a colisão e o HandHitAssist da mesma mão são tratados como um único soco
    private const float SAME_HIT_WINDOW = 0.15f;
    private GameObject lastHitter;
    private float lastHitTime;
    private bool lastHitWasAssist;

    #region LIFECYCLE

    protected virtual void Awake()
    {
        originalLocalScale=transform.localScale;
        cachedRenderer = GetComponentInChildren<Renderer>();
        if (cachedRenderer != null)
            originalMaterial = cachedRenderer.material;

        meshTrailEmitter = GetComponentInChildren<MeshTrailEmitter>();
        if (meshTrailEmitter != null && cachedRenderer != null)
            meshTrailEmitter.materialSource = cachedRenderer;

        foreach (var p in punchPoints)
        {
            p.sphereLocalScale=p.sphere.localScale;
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        transform.localScale = originalLocalScale;
        hasMisstaken = false;
        lastHitter = null;

        if (mainFeedback != null)
            mainFeedback.gameObject.SetActive(false);

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

            if (SwitchHandsController.Instance != null)
            {
                OnSuccess += SwitchHandsController.Instance.RandomizeCurrentBothHands;
                OnFail += SwitchHandsController.Instance.RandomizeCurrentBothHands;
            }

            eventsRegistered = true;
        }

        StartCoroutine(LifeTime());
    }

    protected override void OnDisable()
    {
        base.OnDisable();

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

            if (SwitchHandsController.Instance != null)
            {
                OnSuccess -= SwitchHandsController.Instance.RandomizeCurrentBothHands;
                OnFail -= SwitchHandsController.Instance.RandomizeCurrentBothHands;
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
        if (TryResolve())
            FailFeedback();
    }

    #endregion

    #region COLLISION

    protected override void OnCollisionEnter(Collision collision)
    {
        PunchPoint point = null;

        if (collision.gameObject.CompareTag(PLAYER_TAG))
        {
            // Cubo já resolvido ou ponto já tocado: ignora o toque (cada ponto registra um único toque).
            if (IsResolved)
                return;

            // Essa mão acabou de bater pelo HandHitAssist: a colisão atrasada do mesmo soco não conta de novo
            if (lastHitWasAssist && IsRecentHitBy(collision.gameObject))
                return;

            point = GetClosestPoint(collision.transform.position);
            if (point != null && point.isTouched && !PowerEffect.Instance.isPowered)
                return;
        }

        base.OnCollisionEnter(collision);

        if (collision.gameObject.CompareTag(WALL_TAG))
        {
            HandleFail();
            return;
        }

        if (collision.gameObject.CompareTag(PLAYER_TAG))
        {
            RememberHit(collision.gameObject, false);
            ApplyPlayerHit(collision.gameObject, point, collision.contacts[0].point);
        }
    }

    /// <summary>
    /// Golpe detectado fora do OnCollisionEnter (HandHitAssist do hand tracking).
    /// Retorna false se o golpe foi ignorado (cubo resolvido, ponto já tocado ou mesmo soco já registrado).
    /// </summary>
    public bool TryHandAssistHit(GameObject hitter, Vector3 handPosition, Vector3 contactPoint)
    {
        if (IsResolved)
            return false;

        // Mesmo soco já registrado (pela colisão ou pelo assist): evita que um soco marque dois pontos
        if (IsRecentHitBy(hitter))
            return false;

        PunchPoint point = GetClosestPoint(handPosition);
        if (point != null && point.isTouched && !PowerEffect.Instance.isPowered)
            return false;

        RegisterHit();
        RememberHit(hitter, true);
        ApplyPlayerHit(hitter, point, contactPoint);
        return true;
    }

    private void ApplyPlayerHit(GameObject hitter, PunchPoint point, Vector3 contactPoint)
    {
        if (PowerEffect.Instance.isPowered)
        {
            HandTouchFeedback.Instance.HandFeedback(hitter, true);
            HandleSuccess();
            collisionLocation = contactPoint;
            return;
        }

        if (point != null && !point.isTouched && !hasMisstaken)
        {

            HitPoint(point);

            if (AllPointsTouched())
            {
                HandTouchFeedback.Instance.HandFeedback(hitter, true);
                HandleSuccess();
                collisionLocation = contactPoint;
            }
        }
        else
        {
            HandTouchFeedback.Instance.HandFeedback(hitter, false);
            HandleFail();
        }
    }

    private void RememberHit(GameObject hitter, bool fromAssist)
    {
        lastHitter = hitter;
        lastHitTime = Time.unscaledTime;
        lastHitWasAssist = fromAssist;
    }

    /// <summary>
    /// true se a mesma mão (o objeto ou um pai/filho dele) bateu neste cubo há menos de SAME_HIT_WINDOW.
    /// Tempo real (unscaled), pois o tracking não desacelera com o slow motion.
    /// </summary>
    private bool IsRecentHitBy(GameObject hitter)
    {
        if (lastHitter == null || Time.unscaledTime - lastHitTime > SAME_HIT_WINDOW)
            return false;

        Transform a = hitter.transform;
        Transform b = lastHitter.transform;
        return a == b || a.IsChildOf(b) || b.IsChildOf(a);
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
        // Inicia o retorno ao pool antes de desativar, pois a coroutine roda no ObjectPooler.
        ReturnToPool(PrefabTag);

        gameObject.SetActive(false);

        if (mainParticle != null)
            mainParticle.Play();

        if (mainFeedback != null)
        {
            mainFeedback.gameObject.SetActive(true);

            if (mainFeedback.TryGetComponent<BreakCube>(out var breakCube))
                breakCube.TriggerExplosion(collisionLocation, transform);
        }
    }

    public void FailFeedback()
    {
        hasMisstaken = true;
        StopAllCoroutines();
        StartCoroutine(FailRoutine());
    }

    IEnumerator FailRoutine()
    {
        meshTrailEmitter?.StopAndKillGhosts();

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