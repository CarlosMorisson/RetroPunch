using UnityEngine;
using System.Collections;

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

    private Color _originalColor;

    void Awake()
    {
        Instance=this;
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
        failColor= ColorController.Instance.CurrentSecondary;
    }
    private void InitializeHand(Hand hand)
    {
        if (hand.handObject != null && hand.handMaterial != null)
        {
            if (hand.targetRenderer.TryGetComponent<Renderer>(out Renderer renderer))
            {
                hand.instantiatedMaterial = new Material(hand.handMaterial);
                renderer.material = hand.instantiatedMaterial;

                _originalColor = hand.handMaterial.color;
            }
            else
            {
                Debug.Log($"Nenhum Renderer encontrado no objeto: {hand.targetRenderer.name}");
            }
        }
    }

    public void HandFeedback(GameObject handObj, bool success)
    {
        if (handObj == rightHand.handObject)
        {
            StartCoroutine(ApplyFeedbackRoutine(rightHand, success));
        }
        else if (handObj == leftHand.handObject)
        {
            StartCoroutine(ApplyFeedbackRoutine(leftHand, success));
        }
        else if (handObj == rightController.handObject)
        {
            StartCoroutine(ApplyFeedbackRoutine(rightController, success));
        }
        else if (handObj == leftController.handObject)
        {
            StartCoroutine(ApplyFeedbackRoutine(leftController, success));
        }
        else if (handObj == testHand.handObject)
        {
            StartCoroutine(ApplyFeedbackRoutine(testHand, success));
        }

    }

    private IEnumerator ApplyFeedbackRoutine(Hand hand, bool success)
    {
        Color targetColor = success ? successColor : failColor;
        ParticleSystem targetParticle = success ? hand.successParticle : hand.failParticle;
        hand.successParticle.startColor = successColor;
        hand.failParticle.startColor = failColor;
        if (targetParticle != null)
        {
            targetParticle.Play();
        }

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * transitionSpeed;
            hand.instantiatedMaterial.color = Color.Lerp(hand.instantiatedMaterial.color, targetColor, t);
            yield return null;
        }

        yield return new WaitForSeconds(waitTime);

        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * returnSpeed;
            hand.instantiatedMaterial.color = Color.Lerp(hand.instantiatedMaterial.color, _originalColor, t);
            yield return null;
        }

        hand.instantiatedMaterial.color = _originalColor;
    }
}