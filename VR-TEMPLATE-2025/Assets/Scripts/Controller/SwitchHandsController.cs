using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Events;

/// <summary>
/// Representa uma op��o de m�o (m�o nua, controle, luva, etc).
/// isController define se essa op��o representa um Controller (true) ou uma Hand/m�o rastreada (false).
/// A lateralidade (Left/Right) � definida pela posi��o dela dentro de um BothHands (LeftHand ou RightHand),
/// n�o por um campo aqui.
/// </summary>
[System.Serializable]
public class HandSwitch
{
    public string name;

    [Tooltip("GameObject 'pai' dessa op��o. � ativado quando essa op��o se torna a atual, e desativado quando n�o �.")]
    public GameObject parentObject;

    public GameObject handObject;

    [Tooltip("Lista de objetos com Renderer que recebem o material instanciado.")]
    public List<GameObject> targetRenderer;

    public Material handMaterial;
    public Material powerHandMaterial;
    public Material freezeHandMaterial;
    public ParticleSystem successParticle;
    public ParticleSystem failParticle;
    public ParticleSystem touchParticle;
    [Tooltip("Toca enquanto o Power Time estiver ativo.")]
    public ParticleSystem powerParticle;
    [Tooltip("Toca enquanto o Freeze Time estiver ativo.")]
    public ParticleSystem freezeParticle;

    [Tooltip("Mostra quantos golpes faltam pra trocar de mão (baseado no randomizeTolerance). Desativado fora do modo Shoot.")]
    public TextMeshProUGUI toleranceText;

    [Tooltip("Ativado quando essa opção é a PRÓXIMA a entrar (modo Shoot). Não pode ser filho do parentObject, senão fica desativado junto com ele.")]
    public GameObject nextIndicator;

    [Header("Identifica��o")]
    [Tooltip("Marque true se essa op��o representa um Controller. Deixe false se representa uma Hand (m�o rastreada).")]
    public bool isController;

    [HideInInspector]
    public Material instantiatedMaterial;
    [HideInInspector]
    public Material powerInstantiatedMaterial;
    [HideInInspector]
    public Material freezeInstantiatedMaterial;
    /// <summary>Instância atualmente atribuída aos renderers (normal, power ou freeze).</summary>
    [HideInInspector]
    public Material activeMaterial;
    [HideInInspector]
    public Color originalEmissionColor;
}

/// <summary>
/// Par de op��es (esquerda e direita) que representam um mesmo "conjunto" de m�os
/// (ex: "M�os Nuas", "Controles", "M�os com Luva").
/// </summary>
[System.Serializable]
public class BothHands
{
    public string name;
    public HandSwitch LeftHand;
    public HandSwitch RightHand;
}

/// <summary>
/// Controla qual BothHands est� ativo no momento.
///
/// GameType.Shoot — usa bothHandsList com randomiza��o sem repeti��o.
///
/// Outros GameTypes — usa inputHandsList, que representa as op��es de input
/// dispon�veis ao jogador (ex: Controller, Hand Tracking). O jogador pode
/// trocar dinamicamente via SetCurrentInputHands / NextInputHands / PreviousInputHands.
///
/// Em ambos os casos, SetCurrentBothHands ativa os parentObjects do par escolhido,
/// desativa todos os outros (das duas listas) e notifica o HandTouchFeedback.
/// </summary>
public class SwitchHandsController : MonoBehaviour
{
    [Header("Shoot — Op��es Randomizadas")]
    public List<BothHands> bothHandsList = new List<BothHands>();

    [Header("Outros Modos — Op��es de Input do Jogador")]
    [Tooltip("Cada entrada representa um tipo de input (ex: Controller, Hand Tracking). O jogador escolhe dinamicamente.")]
    public List<BothHands> inputHandsList = new List<BothHands>();

    [Header("Op��o Atual")]
    public BothHands currentBothHands;

    [Header("Tolerância de Randomização")]
    [Tooltip("Quantas chamadas de RandomizeCurrentBothHands são ignoradas antes de trocar as mãos. A troca acontece quando o contador passa desse valor. 0 = troca em toda chamada.")]
    [Min(0)] public int randomizeTolerance = 0;

    [Header("Controle de Randomiza��o (somente leitura)")]
    [SerializeField] private List<BothHands> usedBothHands = new List<BothHands>();
    [SerializeField] private int randomizeCallCount = 0;

    [Header("Próxima Opção (somente leitura)")]
    [Tooltip("Par que vai entrar na próxima troca (modo Shoot).")]
    public BothHands nextBothHands;

    [Header("Slow Motion na Troca")]
    [Tooltip("Time scale aplicado no instante da troca. Vai subindo até 1 ao longo de switchSlowDuration.")]
    [Range(0.01f, 1f)] public float switchTimeScale = 0.5f;
    [Tooltip("Tempo (em segundos reais) para o time scale voltar ao normal. 0 = sem slow motion.")]
    [Min(0f)] public float switchSlowDuration = 1f;

    [Header("Contador Externo")]
    [Tooltip("Texto opcional, fora das mãos, que mostra quantos golpes faltam para a troca.")]
    public TextMeshProUGUI externalToleranceText;
    [Tooltip("Disparado sempre que o número de golpes restantes muda.")]
    public UnityEvent<int> OnRemainingChanged;

    /// <summary>Quantos golpes faltam para a próxima troca (modo Shoot).</summary>
    public int RemainingToSwitch => randomizeTolerance + 1 - randomizeCallCount;

    [Header("�ndice de Input Atual (somente leitura)")]
    [SerializeField] private int currentInputIndex = 0;

    public static SwitchHandsController Instance;

    private Coroutine switchSlowRoutine;
    private const float DEFAULT_TIME_SCALE = 1f;
    private const string EQUIP_AUDIO = "equip";

    void Awake()
    {
        Instance = this;

        DeactivateAllParents();

        // Na inicialização sempre escolhe um par, sem passar pela tolerância
        if (StageLoadController.Instance.gameType == GameType.Shoot)
        {
            BothHands first = PickRandomBothHands(null);
            if (first != null)
            {
                SetCurrentBothHands(first);
                nextBothHands = PickRandomBothHands(first);
            }
            UpdateNextIndicators();
            UpdateToleranceText();
        }
        else
        {
            SetCurrentInputHands(currentInputIndex);
            SetToleranceTextActive(currentBothHands, false);
            if (externalToleranceText != null)
                externalToleranceText.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        // Não deixa o jogo preso em slow motion se o objeto for desativado no meio da transição
        if (switchSlowRoutine != null)
        {
            StopCoroutine(switchSlowRoutine);
            switchSlowRoutine = null;
            if (!FreezeEffect.IsActive)
                Time.timeScale = DEFAULT_TIME_SCALE;
        }
    }

    // ── Shoot ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Escolhe aleatoriamente um BothHands de bothHandsList que ainda n�o foi usado no ciclo.
    /// Quando todos tiverem sido usados, reseta o ciclo e escolhe novamente.
    /// Só troca de fato quando o número de chamadas passa de randomizeTolerance; aí zera o contador.
    /// </summary>
    [ContextMenu("Teste Randomiza")]
    public void RandomizeCurrentBothHands()
    {
        if(GameType.Shoot!=StageLoadController.Instance.gameType)
            return;
        randomizeCallCount++;

        if (randomizeCallCount > randomizeTolerance)
        {
            randomizeCallCount = 0;
            SwitchToNextBothHands();
        }

        UpdateToleranceText();
    }

    /// <summary>
    /// Atualiza o texto das duas mãos do currentBothHands e o contador externo
    /// com quantas chamadas faltam pra próxima troca.
    /// </summary>
    private void UpdateToleranceText()
    {
        int remaining = RemainingToSwitch;
        string value = remaining.ToString();

        if (externalToleranceText != null)
            externalToleranceText.text = value;

        OnRemainingChanged?.Invoke(remaining);

        if (currentBothHands == null)
            return;

        if (currentBothHands.LeftHand?.toleranceText != null)
            currentBothHands.LeftHand.toleranceText.text = value;

        if (currentBothHands.RightHand?.toleranceText != null)
            currentBothHands.RightHand.toleranceText.text = value;
    }

    private void SetToleranceTextActive(BothHands hands, bool active)
    {
        if (hands == null)
            return;

        if (hands.LeftHand?.toleranceText != null)
            hands.LeftHand.toleranceText.gameObject.SetActive(active);

        if (hands.RightHand?.toleranceText != null)
            hands.RightHand.toleranceText.gameObject.SetActive(active);
    }

    /// <summary>
    /// Faz a troca de fato: o next vira o atual, sorteia um novo next,
    /// toca o haptic da nova mão e inicia o slow motion.
    /// </summary>
    private void SwitchToNextBothHands()
    {
        BothHands target = nextBothHands ?? PickRandomBothHands(currentBothHands);
        if (target == null)
            return;

        SetCurrentBothHands(target);
        nextBothHands = PickRandomBothHands(currentBothHands);
        UpdateNextIndicators();

        if (HandTouchFeedback.Instance != null)
            HandTouchFeedback.Instance.PlaySwitchHaptic();

        if (AudioController.Instance != null)
            AudioController.Instance.Play(EQUIP_AUDIO);

        StartSwitchSlowMotion();
    }

    /// <summary>
    /// Sorteia um BothHands de bothHandsList que ainda não foi usado no ciclo, diferente de exclude.
    /// Quando todos tiverem sido usados, reseta o ciclo (mantendo exclude como usado,
    /// para não repetir o mesmo par em sequência).
    /// </summary>
    private BothHands PickRandomBothHands(BothHands exclude)
    {
        if (bothHandsList == null || bothHandsList.Count == 0)
        {
            Debug.LogWarning("[SwitchHandsController] bothHandsList está vazia.");
            return null;
        }

        List<BothHands> available = new List<BothHands>();
        foreach (BothHands item in bothHandsList)
        {
            if (item != null && item != exclude && !usedBothHands.Contains(item))
                available.Add(item);
        }

        if (available.Count == 0)
        {
            usedBothHands.Clear();
            if (exclude != null)
                usedBothHands.Add(exclude);

            foreach (BothHands item in bothHandsList)
            {
                if (item != null && item != exclude)
                    available.Add(item);
            }
        }

        // Só existe uma opção na lista: ela mesma é a próxima
        if (available.Count == 0)
            return exclude;

        BothHands chosen = available[Random.Range(0, available.Count)];
        usedBothHands.Add(chosen);
        return chosen;
    }

    /// <summary>
    /// Ativa o nextIndicator só nas mãos do nextBothHands e desativa nas demais de bothHandsList.
    /// Só funciona no modo Shoot; nos outros modos não mexe nos indicadores.
    /// </summary>
    private void UpdateNextIndicators()
    {
        if (!IsShootMode() || bothHandsList == null)
            return;

        foreach (BothHands bh in bothHandsList)
        {
            if (bh == null) continue;
            SetNextIndicatorActive(bh, nextBothHands != null && bh == nextBothHands && bh != currentBothHands);
        }
    }

    private void SetNextIndicatorActive(BothHands hands, bool active)
    {
        if (hands == null) return;

        SetNextIndicatorActive(hands.LeftHand, active);
        SetNextIndicatorActive(hands.RightHand, active);
    }

    private static void SetNextIndicatorActive(HandSwitch hand, bool active)
    {
        // nextIndicator é opcional: nem toda mão tem um
        if (hand == null || hand.nextIndicator == null)
            return;

        if (hand.nextIndicator.activeSelf != active)
            hand.nextIndicator.SetActive(active);
    }

    private static bool IsShootMode()
    {
        return StageLoadController.Instance != null
            && StageLoadController.Instance.gameType == GameType.Shoot;
    }

    private void StartSwitchSlowMotion()
    {
        // O Freeze Time tem prioridade sobre o time scale
        if (switchSlowDuration <= 0f || FreezeEffect.IsActive)
            return;

        if (switchSlowRoutine != null)
            StopCoroutine(switchSlowRoutine);

        switchSlowRoutine = StartCoroutine(SwitchSlowMotionRoutine());
    }

    /// <summary>
    /// Coloca o time scale em switchTimeScale e vai subindo até 1 ao longo de switchSlowDuration (tempo real).
    /// </summary>
    private IEnumerator SwitchSlowMotionRoutine()
    {
        float time = 0f;
        Time.timeScale = switchTimeScale;

        while (time < switchSlowDuration)
        {
            // Freeze começou no meio: ele assume o controle do time scale
            if (FreezeEffect.IsActive)
            {
                switchSlowRoutine = null;
                yield break;
            }

            time += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(time / switchSlowDuration);
            t = t * t * (3f - 2f * t);

            Time.timeScale = Mathf.Lerp(switchTimeScale, DEFAULT_TIME_SCALE, t);

            yield return null;
        }

        // Freeze pode ter começado durante o último frame: não sobrescreve o time scale dele
        if (!FreezeEffect.IsActive)
            Time.timeScale = DEFAULT_TIME_SCALE;

        switchSlowRoutine = null;
    }

    // ── Input do Jogador (n�o-Shoot) ─────────────────────────────────────────

    /// <summary>
    /// Seleciona o par de m�os de inputHandsList pelo �ndice.
    /// O �ndice faz wrap circular (negativo e maior que o tamanho s�o tratados corretamente).
    /// </summary>
    public void SetCurrentInputHands(int index)
    {
        if (inputHandsList == null || inputHandsList.Count == 0)
        {
            Debug.LogWarning("[SwitchHandsController] inputHandsList est� vazia.");
            return;
        }

        currentInputIndex = ((index % inputHandsList.Count) + inputHandsList.Count) % inputHandsList.Count;
        SetCurrentBothHands(inputHandsList[currentInputIndex]);
    }

    /// <summary>Avan�a para o pr�ximo par de input (com wrap).</summary>
    public void NextInputHands() => SetCurrentInputHands(currentInputIndex + 1);

    /// <summary>Volta para o par de input anterior (com wrap).</summary>
    public void PreviousInputHands() => SetCurrentInputHands(currentInputIndex - 1);

    // ── Comum ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Define o BothHands atual: desativa todos os parents das duas listas,
    /// ativa o par escolhido e notifica o HandTouchFeedback.
    /// </summary>
    public void SetCurrentBothHands(BothHands newHands)
    {
        if (newHands == null)
        {
            Debug.LogWarning("[SwitchHandsController] Tentativa de definir currentBothHands nulo.");
            return;
        }

        currentBothHands = newHands;

        DeactivateAllParents();
        SetParentActive(newHands, true);

        if (HandTouchFeedback.Instance != null)
            HandTouchFeedback.Instance.OnHandsSwitched(newHands);
    }

    private void SetParentActive(BothHands hands, bool active)
    {
        if (hands == null) return;

        if (hands.LeftHand?.parentObject != null)
            hands.LeftHand.parentObject.SetActive(active);

        if (hands.RightHand?.parentObject != null)
            hands.RightHand.parentObject.SetActive(active);
    }

    private void DeactivateAllParents()
    {
        if (bothHandsList != null)
            foreach (BothHands bh in bothHandsList)
                SetParentActive(bh, false);

        if (inputHandsList != null)
            foreach (BothHands bh in inputHandsList)
                SetParentActive(bh, false);
    }
}
