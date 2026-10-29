using UnityEngine;

public enum TutorialCubeType
{
    Ordinary,
    Power,
    Freeze
}

// Cubo simples do tutorial: ao ser golpeado da os feedbacks, pontua e avisa o TutorialPunchCube.
// Pode ser restaurado para o estado inicial quando o step reinicia.
public class TutorialCube : MonoBehaviour, IHandHittable
{
    private const string PLAYER_TAG = "Player";

    [Header("Tipo")]
    [Tooltip("Power e Freeze disparam o Power Time / Freeze Time (sem os sons, pois o tutorial roda antes da musica).")]
    public TutorialCubeType CubeType = TutorialCubeType.Ordinary;

    [Header("Feedback")]
    public Transform FeedbackObject;

    [HideInInspector]
    public TutorialPunchCube Owner;

    public bool IsHit { get; private set; }

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 initialScale;
    private bool initialized;
    private Rigidbody rb;

    private void Awake()
    {
        CacheInitialState();

        // O cubo e movido pelo grupo (tween); a fisica so serve para detectar o golpe.
        // Travar posicao/rotacao impede que colisoes empurrem ou girem o cubo.
        if (TryGetComponent(out rb))
        {
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    private void CacheInitialState()
    {
        if (initialized) return;

        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
        initialScale = transform.localScale;
        initialized = true;
    }

    public void Restore()
    {
        CacheInitialState();

        IsHit = false;
        ResetPhysics();
        transform.localPosition = initialPosition;
        transform.localRotation = initialRotation;
        transform.localScale = initialScale;
        gameObject.SetActive(true);

        if (FeedbackObject != null)
            FeedbackObject.gameObject.SetActive(false);
    }

    private void ResetPhysics()
    {
        if (rb == null || rb.isKinematic) return;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag(PLAYER_TAG)) return;

        Hit(collision.gameObject, collision.contacts[0].point);
    }

    public bool TryHandAssistHit(GameObject hitter, Vector3 handPosition, Vector3 contactPoint)
    {
        return Hit(hitter, contactPoint);
    }

    private bool Hit(GameObject hitter, Vector3 point)
    {
        if (IsHit) return false;
        IsHit = true;

        ResetPhysics();

        if (AudioController.Instance != null)
            AudioController.Instance.Play("Hit");

        if (HandTouchFeedback.Instance != null)
            HandTouchFeedback.Instance.HandFeedback(hitter, true);

        if (PointController.Instance != null)
            PointController.Instance.IncreasePoint();

        if (ColorController.Instance != null)
            ColorController.Instance.TriggerSuccessFlash();

        if (FeedbackObject != null)
        {
            FeedbackObject.gameObject.SetActive(true);
            if (FeedbackObject.TryGetComponent<BreakCube>(out var breakCube))
                breakCube.TriggerExplosion(point, transform);
        }

        TriggerTypeEffect();

        gameObject.SetActive(false);

        if (Owner != null)
            Owner.NotifyCubeHit(this);

        return true;
    }

    private void TriggerTypeEffect()
    {
        switch (CubeType)
        {
            case TutorialCubeType.Power:
                if (PowerEffect.Instance != null)
                    PowerEffect.Instance.TriggerPowerSilent();
                break;

            case TutorialCubeType.Freeze:
                if (FreezeEffect.Instance != null)
                    FreezeEffect.Instance.TriggerFreezeSilent();
                break;
        }
    }
}
