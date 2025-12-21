using UnityEngine;
using DG.Tweening;

public class FeedbackCube : MonoBehaviour
{
    [Header("Settings")]
    public string groundTag = "Ground";
    public float bounceForce = 5f;
    public float scaleDuration = 0.5f;

    private Vector3 initialLocalPosition;
    private Vector3 initialScale;
    private Vector3 initialLocalRotation;
    private Rigidbody rb;
    private bool hasCollided;
    private bool tweenFinished;

    private FeedbackCubeManager manager;

    private void Awake()
    {
        initialLocalPosition = transform.localPosition;
        initialScale = transform.localScale;
        initialLocalRotation = transform.localEulerAngles;
        rb = GetComponent<Rigidbody>();
        manager = GetComponentInParent<FeedbackCubeManager>();
    }

    private void OnEnable()
    {
        hasCollided = false;
        tweenFinished = false;
        transform.localPosition = initialLocalPosition;
        transform.localScale = initialScale;
        transform.localEulerAngles = initialLocalRotation;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasCollided) return;

        if (collision.gameObject.CompareTag(groundTag))
        {
            hasCollided = true;
            HandleBounce();
        }
    }

    private void HandleBounce()
    {
        rb.AddForce(Vector3.up * bounceForce, ForceMode.Impulse);

        Sequence seq = DOTween.Sequence();

        seq.Append(transform.DOScale(Vector3.zero, scaleDuration).SetEase(Ease.InQuad));
        seq.OnComplete(() =>
        {
            tweenFinished = true;
            manager?.NotifyFinished();
        });
    }

    public void ResetCube()
    {
        DOTween.Kill(transform);
        transform.localPosition = initialLocalPosition;
        transform.localScale = initialScale;
        transform.localEulerAngles = initialLocalRotation;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        hasCollided = false;
        tweenFinished = false;
    }

    public bool IsFinished => tweenFinished;
}
