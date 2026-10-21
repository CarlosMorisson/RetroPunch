using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Events;
using Unity.XR.CoreUtils.Bindings;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

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
///
/// Modalidade — escuta o XRInputModalityManager e só considera os pares cujo
/// isController condiz com o que está sendo usado (Controller ou Hand Tracking).
/// Sempre que a modalidade muda, as mãos são escolhidas/sorteadas de novo.
/// </summary>
public class SwitchHandsController : MonoBehaviour
{
    [Header("Shoot — Op��es Randomizadas")]
    public List<BothHands> bothHandsList = new List<BothHands>();

    [Header("Outros Modos — Op��es de Input do Jogador")]
    [Tooltip("Cada entrada representa um tipo de input (ex: Controller, Hand Tracking). O jogador escolhe dinamicamente.")]
    public List<BothHands> inputHandsList = new List<BothHands>();

    [Header("Push — Opções de Input do Jogador")]
    [Tooltip("Mesma lógica do inputHandsList (Punch), mas com as mãos próprias do modo Push.")]
    public List<BothHands> pushHandsList = new List<BothHands>();

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

    [Header("Índice de Push Atual (somente leitura)")]
    [SerializeField] private int currentPushIndex = 0;

    [Header("Modalidade Atual (somente leitura)")]
    [Tooltip("true = Controller, false = Hand Tracking. Atualizado pelo XRInputModalityManager.")]
    [SerializeField] private bool usingController = true;

    /// <summary>true se o jogador está usando Controller, false se está usando Hand Tracking.</summary>
    public bool UsingController => usingController;

    public static SwitchHandsController Instance;

    private IEventBinding inputModeBinding;
    private Coroutine switchSlowRoutine;
    private const float DEFAULT_TIME_SCALE = 1f;
    private const string EQUIP_AUDIO = "equip";

    void Awake()
    {
        Instance = this;

        XRInputModalityManager.InputMode mode = XRInputModalityManager.currentInputMode.Value;
        if (mode != XRInputModalityManager.InputMode.None)
            usingController = mode == XRInputModalityManager.InputMode.MotionController;

        DeactivateAllParents();
        SelectHandsForCurrentModality();
    }

    private void OnEnable()
    {
        inputModeBinding = XRInputModalityManager.currentInputMode.Subscribe(OnInputModeChanged);
    }

    /// <summary>
    /// Chamado pelo XRInputModalityManager quando a modalidade muda.
    /// None (nada rastreado) é ignorado para não trocar as mãos à toa.
    /// </summary>
    private void OnInputModeChanged(XRInputModalityManager.InputMode mode)
    {
        if (mode == XRInputModalityManager.InputMode.None)
            return;

        bool controller = mode == XRInputModalityManager.InputMode.MotionController;
        if (controller == usingController)
            return;

        usingController = controller;
        SelectHandsForCurrentModality();
    }

    /// <summary>
    /// Escolhe as mãos do zero de acordo com a modalidade atual.
    /// Shoot: reinicia o ciclo de sorteio e o contador de tolerância.
    /// Push/Punch: seleciona a primeira opção da lista compatível com a modalidade.
    /// </summary>
    private void SelectHandsForCurrentModality()
    {
        switch (StageLoadController.Instance.gameType)
        {
            case GameType.Shoot:
                usedBothHands.Clear();
                randomizeCallCount = 0;

                // Sempre escolhe um par, sem passar pela tolerância
                BothHands first = PickRandomBothHands(null);
                if (first != null)
                {
                    SetCurrentBothHands(first);
                    nextBothHands = PickRandomBothHands(first);
                }
                UpdateNextIndicators();
                UpdateToleranceText();
                break;

            case GameType.Push:
                SetCurrentPushHands(currentPushIndex);
                HideToleranceTexts();
                break;

            case GameType.Punch:
            default:
                SetCurrentInputHands(currentInputIndex);
                HideToleranceTexts();
                break;
        }
    }

    /// <summary>
    /// Um BothHands é de Controller quando suas mãos são isController (usa a esquerda; a direita se a esquerda for nula).
    /// </summary>
    private static bool IsController(BothHands hands)
    {
        if (hands.LeftHand != null)
            return hands.LeftHand.isController;

        return hands.RightHand != null && hands.RightHand.isController;
    }

    private bool MatchesCurrentModality(BothHands hands)
    {
        return hands != null && IsController(hands) == usingController;
    }

    /// <summary>Esconde o contador de troca (só é usado no modo Shoot).</summary>
    private void HideToleranceTexts()
    {
        if (externalToleranceText != null)
            externalToleranceText.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        inputModeBinding?.Unbind();
        inputModeBinding = null;

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
    /// Atualiza o contador externo com quantas chamadas faltam pra próxima troca.
    /// </summary>
    private void UpdateToleranceText()
    {
        int remaining = RemainingToSwitch;

        if (externalToleranceText != null)
            externalToleranceText.text = remaining.ToString();

        OnRemainingChanged?.Invoke(remaining);
    }

    /// <summary>
    /// Faz a troca de fato: o next vira o atual, sorteia um novo next,
    /// toca o haptic da nova mão e inicia o slow motion.
    /// </summary>
    private void SwitchToNextBothHands()
    {
        BothHands target = MatchesCurrentModality(nextBothHands) ? nextBothHands : PickRandomBothHands(currentBothHands);
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
    /// Sorteia um BothHands de bothHandsList compatível com a modalidade atual (isController),
    /// que ainda não foi usado no ciclo, diferente de exclude.
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
            if (MatchesCurrentModality(item) && item != exclude && !usedBothHands.Contains(item))
                available.Add(item);
        }

        if (available.Count == 0)
        {
            usedBothHands.Clear();
            if (exclude != null)
                usedBothHands.Add(exclude);

            foreach (BothHands item in bothHandsList)
            {
                if (MatchesCurrentModality(item) && item != exclude)
                    available.Add(item);
            }
        }

        // Só existe uma opção compatível na lista: ela mesma é a próxima
        if (available.Count == 0)
        {
            if (MatchesCurrentModality(exclude))
                return exclude;

            Debug.LogWarning($"[SwitchHandsController] Nenhum BothHands em bothHandsList com isController = {usingController}.");
            return null;
        }

        BothHands chosen = available[Random.Range(0, available.Count)];
        usedBothHands.Add(chosen);
        return chosen;
    }

    /// <summary>
    /// Ativa o nextIndicator só nas mãos do nextBothHands e desativa nas demais de bothHandsList.
    /// Só funciona no modo Shoot; nos outros modos não mexe nos indicadores.
    /// O mesmo indicador pode ser compartilhado por pares de modalidades diferentes
    /// (ex: CONTROLLER-Shield e HANDSTRACKING-Shield), então primeiro junta quais devem
    /// ficar ativos e só depois aplica, para um par não desligar o indicador do outro.
    /// </summary>
    private void UpdateNextIndicators()
    {
        if (!IsShootMode() || bothHandsList == null)
            return;

        HashSet<GameObject> activeIndicators = new HashSet<GameObject>();
        if (nextBothHands != null && nextBothHands != currentBothHands)
        {
            AddNextIndicator(activeIndicators, nextBothHands.LeftHand);
            AddNextIndicator(activeIndicators, nextBothHands.RightHand);
        }

        foreach (BothHands bh in bothHandsList)
        {
            if (bh == null) continue;
            SetNextIndicatorActive(bh.LeftHand, activeIndicators);
            SetNextIndicatorActive(bh.RightHand, activeIndicators);
        }
    }

    private static void AddNextIndicator(HashSet<GameObject> indicators, HandSwitch hand)
    {
        // nextIndicator é opcional: nem toda mão tem um
        if (hand != null && hand.nextIndicator != null)
            indicators.Add(hand.nextIndicator);
    }

    private static void SetNextIndicatorActive(HandSwitch hand, HashSet<GameObject> activeIndicators)
    {
        if (hand == null || hand.nextIndicator == null)
            return;

        bool active = activeIndicators.Contains(hand.nextIndicator);
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
    /// Se o par do índice não for da modalidade atual, avança até o próximo compatível.
    /// </summary>
    public void SetCurrentInputHands(int index) => SelectInputHands(index, 1);

    /// <summary>Avan�a para o pr�ximo par de input compatível (com wrap).</summary>
    public void NextInputHands() => SelectInputHands(currentInputIndex + 1, 1);

    /// <summary>Volta para o par de input anterior compatível (com wrap).</summary>
    public void PreviousInputHands() => SelectInputHands(currentInputIndex - 1, -1);

    private void SelectInputHands(int index, int step)
    {
        if (TrySelectFromList(inputHandsList, "inputHandsList", index, step, out int selected))
            currentInputIndex = selected;
    }

    // ── Push ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Seleciona o par de mãos de pushHandsList pelo índice.
    /// O índice faz wrap circular (negativo e maior que o tamanho são tratados corretamente).
    /// Se o par do índice não for da modalidade atual, avança até o próximo compatível.
    /// </summary>
    public void SetCurrentPushHands(int index) => SelectPushHands(index, 1);

    /// <summary>Avança para o próximo par de push compatível (com wrap).</summary>
    public void NextPushHands() => SelectPushHands(currentPushIndex + 1, 1);

    /// <summary>Volta para o par de push anterior compatível (com wrap).</summary>
    public void PreviousPushHands() => SelectPushHands(currentPushIndex - 1, -1);

    private void SelectPushHands(int index, int step)
    {
        if (TrySelectFromList(pushHandsList, "pushHandsList", index, step, out int selected))
            currentPushIndex = selected;
    }

    // ── Comum ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Procura, a partir de index e andando de step em step (com wrap), o primeiro par da lista
    /// compatível com a modalidade atual e o define como atual.
    /// </summary>
    private bool TrySelectFromList(List<BothHands> list, string listName, int index, int step, out int selected)
    {
        selected = -1;

        if (list == null || list.Count == 0)
        {
            Debug.LogWarning($"[SwitchHandsController] {listName} está vazia.");
            return false;
        }

        int count = list.Count;
        for (int i = 0; i < count; i++)
        {
            int candidate = (((index + i * step) % count) + count) % count;
            if (MatchesCurrentModality(list[candidate]))
            {
                selected = candidate;
                SetCurrentBothHands(list[candidate]);
                return true;
            }
        }

        Debug.LogWarning($"[SwitchHandsController] Nenhum BothHands em {listName} com isController = {usingController}.");
        return false;
    }

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

        if (pushHandsList != null)
            foreach (BothHands bh in pushHandsList)
                SetParentActive(bh, false);
    }
}
