using UnityEngine;
using UnityEngine.XR.Content.Interaction;
using System.Collections;

[DisallowMultipleComponent]
public class RobotMovemment : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Arraste aqui o XRJoystick da cena")]
    [SerializeField] private XRJoystick joystick;

    [Header("Configurações de Movimento")]
    [Tooltip("Velocidade de movimento do objeto")]
    [SerializeField] private float moveSpeed = 2f;

    [Tooltip("Velocidade de rotação (quanto maior, mais rápido o robô gira)")]
    [SerializeField] private float rotationSpeed = 6f;

    [Tooltip("Suavização de resposta ao movimento (quanto maior, mais lento o ajuste)")]
    [SerializeField, Range(1f, 20f)] private float smoothness = 8f;

    [Tooltip("Espaço de movimento — Self = relativo ao objeto, World = absoluto")]
    [SerializeField] private Space movementSpace = Space.World;

    [Header("Limites de Movimento (opcional)")]
    [SerializeField] private bool useLimits = false;
    [SerializeField] private Vector3 minLimits = new Vector3(-5f, 0f, -5f);
    [SerializeField] private Vector3 maxLimits = new Vector3(5f, 0f, 5f);

    private Vector2 targetInput;
    private Vector2 smoothedInput;
    private Coroutine smoothRoutine;

    // ----------------------------- //
    private void OnEnable()
    {
        if (joystick == null)
        {
            Debug.LogWarning($"{nameof(RobotMovemment)}: Nenhum XRJoystick atribuído.");
            return;
        }

        joystick.onValueChangeX.AddListener(OnXChange);
        joystick.onValueChangeY.AddListener(OnYChange);
    }

    private void OnDisable()
    {
        if (joystick == null) return;

        joystick.onValueChangeX.RemoveListener(OnXChange);
        joystick.onValueChangeY.RemoveListener(OnYChange);
    }

    #region ChangeEvents
    private void OnXChange(float x)
    {
        targetInput.x = x;
        StartSmoothMotion();
    }

    private void OnYChange(float y)
    {
        targetInput.y = y;
        StartSmoothMotion();
    }
    #endregion

    #region MovementLogic
    private void StartSmoothMotion()
    {
        if (smoothRoutine != null)
            StopCoroutine(smoothRoutine);

        smoothRoutine = StartCoroutine(SmoothMovementCoroutine());
    }

    private IEnumerator SmoothMovementCoroutine()
    {
        while ((smoothedInput - targetInput).sqrMagnitude > 0.0001f || targetInput.sqrMagnitude > 0.0001f)
        {
            smoothedInput = Vector2.Lerp(smoothedInput, targetInput, Time.deltaTime * smoothness);

            Vector3 moveDir = new Vector3(smoothedInput.x, 0f, smoothedInput.y);

            if (moveDir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir.normalized, Vector3.up);

                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
            }

            transform.Translate(Vector3.forward * moveDir.magnitude * moveSpeed * Time.deltaTime, Space.Self);

            if (useLimits)
            {
                Vector3 pos = transform.position;
                pos.x = Mathf.Clamp(pos.x, minLimits.x, maxLimits.x);
                pos.y = Mathf.Clamp(pos.y, minLimits.y, maxLimits.y);
                pos.z = Mathf.Clamp(pos.z, minLimits.z, maxLimits.z);
                transform.position = pos;
            }

            yield return null;
        }

        smoothedInput = targetInput;
        smoothRoutine = null;
    }
    #endregion
}
