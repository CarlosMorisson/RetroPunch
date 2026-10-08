using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class ArrowStringInteractable : XRGrabInteractable
{
    [Header("Bow Reference")]
    public BowController bow;

    [Header("Pull Settings")]
    [Tooltip("Distância máxima que a corda pode ser puxada")]
    public float maxPullDistance = 0.5f;

    [Tooltip("Força mínima de pull para disparar (evita disparos acidentais)")]
    [Range(0f, 1f)]
    public float minPullToFire = 0.1f;

    public float PullAmount { get; private set; }

    private Vector3 _restPosition;

    private bool _isHeld;

    protected override void Awake()
    {
        base.Awake();

        movementType = MovementType.Instantaneous;
        trackPosition = true;
        trackRotation = false;
        throwOnDetach = false;

        _restPosition = transform.localPosition;
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        _isHeld = true;
        bow?.OnStringGrabbed();
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        _isHeld = false;

        if (PullAmount >= minPullToFire)
            bow?.Fire(PullAmount);
        else
            bow?.OnStringReleased(); 
        ResetString();
    }

    private void Update()
    {
        if (!_isHeld) return;

        float distance = Vector3.Distance(transform.position, bow.StringRestPoint.position);
        PullAmount = Mathf.Clamp01(distance / maxPullDistance);

        Vector3 pullDir = (transform.position - bow.StringRestPoint.position).normalized;
        float clampedDist = Mathf.Min(distance, maxPullDistance);
        transform.position = bow.StringRestPoint.position + pullDir * clampedDist;

        bow?.UpdatePull(PullAmount, transform.position);
    }

    private void ResetString()
    {
        PullAmount = 0f;
        transform.localPosition = _restPosition;
    }
}