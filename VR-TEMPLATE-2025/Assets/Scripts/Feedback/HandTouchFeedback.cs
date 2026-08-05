using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Feedback;

public class HandTouchFeedback : MonoBehaviour
{
    public static HandTouchFeedback Instance;

    [System.Serializable]
    public class Hand
    {
        public string name;
        public GameObject handObject;

        [Tooltip("Lista de objetos com Renderer que recebem o material instanciado.")]
        public List<GameObject> targetRenderer;

        public Material handMaterial;
        public ParticleSystem successParticle;
        public ParticleSystem failParticle;
        public ParticleSystem touchParticle;

        [Header("Identificação")]
        [Tooltip("true = Controller, false = Hand (mão rastreada).")]
        public bool isController;
        [Tooltip("true = Right, false = Left.")]
        public bool isRight;

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

        if (rightHand != null) { rightHand.isController = false; rightHand.isRight = true; }
        if (leftHand != null) { leftHand.isController = false; leftHand.isRight = false; }
        if (rightController != null) { rightController.isController = true; rightController.isRight = true; }
        if (leftController != null) { leftController.isController = true; leftController.isRight = false; }

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
        if (hand == null || hand.handObject == null || hand.handMaterial == null ||
            hand.targetRenderer == null || hand.targetRenderer.Count == 0)
        {
            return;
        }

        hand.instantiatedMaterial = new Material(hand.handMaterial);
        hand.instantiatedMaterial.EnableKeyword("_EMISSION");

        if (hand.instantiatedMaterial.HasProperty(EmissionColorProperty))
        {
            hand.originalEmissionColor = hand.instantiatedMaterial.GetColor(EmissionColorProperty);
        }
        else
        {
            hand.originalEmissionColor = Color.black;
        }

        foreach (GameObject targetObj in hand.targetRenderer)
        {
            if (targetObj == null) continue;

            if (targetObj.TryGetComponent<Renderer>(out Renderer renderer))
            {
                renderer.material = hand.instantiatedMaterial;
            }
            else
            {
                Debug.LogWarning($"Nenhum Renderer encontrado no objeto: {targetObj.name}");
            }
        }
    }

    /// <summary>
    /// Retorna o slot (Hand) correspondente à combinação isController + isRight.
    /// Não considera o testHand, que é um slot manual/avulso.
    /// </summary>
    public Hand GetHandSlot(bool isController, bool isRight)
    {
        List<Hand> allHands = new List<Hand> { rightHand, leftHand, rightController, leftController };

        foreach (Hand h in allHands)
        {
            if (h != null && h.isController == isController && h.isRight == isRight)
            {
                return h;
            }
        }

        return null;
    }

    /// <summary>
    /// Valida o tipo de um Hand (Controller ou Hand) e sua lateralidade.
    /// Retorna false se o hand for nulo.
    /// </summary>
    public bool ValidateHandType(Hand hand, out bool isController, out bool isRight)
    {
        if (hand == null)
        {
            isController = false;
            isRight = false;
            return false;
        }

        isController = hand.isController;
        isRight = hand.isRight;
        return true;
    }

    /// <summary>
    /// Recebe os dados de uma HandSwitch (vinda do SwitchHandsController) e aplica
    /// no slot correto, com base em data.isController e na lateralidade informada (isRight).
    /// </summary>
    public void UpdateHandData(HandSwitch data, bool isRight)
    {
        if (data == null) return;

        Hand target = GetHandSlot(data.isController, isRight);

        if (target == null)
        {
            Debug.LogWarning($"[HandTouchFeedback] Nenhum slot encontrado para isController={data.isController}, isRight={isRight}");
            return;
        }

        target.handObject = data.handObject;
        target.targetRenderer = data.targetRenderer;
        target.handMaterial = data.handMaterial;
        target.successParticle = data.successParticle;
        target.failParticle = data.failParticle;
        target.touchParticle = data.touchParticle;

        InitializeHand(target);
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

        if (hand.touchParticle != null)
            hand.touchParticle.Play();

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
            if (hand.instantiatedMaterial == null) yield break;

            t += Time.deltaTime * transitionSpeed;
            Color blendedColor = Color.Lerp(currentMatColor, targetColor, t);
            hand.instantiatedMaterial.SetColor(EmissionColorProperty, blendedColor);
            yield return null;
        }

        yield return new WaitForSeconds(waitTime);

        if (hand.instantiatedMaterial == null) yield break;

        t = 0;
        currentMatColor = hand.instantiatedMaterial.GetColor(EmissionColorProperty);

        while (t < 1)
        {
            if (hand.instantiatedMaterial == null) yield break;

            t += Time.deltaTime * returnSpeed;
            Color blendedColor = Color.Lerp(currentMatColor, hand.originalEmissionColor, t);
            hand.instantiatedMaterial.SetColor(EmissionColorProperty, blendedColor);
            yield return null;
        }

        hand.instantiatedMaterial.SetColor(EmissionColorProperty, hand.originalEmissionColor);
    }
}