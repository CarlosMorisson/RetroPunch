using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Bola do tutorial do Push: ao ser golpeada e lancada na direcao da mao (mesma logica do PushCube).
// Entrar no portal do mesmo PushType e sucesso; portal errado, parede ou tempo esgotado e falha.
// Power e Freeze valem em qualquer portal: Freeze dispara o Freeze Time e Power dispara o Power Time
// e destroi (com sucesso) todas as bolas que ainda estao em jogo, como o PushCube faz no OnPowerStarted.
// Avisa o TutorialPushCube e pode ser restaurada para o estado inicial quando o step reinicia.
[RequireComponent(typeof(Rigidbody))]
public class TutorialPushBall : MonoBehaviour
{
    private const string PLAYER_TAG = "Player";
    private const string WALL_TAG = "Wall";
    private const string PORTAL_TAG = "Portal";

    // Bolas tocadas e ainda nao resolvidas; os portais puxam estas junto com os PushCube.TouchedCubes.
    public static readonly List<TutorialPushBall> TouchedBalls = new();

    [Header("Tipo")]
    [Tooltip("Power e Freeze valem em qualquer portal e disparam o Power Time / Freeze Time (sem os sons, pois o tutorial roda antes da musica).")]
    public TutorialCubeType BallType = TutorialCubeType.Ordinary;

    [Header("Push")]
    [Tooltip("Portal certo para a bola Ordinary.")]
    public PushType pushDirection;
    [Tooltip("Multiplicador de impacto baseado na velocidade da mao do jogador")]
    public float handVelocityMultiplier = 1.2f;
    [Tooltip("Velocidade minima/maxima da bola apos o soco")]
    public float minPushSpeed = 4f;
    public float maxPushSpeed = 15f;
    [Tooltip("Abaixo desta velocidade (m/s) a mao e considerada parada e usa a normal do contato")]
    public float minHandSpeedForDirection = 0.5f;
    [Tooltip("Tempo (s) apos o toque para a bola chegar em um portal; depois disso conta como falha.")]
    public float TouchTimeout = 4f;

    [Header("Feedback")]
    [Tooltip("Objeto com BreakCube. Nao pode ser filho desta bola, pois ela e desativada no sucesso.")]
    public Transform FeedbackObject;
    public float FailScaleDuration = 0.4f;

    [HideInInspector]
    public TutorialPushCube Owner;

    public bool IsTouched { get; private set; }
    public bool IsResolved { get; private set; }

    public Rigidbody Body => rb;
    public bool CanBePulled => IsTouched && !IsResolved && rb != null;
    private bool AnyPortal => BallType == TutorialCubeType.Power || BallType == TutorialCubeType.Freeze;

    private Transform initialParent;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 initialScale;
    private bool initialGravity;
    private bool initialized;

    private Rigidbody rb;
    private Vector3 hitPoint;
    private Coroutine timeoutRoutine;

    // Pares bola/mao com colisao desligada apos o toque; religados no Restore/OnDisable.
    private readonly List<(Collider, Collider)> ignoredPairs = new();

    private void Awake()
    {
        CacheInitialState();
        Lock();
    }

    private void OnEnable()
    {
        PowerEffect.OnPowerStarted += OnPowerExplosion;
    }

    private void CacheInitialState()
    {
        if (initialized) return;

        rb = GetComponent<Rigidbody>();
        initialGravity = rb.useGravity;
        initialParent = transform.parent;
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
        initialScale = transform.localScale;
        initialized = true;
    }

    // Antes do toque a bola e levada pelo grupo (tween); a fisica so serve para detectar o golpe.
    private void Lock()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeAll;
    }

    // Apos o toque a bola sai do grupo e passa a ser movida pela fisica, como o PushCube.
    private void Release()
    {
        transform.SetParent(null, true);
        rb.constraints = RigidbodyConstraints.None;
        rb.useGravity = initialGravity;
    }

    public void Restore()
    {
        CacheInitialState();

        StopTimeout();
        transform.DOKill();
        RestorePlayerCollision();
        TouchedBalls.Remove(this);

        IsTouched = false;
        IsResolved = false;
        hitPoint = transform.position;

        transform.SetParent(initialParent, false);
        transform.localPosition = initialPosition;
        transform.localRotation = initialRotation;
        transform.localScale = initialScale;
        Lock();
        gameObject.SetActive(true);

        if (FeedbackObject != null)
            FeedbackObject.gameObject.SetActive(false);
    }

    #region Collision

    private void OnCollisionEnter(Collision collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag(PLAYER_TAG))
        {
            HandVelocityTracker hand = other.GetComponentInParent<HandVelocityTracker>();
            Vector3 handVelocity = hand != null ? hand.Velocity : Vector3.zero;
            ContactPoint contact = collision.contacts[0];

            TryPlayerHit(other, contact.point, contact.normal, handVelocity);
            return;
        }

        if (!IsTouched || IsResolved) return;

        if (other.CompareTag(WALL_TAG))
        {
            Resolve(false);
            return;
        }

        if (other.CompareTag(PORTAL_TAG) && other.TryGetComponent(out PortalFeedback portal))
        {
            bool success = AnyPortal || portal.PushType == pushDirection;

            if (success)
                portal.HandleSuccess();
            else
                portal.HandleFail();

            Resolve(success);
        }
    }

    /// <summary>
    /// Batida do jogador (colisao normal ou sweep da raquete). Retorna false se a bola ja foi tocada ou resolvida.
    /// </summary>
    public bool TryPlayerHit(GameObject hitter, Vector3 point, Vector3 pushNormal, Vector3 handVelocity)
    {
        if (IsTouched || IsResolved) return false;
        IsTouched = true;

        hitPoint = point;

        Release();
        IgnorePlayerCollision(hitter);
        ApplyBounceForce(pushNormal, handVelocity);

        if (!TouchedBalls.Contains(this))
            TouchedBalls.Add(this);

        if (HandTouchFeedback.Instance != null)
            HandTouchFeedback.Instance.HandFeedback(hitter, true);

        timeoutRoutine = StartCoroutine(TimeoutRoutine());
        return true;
    }

    private void ApplyBounceForce(Vector3 pushNormal, Vector3 handVelocity)
    {
        float handSpeed = handVelocity.magnitude;

        // Direcao do golpe da mao; a normal do contato so e usada se a mao estiver praticamente parada
        Vector3 direction = handSpeed >= minHandSpeedForDirection
            ? handVelocity / handSpeed
            : pushNormal;

        float finalSpeed = Mathf.Clamp(handSpeed * handVelocityMultiplier, minPushSpeed, maxPushSpeed);

        rb.linearVelocity = direction * finalSpeed;
        rb.angularVelocity = Vector3.zero;

        if (TryGetComponent(out PushCubeFeedback pushFeedback))
            pushFeedback.HandlePushFeedback(transform.position, direction);
    }

    // Mesmo motivo do PushCube: em golpes rapidos a mao ja esta dentro da bola e a fisica anularia o empurrao.
    private void IgnorePlayerCollision(GameObject hitter)
    {
        HandVelocityTracker tracker = hitter.GetComponentInParent<HandVelocityTracker>();
        Transform handRoot = tracker != null ? tracker.transform : hitter.transform;

        Collider[] handColliders = handRoot.GetComponentsInChildren<Collider>(true);
        Collider[] ballColliders = GetComponentsInChildren<Collider>(true);

        foreach (Collider ball in ballColliders)
        {
            foreach (Collider hand in handColliders)
            {
                Physics.IgnoreCollision(ball, hand, true);
                ignoredPairs.Add((ball, hand));
            }
        }
    }

    private void RestorePlayerCollision()
    {
        foreach ((Collider ball, Collider hand) in ignoredPairs)
        {
            if (ball != null && hand != null)
                Physics.IgnoreCollision(ball, hand, false);
        }
        ignoredPairs.Clear();
    }

    private IEnumerator TimeoutRoutine()
    {
        yield return new WaitForSeconds(TouchTimeout);
        timeoutRoutine = null;
        Resolve(false);
    }

    private void StopTimeout()
    {
        if (timeoutRoutine == null) return;

        StopCoroutine(timeoutRoutine);
        timeoutRoutine = null;
    }

    #endregion

    #region Resultado

    // O grupo chegou no fim sem a bola ser tocada.
    public void Miss()
    {
        if (IsTouched) return;
        Resolve(false);
    }

    private void Resolve(bool success)
    {
        if (IsResolved) return;
        IsResolved = true;

        StopTimeout();
        TouchedBalls.Remove(this);

        if (success)
            SuccessFeedback();
        else
            FailFeedback();

        if (Owner != null)
            Owner.NotifyBallResolved(this, success);
    }

    private void SuccessFeedback()
    {
        if (PointController.Instance != null)
            PointController.Instance.IncreasePoint();

        if (ColorController.Instance != null)
            ColorController.Instance.TriggerSuccessFlash();

        if (FeedbackObject != null)
        {
            FeedbackObject.gameObject.SetActive(true);
            if (FeedbackObject.TryGetComponent<BreakCube>(out var breakCube))
                breakCube.TriggerExplosion(hitPoint, transform);
        }

        TriggerTypeEffect();

        gameObject.SetActive(false);
    }

    private void FailFeedback()
    {
        if (ColorController.Instance != null)
            ColorController.Instance.TriggerFailDim();

        Lock();
        transform.DOKill();
        transform.DOScale(Vector3.zero, FailScaleDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() => gameObject.SetActive(false));
    }

    private void TriggerTypeEffect()
    {
        switch (BallType)
        {
            case TutorialCubeType.Power:
                if (PowerEffect.Instance != null)
                {
                    PowerEffect.Instance.TriggerPowerSilent();
                    // Destroi as outras bolas na hora (via OnPowerStarted), sem depender do evento configurado na cena.
                    PowerEffect.Instance.Explosion();
                }
                break;

            case TutorialCubeType.Freeze:
                if (FreezeEffect.Instance != null)
                    FreezeEffect.Instance.TriggerFreezeSilent();
                break;
        }
    }

    // Explosao do Power: toda bola ainda em jogo (tocada ou nao) conta como sucesso.
    private void OnPowerExplosion()
    {
        if (IsResolved) return;

        if (!IsTouched)
            hitPoint = transform.position;

        Resolve(true);
    }

    #endregion

    private void OnDisable()
    {
        PowerEffect.OnPowerStarted -= OnPowerExplosion;
        StopTimeout();
        TouchedBalls.Remove(this);
        RestorePlayerCollision();
    }
}
