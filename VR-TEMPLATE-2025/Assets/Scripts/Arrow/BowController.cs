using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class BowController : MonoBehaviour
{
    [Header("References")]
    public Transform stringRestPoint;
    public Transform arrowSpawnPoint;
    public Transform leftSide;
    public Transform rightSide;

    [Header("String Line Renderers")]
    public LineRenderer leftStringLine;
    public LineRenderer rightStringLine;

    [Header("Aim Line")]
    public LineRenderer aimLine;
    public int aimLinePoints = 20;
    public float aimSimulationTime = 1.5f;

    [Header("Arrow Settings")]
    public float minFireForce = 5f;
    public float maxFireForce = 30f;

    [Header("Haptics")]
    [Range(0f, 1f)]
    public float hapticIntensity = 0.8f;
    public float hapticDuration = 0.1f;

    private const string PREFAB_TAG = "Arrow";
    public Transform StringRestPoint => stringRestPoint;

    private bool _isAiming;
    private ArrowStringInteractable _string;

    // Posição atual da corda (rest ou puxada)
    private Vector3 _currentStringPos;

    private void Awake()
    {
        _string = GetComponentInChildren<ArrowStringInteractable>();

        if (aimLine != null)
            aimLine.enabled = false;

        // Inicializa os line renderers da corda
        SetupStringLineRenderer(leftStringLine);
        SetupStringLineRenderer(rightStringLine);

        _currentStringPos = stringRestPoint != null ? stringRestPoint.position : Vector3.zero;
    }

    private void SetupStringLineRenderer(LineRenderer lr)
    {
        if (lr == null) return;
        lr.positionCount = 2;
        lr.enabled = true;
    }

    private void Update()
    {
        if (!_isAiming && stringRestPoint != null)
            _currentStringPos = stringRestPoint.position;

        UpdateStringLines();
    }

    private void UpdateStringLines()
    {
        if (leftStringLine != null && leftSide != null)
        {
            leftStringLine.SetPosition(0, leftSide.position);
            leftStringLine.SetPosition(1, _currentStringPos);
        }

        if (rightStringLine != null && rightSide != null)
        {
            rightStringLine.SetPosition(0, rightSide.position);
            rightStringLine.SetPosition(1, _currentStringPos);
        }
    }

    public void OnStringGrabbed()
    {
        _isAiming = true;

        if (aimLine != null)
            aimLine.enabled = true;
    }

    public void OnStringReleased()
    {
        _isAiming = false;

        if (aimLine != null)
            aimLine.enabled = false;

        if (stringRestPoint != null)
            _currentStringPos = stringRestPoint.position;
    }

    public void UpdatePull(float pullAmount, Vector3 stringPosition)
    {
        if (!_isAiming) return;

        _currentStringPos = stringPosition;

        UpdateAimLine(pullAmount);
    }

    public void Fire(float pullAmount)
    {
        _isAiming = false;

        if (aimLine != null)
            aimLine.enabled = false;

        if (stringRestPoint != null)
            _currentStringPos = stringRestPoint.position;

        float force = Mathf.Lerp(minFireForce, maxFireForce, pullAmount);

        GameObject arrow = ObjectPooler.Instance.SpawnFromPool(
            PREFAB_TAG,
            arrowSpawnPoint.position,
            arrowSpawnPoint.rotation
        );

        if (arrow.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = false;
            rb.AddForce(arrowSpawnPoint.forward * force, ForceMode.Impulse);
        }

        if (arrow.TryGetComponent<ArrowProjectile>(out var projectile))
            projectile.Initialize(arrowSpawnPoint.forward, force);

        TriggerHaptic();
    }

    private void UpdateAimLine(float pullAmount)
    {
        if (aimLine == null || arrowSpawnPoint == null) return;

        float force = Mathf.Lerp(minFireForce, maxFireForce, pullAmount);
        Vector3 startPos = arrowSpawnPoint.position;
        Vector3 startVel = arrowSpawnPoint.forward * force;

        aimLine.positionCount = aimLinePoints;

        for (int i = 0; i < aimLinePoints; i++)
        {
            float t = (float)i / (aimLinePoints - 1) * aimSimulationTime;
            Vector3 point = startPos
                + startVel * t
                + 0.5f * Physics.gravity * t * t;
            aimLine.SetPosition(i, point);
        }
    }

    private void TriggerHaptic()
    {
        if (_string == null) return;

        foreach (var interactor in _string.interactorsSelecting)
        {
            if (interactor is XRBaseInputInteractor inputInteractor)
                inputInteractor.SendHapticImpulse(hapticIntensity, hapticDuration);
        }
    }
}