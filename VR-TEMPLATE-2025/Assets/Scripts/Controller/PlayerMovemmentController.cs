using UnityEngine;

/// <summary>
/// Estima calorias pela fórmula ACSM: kcal/min = MET * 3.5 * peso(kg) / 200.
/// O MET é estimado pela velocidade média das mãos (socos) e da cabeça (esquivas/agachamentos),
/// suavizada no tempo e mapeada entre "em pé parado" e "boxe intenso" (Compendium of Physical Activities).
/// </summary>
public class VRCalorieEstimator : MonoBehaviour
{
    public static VRCalorieEstimator Instance;
    [Header("Configurações de Usuário")]
    [SerializeField] private float userWeightKg = 70f;

    [Header("Referências de Hardware")]
    [SerializeField] private Transform head;
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;

    [Header("Calibração de MET")]
    [Tooltip("MET em pé, quase sem se mexer.")]
    [SerializeField] private float restMET = 1.3f;
    [Tooltip("MET em ritmo de boxe/sparring intenso.")]
    [SerializeField] private float maxMET = 9.0f;
    [Tooltip("Atividade (m/s) abaixo da qual o jogador é considerado parado.")]
    [SerializeField] private float idleActivity = 0.5f;
    [Tooltip("Atividade (m/s) considerada esforço máximo.")]
    [SerializeField] private float intenseActivity = 6.0f;
    [Tooltip("Peso do movimento da cabeça: mover a cabeça = mover o corpo inteiro (agachar/esquivar).")]
    [SerializeField] private float headWeight = 3.0f;
    [Tooltip("Tempo (s) para o gasto responder à mudança de intensidade.")]
    [SerializeField] private float smoothingTime = 2.0f;

    [Header("Resultados (Somente Leitura)")]
    [SerializeField] private float totalCaloriesBurned;
    [SerializeField] private float currentMET;
    [SerializeField] private float smoothedActivity;

    // Velocidades acima disso são falhas de tracking (teleporte/perda de controle), não movimento real.
    private const float MaxTrackedSpeed = 10f;
    // Frames maiores que isso (travadas, headset tirado) são ignorados.
    private const float MaxFrameTime = 0.1f;

    private Vector3 lastHeadPos;
    private Vector3 lastLeftHandPos;
    private Vector3 lastRightHandPos;
    private bool saved;

    private const string MAIN_TOTAL_CALORIES = "MainTotalCalories";
    public bool Stop { private get; set; }
    void Start()
    {
        Instance = this;
        currentMET = restMET;
        CachePositions();
    }

    void Update()
    {
        if(Stop) return;
        CalculateEnergy();
    }

    private void CalculateEnergy()
    {
        if (!head || !leftHand || !rightHand) return;

        // Tempo real: o movimento do jogador e o gasto de energia não desaceleram com o slow motion/freeze.
        float dt = Time.unscaledDeltaTime;
        if (dt <= 0f || dt > MaxFrameTime)
        {
            CachePositions();
            return;
        }

        float headSpeed = Speed(head.localPosition, lastHeadPos, dt);
        float leftHandSpeed = Speed(leftHand.localPosition, lastLeftHandPos, dt);
        float rightHandSpeed = Speed(rightHand.localPosition, lastRightHandPos, dt);
        CachePositions();

        float activity = leftHandSpeed + rightHandSpeed + headSpeed * headWeight;

        // O metabolismo não reage frame a frame; usa a média móvel da intensidade.
        smoothedActivity = Mathf.Lerp(smoothedActivity, activity, 1f - Mathf.Exp(-dt / smoothingTime));

        float intensity = Mathf.InverseLerp(idleActivity, intenseActivity, smoothedActivity);
        currentMET = Mathf.Lerp(restMET, maxMET, intensity);

        float caloriesPerSecond = (currentMET * 3.5f * userWeightKg) / (200f * 60f);
        totalCaloriesBurned += caloriesPerSecond * dt;
    }

    private static float Speed(Vector3 current, Vector3 last, float dt)
    {
        return Mathf.Min((current - last).magnitude / dt, MaxTrackedSpeed);
    }

    private void CachePositions()
    {
        if (head) lastHeadPos = head.localPosition;
        if (leftHand) lastLeftHandPos = leftHand.localPosition;
        if (rightHand) lastRightHandPos = rightHand.localPosition;
    }

    public float GetTotalCalories()
    {
        Stop = true;
        if (!saved)
        {
            saved = true;
            float calories = PlayerPrefs.GetFloat(MAIN_TOTAL_CALORIES) + totalCaloriesBurned;
            PlayerPrefs.SetFloat(MAIN_TOTAL_CALORIES, calories);
            PlayerPrefs.Save();
        }
        return totalCaloriesBurned;
    }
}
