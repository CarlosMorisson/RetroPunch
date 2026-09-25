using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

public class HandTouchFeedback : MonoBehaviour
{
    public static HandTouchFeedback Instance;

    [Header("Controlador de Mãos")]
    [SerializeField] private SwitchHandsController switchController;

    [Header("Mão de Teste (opcional)")]
    public HandSwitch testHand;

    [Header("Haptics dos Controles")]
    public HapticImpulsePlayer leftHapticPlayer;
    public HapticImpulsePlayer rightHapticPlayer;

    [Header("Configurações de Cores")]
    public Color successColor = Color.green;
    public Color failColor = Color.red;

    [Header("Configurações de Transição")]
    public float transitionSpeed = 5f;
    public float returnSpeed = 2f;
    public float waitTime = 0.2f;

    // Materiais instanciados do par de mãos atual, cacheados localmente para
    // garantir que a animação de feedback sempre modifique o material que
    // está de fato atribuído aos renderers (e não o asset compartilhado).
    private Material currentLeftInstancedMaterial;
    private Material currentRightInstancedMaterial;

    // Emissão original de cada instância (normal, power, freeze), capturada na criação.
    private readonly Dictionary<Material, Color> baseEmissionColors = new Dictionary<Material, Color>();

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
        InitializeAll();
        if (testHand != null) InitializeHandSwitch(testHand);
    }

    private IEnumerator Start()
    {
        // Garante o cache mesmo se o Awake do SwitchHandsController rodar
        // antes do nosso (Instance ainda nulo na hora do OnHandsSwitched).
        CacheCurrentHandsMaterials();

        yield return new WaitForSeconds(returnSpeed);
        successColor = ColorController.Instance.CurrentPrimary;
        failColor = ColorController.Instance.CurrentSecondary;
    }

    /// <summary>
    /// Inicializa os materiais de todos os HandSwitches das duas listas do SwitchHandsController
    /// (bothHandsList para Shoot e inputHandsList para outros modos).
    /// </summary>
    public void InitializeAll()
    {
        if (switchController == null) return;

        foreach (BothHands bh in switchController.bothHandsList)
        {
            if (bh == null) continue;
            InitializeHandSwitch(bh.LeftHand);
            InitializeHandSwitch(bh.RightHand);
        }

        foreach (BothHands bh in switchController.inputHandsList)
        {
            if (bh == null) continue;
            InitializeHandSwitch(bh.LeftHand);
            InitializeHandSwitch(bh.RightHand);
        }
    }

    /// <summary>
    /// Garante que o material instanciado do HandSwitch existe e está atribuído aos renderers.
    /// Idempotente: não recria o material se já estiver inicializado.
    /// </summary>
    public void InitializeHandSwitch(HandSwitch hand)
    {
        if (hand == null || hand.handObject == null || hand.handMaterial == null ||
            hand.targetRenderer == null || hand.targetRenderer.Count == 0)
            return;

        ApplyStateMaterial(hand);
    }

    /// <summary>
    /// Escolhe o material conforme o estado atual (Power > Freeze > Normal). Se a instância
    /// correspondente é null, cria a partir do material base; se já existe, apenas a insere
    /// nos renderers.
    /// </summary>
    private void ApplyStateMaterial(HandSwitch hand)
    {
        Material active;
        bool powered = PowerEffect.Instance != null && PowerEffect.Instance.isPowered;
        bool frozen = !powered && FreezeEffect.IsFrozen;

        if (powered && hand.powerHandMaterial != null)
            active = GetOrCreateInstance(ref hand.powerInstantiatedMaterial, hand.powerHandMaterial);
        else if (frozen && hand.freezeHandMaterial != null)
            active = GetOrCreateInstance(ref hand.freezeInstantiatedMaterial, hand.freezeHandMaterial);
        else
            active = GetOrCreateInstance(ref hand.instantiatedMaterial, hand.handMaterial);

        // Ao voltar ao normal, os dois particles são parados.
        SetParticle(hand.powerParticle, powered);
        SetParticle(hand.freezeParticle, frozen);

        hand.activeMaterial = active;
        hand.originalEmissionColor = baseEmissionColors[active];

        foreach (GameObject targetObj in hand.targetRenderer)
        {
            if (targetObj == null) continue;

            if (targetObj.TryGetComponent<Renderer>(out Renderer renderer))
                renderer.material = active;
            else
                Debug.LogWarning($"Nenhum Renderer encontrado no objeto: {targetObj.name}");
        }
    }

    private static void SetParticle(ParticleSystem particle, bool play)
    {
        if (particle == null) return;

        if (play)
        {
            if (!particle.isPlaying) particle.Play();
        }
        else if (particle.isPlaying)
        {
            particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private Material GetOrCreateInstance(ref Material instance, Material source)
    {
        if (instance == null)
        {
            instance = new Material(source);
            instance.EnableKeyword("_EMISSION");

            baseEmissionColors[instance] = instance.HasProperty(EmissionColorProperty)
                ? instance.GetColor(EmissionColorProperty)
                : Color.black;
        }

        return instance;
    }

    private void OnEnable()
    {
        PowerEffect.OnPowerChanged += HandleStateChanged;
        FreezeEffect.OnFreezeChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        PowerEffect.OnPowerChanged -= HandleStateChanged;
        FreezeEffect.OnFreezeChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(bool active)
    {
        // InitializeHandSwitch valida os campos e reaplica o material do estado atual.
        CacheCurrentHandsMaterials();
        InitializeAll();
        if (testHand != null) InitializeHandSwitch(testHand);
    }

    /// <summary>
    /// Chamado pelo SwitchHandsController ao trocar o par de mãos ativo.
    /// Garante que o novo par está inicializado antes do primeiro feedback.
    /// </summary>
    public void OnHandsSwitched(BothHands newHands)
    {
        if (newHands == null) return;
        InitializeHandSwitch(newHands.LeftHand);
        InitializeHandSwitch(newHands.RightHand);
        CacheCurrentHandsMaterials();
    }

    /// <summary>
    /// Garante que o par de mãos atual (switchController.currentBothHands) está
    /// inicializado e guarda localmente os materiais instanciados do Left/Right,
    /// para que a animação de feedback sempre atue sobre o material que está
    /// de fato atribuído aos renderers (e não sobre o asset compartilhado).
    /// </summary>
    public void CacheCurrentHandsMaterials()
    {
        if (switchController == null || switchController.currentBothHands == null) return;

        BothHands current = switchController.currentBothHands;

        InitializeHandSwitch(current.LeftHand);
        InitializeHandSwitch(current.RightHand);

        currentLeftInstancedMaterial = current.LeftHand?.activeMaterial;
        currentRightInstancedMaterial = current.RightHand?.activeMaterial;
    }

    /// <summary>
    /// Retorna o HandSwitch do currentBothHands que corresponde a isController + lateralidade.
    /// </summary>
    public HandSwitch GetHandSlot(bool isController, bool isRight)
    {
        if (switchController == null || switchController.currentBothHands == null) return null;

        BothHands current = switchController.currentBothHands;
        HandSwitch candidate = isRight ? current.RightHand : current.LeftHand;

        if (candidate != null && candidate.isController == isController)
            return candidate;

        return null;
    }

    /// <summary>
    /// Retorna o HandSwitch do currentBothHands (ou testHand) cujo handObject é o próprio handObj
    /// ou um ancestral dele (o collider que bateu no cubo pode estar em qualquer profundidade abaixo do handObject).
    /// </summary>
    private HandSwitch FindHandSwitch(GameObject handObj)
    {
        if (handObj == null) return null;

        if (switchController != null && switchController.currentBothHands != null)
        {
            BothHands current = switchController.currentBothHands;

            HandSwitch match = MatchesAncestor(handObj, current.LeftHand) ?? MatchesAncestor(handObj, current.RightHand);
            if (match != null) return match;
        }

        return MatchesAncestor(handObj, testHand);
    }

    private static HandSwitch MatchesAncestor(GameObject handObj, HandSwitch candidate)
    {
        if (candidate == null || candidate.handObject == null) return null;

        for (Transform t = handObj.transform; t != null; t = t.parent)
        {
            if (t.gameObject == candidate.handObject) return candidate;
        }

        return null;
    }

    public void HandFeedback(GameObject handObj, bool success)
    {
        HandSwitch hand = FindHandSwitch(handObj);
        if (hand == null) return;

        StartCoroutine(ApplyFeedbackRoutine(hand, success));

        HapticImpulsePlayer hapticPlayer = IsRightHand(hand) ? rightHapticPlayer : leftHapticPlayer;
        if (hapticPlayer != null)
        {
            if (success)
                hapticPlayer.SendHapticImpulse(SUCCESS_HAPTIC_INTENSITY, SUCCESS_HAPTIC_DURATION, SUCCESS_HAPTIC_FREQUENCY);
            else
                hapticPlayer.SendHapticImpulse(FAIL_HAPTIC_INTENSITY, FAIL_HAPTIC_DURATION, FAIL_HAPTIC_FREQUENCY);
        }
    }

    private bool IsRightHand(HandSwitch hand)
    {
        return switchController != null
            && switchController.currentBothHands != null
            && switchController.currentBothHands.RightHand == hand;
    }

    /// <summary>
    /// Retorna o material instanciado cacheado (currentLeft/RightInstancedMaterial) para
    /// o hand informado, se ele for parte do par atual. Cai para hand.instantiatedMaterial
    /// como fallback (ex: testHand, que não faz parte do currentBothHands).
    /// </summary>
    private Material GetCachedMaterial(HandSwitch hand)
    {
        if (switchController != null && switchController.currentBothHands != null)
        {
            if (switchController.currentBothHands.LeftHand == hand) return currentLeftInstancedMaterial;
            if (switchController.currentBothHands.RightHand == hand) return currentRightInstancedMaterial;
        }

        return hand.activeMaterial;
    }

    private IEnumerator ApplyFeedbackRoutine(HandSwitch hand, bool success)
    {
        Material mat = GetCachedMaterial(hand);
        if (mat == null) yield break;

        if (hand.touchParticle != null)
            hand.touchParticle.Play();

        Color targetColor = success ? successColor : failColor;
        ParticleSystem targetParticle = success ? hand.successParticle : hand.failParticle;

        if (hand.successParticle != null) hand.successParticle.startColor = successColor;
        if (hand.failParticle != null) hand.failParticle.startColor = failColor;

        if (targetParticle != null)
            targetParticle.Play();

        float t = 0;
        Color currentMatColor = mat.GetColor(EmissionColorProperty);

        while (t < 1)
        {
            if (mat == null) yield break;
            t += Time.deltaTime * transitionSpeed;
            mat.SetColor(EmissionColorProperty, Color.Lerp(currentMatColor, targetColor, t));
            yield return null;
        }

        yield return new WaitForSeconds(waitTime);

        if (mat == null) yield break;

        t = 0;
        currentMatColor = mat.GetColor(EmissionColorProperty);

        while (t < 1)
        {
            if (mat == null) yield break;
            t += Time.deltaTime * returnSpeed;
            mat.SetColor(EmissionColorProperty, Color.Lerp(currentMatColor, hand.originalEmissionColor, t));
            yield return null;
        }
        mat.SetColor(EmissionColorProperty, hand.originalEmissionColor);
    }
}
