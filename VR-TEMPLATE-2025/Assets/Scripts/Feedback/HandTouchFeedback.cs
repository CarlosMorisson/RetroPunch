using UnityEngine;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Feedback;

public class HandTouchFeedback : MonoBehaviour
{
    public static HandTouchFeedback Instance;

    [System.Serializable]
    public class Hand
    {
        public string name;
        public GameObject handObject;
        public GameObject targetRenderer;
        public Material handMaterial;
        public ParticleSystem successParticle;
        public ParticleSystem failParticle;

        [HideInInspector]
        public Material instantiatedMaterial;
        [HideInInspector]
        public Color originalEmissionColor;
    }

    [Header("Configurações das Mãos")]
    public Hand rightHand;
    public Hand leftHand;
    public Hand rightController;
    public Hand leftController;
    public Hand testHand;

    [Header("Configurações de Cores")]
    public Color successColor = Color.green;
    public Color failColor = Color.red;

    [Header("Configurações de Transição")]
    public float transitionSpeed = 5f;
    public float returnSpeed = 2f;
    public float waitTime = 0.2f;

    private static readonly int EmissionColorProperty = Shader.PropertyToID("_EmissionColor");

    private const float SUCCESS_HAPTIC_INTENSITY = 0.7f;
    private const float SUCCESS_HAPTIC_DURATION = 0.15f;
    private const float SUCCESS_HAPTIC_FREQUENCY = 0.5f;

    private const float FAIL_HAPTIC_INTENSITY = 0.4f;
    private const float FAIL_HAPTIC_DURATION = 0.25f;
    private const float FAIL_HAPTIC_FREQUENCY = 0.5f;

    void Awake()
    {
        Instance = this;
        InitializeHand(rightHand);
        InitializeHand(leftHand);
        InitializeHand(rightController);
        InitializeHand(leftController);
        InitializeHand(testHand);
    }

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(returnSpeed);
        successColor = ColorController.Instance.CurrentPrimary;
        failColor = ColorController.Instance.CurrentSecondary;
    }

    private void InitializeHand(Hand hand)
    {
        if (hand.handObject != null && hand.handMaterial != null && hand.targetRenderer != null)
        {
            if (hand.targetRenderer.TryGetComponent<Renderer>(out Renderer renderer))
            {
                hand.instantiatedMaterial = new Material(hand.handMaterial);
                renderer.material = hand.instantiatedMaterial;

                hand.instantiatedMaterial.EnableKeyword("_EMISSION");

                if (hand.instantiatedMaterial.HasProperty(EmissionColorProperty))
                {
                    hand.originalEmissionColor = hand.instantiatedMaterial.GetColor(EmissionColorProperty);
                }
                else
                {
                    hand.originalEmissionColor = Color.black;
                }
            }
            else
            {
                Debug.LogWarning($"Nenhum Renderer encontrado no objeto: {hand.targetRenderer.name}");
            }
        }
    }

    public void HandFeedback(GameObject handObj, bool success)
    {
        if (handObj == rightHand.handObject) StartCoroutine(ApplyFeedbackRoutine(rightHand, success));
        else if (handObj == leftHand.handObject) StartCoroutine(ApplyFeedbackRoutine(leftHand, success));
        else if (handObj == rightController.handObject) StartCoroutine(ApplyFeedbackRoutine(rightController, success));
        else if (handObj == leftController.handObject) StartCoroutine(ApplyFeedbackRoutine(leftController, success));
        else if (handObj == testHand.handObject) StartCoroutine(ApplyFeedbackRoutine(testHand, success));

        if (handObj != null && handObj.TryGetComponent<SimpleHapticFeedback>(out SimpleHapticFeedback simpleHaptic))
        {
            simpleHaptic.enabled = true;

            if (simpleHaptic.hapticImpulsePlayer != null)
            {
                if (success)
                {
                    simpleHaptic.hapticImpulsePlayer.SendHapticImpulse(SUCCESS_HAPTIC_INTENSITY, SUCCESS_HAPTIC_DURATION, SUCCESS_HAPTIC_FREQUENCY);
                }
                else
                {
                    simpleHaptic.hapticImpulsePlayer.SendHapticImpulse(FAIL_HAPTIC_INTENSITY, FAIL_HAPTIC_DURATION, FAIL_HAPTIC_FREQUENCY);
                }
            }
        }
    }

    private IEnumerator ApplyFeedbackRoutine(Hand hand, bool success)
    {
        if (hand.instantiatedMaterial == null) yield break;

        Color targetColor = success ? successColor : failColor;
        ParticleSystem targetParticle = success ? hand.successParticle : hand.failParticle;

        if (hand.successParticle != null) hand.successParticle.startColor = successColor;
        if (hand.failParticle != null) hand.failParticle.startColor = failColor;

        if (targetParticle != null)
        {
            targetParticle.Play();
        }
        float t = 0;
        Color currentMatColor = hand.instantiatedMaterial.GetColor(EmissionColorProperty);

        while (t < 1)
        {
            t += Time.deltaTime * transitionSpeed;
            Color blendedColor = Color.Lerp(currentMatColor, targetColor, t);
            hand.instantiatedMaterial.SetColor(EmissionColorProperty, blendedColor);
            yield return null;
        }

        yield return new WaitForSeconds(waitTime);
        t = 0;
        currentMatColor = hand.instantiatedMaterial.GetColor(EmissionColorProperty);

        while (t < 1)
        {
            t += Time.deltaTime * returnSpeed;
            Color blendedColor = Color.Lerp(currentMatColor, hand.originalEmissionColor, t);
            hand.instantiatedMaterial.SetColor(EmissionColorProperty, blendedColor);
            yield return null;
        }

        hand.instantiatedMaterial.SetColor(EmissionColorProperty, hand.originalEmissionColor);
    }
}