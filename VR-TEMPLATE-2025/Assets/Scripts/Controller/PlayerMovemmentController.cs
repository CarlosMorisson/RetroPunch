using UnityEngine;

public class VRCalorieEstimator : MonoBehaviour
{
    public static VRCalorieEstimator Instance;
    [Header("Configurações de Usuário")]
    [SerializeField] private float userWeightKg = 70f; 

    [Header("Referências de Hardware")]
    [SerializeField] private Transform head;
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;

    [Header("Resultados (Somente Leitura)")]
    [SerializeField] private float totalCaloriesBurned;
    [SerializeField] private float currentMET;

    private const float BasalMET = 1.0f; 
    private const float ActiveModifier = 0.05f; 

    private Vector3 lastHeadPos;
    private Vector3 lastLeftHandPos;
    private Vector3 lastRightHandPos;
    public bool Stop { private get; set; }
    void Start()
    {
        Instance = this;
        if (head) lastHeadPos = head.localPosition;
        if (leftHand) lastLeftHandPos = leftHand.localPosition;
        if (rightHand) lastRightHandPos = rightHand.localPosition;
    }

    void Update()
    {
        if(Stop) return;
        CalculateEnergy();
    }

    private void CalculateEnergy()
    {
        if (!head || !leftHand || !rightHand) return;

        float headSpeed = (head.localPosition - lastHeadPos).magnitude / Time.deltaTime;
        float leftHandSpeed = (leftHand.localPosition - lastLeftHandPos).magnitude / Time.deltaTime;
        float rightHandSpeed = (rightHand.localPosition - lastRightHandPos).magnitude / Time.deltaTime;

        float totalMovement = headSpeed + leftHandSpeed + rightHandSpeed;

        currentMET = BasalMET + (totalMovement * ActiveModifier);

        currentMET = Mathf.Clamp(currentMET, 1.0f, 8.0f);
        float caloriesPerSecond = (currentMET * 3.5f * userWeightKg) / (200f * 60f);

        totalCaloriesBurned += caloriesPerSecond * Time.deltaTime;

        lastHeadPos = head.localPosition;
        lastLeftHandPos = leftHand.localPosition;
        lastRightHandPos = rightHand.localPosition;
    }
    public float GetTotalCalories()
    {
        Stop = true;
        return totalCaloriesBurned;
    }
}