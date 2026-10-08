using System.Collections;
using System.Collections.Generic;
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
    [Tooltip("Multiplicador de impacto baseado na velocidade da m�o do jogador")]
    public float handVelocityMultiplier = 1.2f;
    [Tooltip("Velocidade mínima/máxima do cubo após o soco")]
    public float minPushSpeed = 4f;
    public float maxPushSpeed = 15f;
    [Tooltip("Abaixo desta velocidade (m/s) a mão é considerada parada e usa a normal do contato")]
    public float minHandSpeedForDirection = 0.5f;

    // Cubos já tocados pelo jogador e ainda não resolvidos; os portais puxam apenas estes.
    public static readonly List<PushCube> TouchedCubes = new List<PushCube>();

    public Rigidbody Body => rb;
    public bool CanBePulled => IsTouchLocked && !IsResolved && rb != null && !rb.isKinematic;

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
    private MeshTrailEmitter meshTrailEmitter;

    // Pares bola/mão com colisão desligada após o toque; religados no OnDisable (pool).
    private readonly List<(Collider, Collider)> ignoredPairs = new List<(Collider, Collider)>();

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

        meshTrailEmitter = GetComponentInChildren<MeshTrailEmitter>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        hasMisstaken = false;
        transform.localRotation = initialRotation;
        transform.localScale = initialScale;
        feedbackRotate.localRotation = initialRotation;
        feedbackRotate.gameObject.SetActive(false);
        visualBoosters = Object.FindObjectsByType<BuildMovemmentVisual>(FindObjectsSortMode.None);

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

        OnSuccess += OnSucessLocal.Invoke;

        PowerEffect.OnPowerStarted += CheckExplosionSucess;


        StartCoroutine(LifeTime());
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        TouchedCubes.Remove(this);
        RestorePlayerCollision();

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

        OnSuccess -= OnSucessLocal.Invoke;

        PowerEffect.OnPowerStarted -= CheckExplosionSucess;

        visualBoosters = Object.FindObjectsByType<BuildMovemmentVisual>(FindObjectsSortMode.None);

        foreach (var booster in visualBoosters)
        {
            if (booster == null) continue;

            OnSuccess -= booster.TriggerBoost;
            OnFail -= booster.TriggerDeBoost;
        }

        if (ColorController.Instance != null)
        {
            OnSuccess -= ColorController.Instance.TriggerSuccessFlash;
            OnFail -= ColorController.Instance.TriggerFailDim;
        }
    }

    public IEnumerator LifeTime()
    {
        yield return new WaitForSeconds(CubeLifeTime);
        if (TryResolve())
            FailFeedback();
    }

    #endregion


    #region Collision

    protected override void OnCollisionEnter(Collision collision)
    {
        // Apenas o primeiro toque do jogador é registrado; os demais são bloqueados até o próximo OnEnable.
        if (collision.gameObject.CompareTag(PLAYER_TAG) && !TryLockTouch())
            return;

        base.OnCollisionEnter(collision);

        if (collision.gameObject.CompareTag(WALL_TAG))
        {
            HandleFail();
            return;
        }

        if (collision.gameObject.CompareTag(PLAYER_TAG))
        {
            HandVelocityTracker hand = collision.gameObject.GetComponentInParent<HandVelocityTracker>();
            Vector3 handVelocity = hand != null ? hand.Velocity : Vector3.zero;
            ContactPoint contact = collision.contacts[0];

            ApplyPlayerHit(collision.gameObject, contact.point, contact.normal, handVelocity);
        }
        if (collision.gameObject.CompareTag(PORTAL_NAME) && !IsResolved)
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
    /// <summary>
    /// Batida detectada fora do OnCollisionEnter (ex: sweep da raquete em golpes rápidos que atravessariam o collider).
    /// Retorna false se o cubo já foi tocado ou resolvido.
    /// </summary>
    /// <param name="hitter">Objeto que bateu (usado para achar a mão no HandTouchFeedback).</param>
    /// <param name="point">Ponto do impacto.</param>
    /// <param name="pushNormal">Direção de empurrão usada se a mão estiver praticamente parada.</param>
    /// <param name="handVelocity">Velocidade da mão/raquete no momento do golpe.</param>
    public bool TryPlayerHit(GameObject hitter, Vector3 point, Vector3 pushNormal, Vector3 handVelocity)
    {
        if (!TryLockTouch())
            return false;

        RegisterHit();
        ApplyPlayerHit(hitter, point, pushNormal, handVelocity);
        return true;
    }

    private void ApplyPlayerHit(GameObject hitter, Vector3 point, Vector3 pushNormal, Vector3 handVelocity)
    {
        collisionLocation = point;

        IgnorePlayerCollision(hitter);

        ApplyBounceForce(pushNormal, handVelocity);
        if (!TouchedCubes.Contains(this))
            TouchedCubes.Add(this);
        HandTouchFeedback.Instance.HandFeedback(hitter, true);
        StartCoroutine(WaitToDestroy());
    }

    /// <summary>
    /// Desliga a colisão entre a bola e a mão/raquete que bateu.
    /// Em golpes rápidos a raquete já está enterrada na bola quando a batida é detectada; sem isso, no passo seguinte
    /// a física empurra a bola pra fora pelo lado errado e anula a velocidade aplicada.
    /// Como só o primeiro toque conta (TryLockTouch), a mão não precisa mais colidir com esta bola.
    /// </summary>
    private void IgnorePlayerCollision(GameObject hitter)
    {
        HandVelocityTracker tracker = hitter.GetComponentInParent<HandVelocityTracker>();
        Transform handRoot = tracker != null ? tracker.transform : hitter.transform;
        Transform ballRoot = rb != null ? rb.transform : transform;

        Collider[] handColliders = handRoot.GetComponentsInChildren<Collider>(true);
        Collider[] ballColliders = ballRoot.GetComponentsInChildren<Collider>(true);

        foreach (Collider ball in ballColliders)
        {
            foreach (Collider hand in handColliders)
            {
                Physics.IgnoreCollision(ball, hand, true);
                ignoredPairs.Add((ball, hand));
            }
        }
    }

    /// <summary>Religa as colisões ignoradas; necessário porque a bola volta pro pool e é reutilizada.</summary>
    private void RestorePlayerCollision()
    {
        foreach ((Collider ball, Collider hand) in ignoredPairs)
        {
            if (ball != null && hand != null)
                Physics.IgnoreCollision(ball, hand, false);
        }
        ignoredPairs.Clear();
    }

    private void ApplyBounceForce(Vector3 pushNormal, Vector3 handVelocity)
    {
        if (rb == null) return;

        float handSpeed = handVelocity.magnitude;

        // Direção do golpe da mão; a normal do contato só é usada se a mão estiver praticamente parada
        Vector3 pushDirectionVector = handSpeed >= minHandSpeedForDirection
            ? handVelocity / handSpeed
            : pushNormal;

        float finalSpeed = Mathf.Clamp(handSpeed * handVelocityMultiplier, minPushSpeed, maxPushSpeed);

        rb.linearVelocity = pushDirectionVector * finalSpeed;
        rb.angularVelocity = Vector3.zero;
        GetComponent<PushCubeFeedback>().HandlePushFeedback(transform.position, pushDirectionVector);
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
        meshTrailEmitter?.StopAndKillGhosts();
        feedbackRotate.gameObject.SetActive(true);
        feedbackRotate.GetComponent<BreakCube>().TriggerExplosion(collisionLocation, transform);
        ReturnToPool(PrefabTag);
    }

    public void FailFeedback()
    {
        hasMisstaken = true;
        StopAllCoroutines();
        StartCoroutine(FailRoutine());
    }

    private IEnumerator FailRoutine()
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

    #region Helpers

    protected override bool ConcludeCondition(GameObject other)
    {
        return overlapReached;
    }

    #endregion

}