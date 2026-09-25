using UnityEngine;
using System.Collections.Generic;

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

    [Header("Controle de Randomiza��o (somente leitura)")]
    [SerializeField] private List<BothHands> usedBothHands = new List<BothHands>();

    [Header("�ndice de Input Atual (somente leitura)")]
    [SerializeField] private int currentInputIndex = 0;

    void Awake()
    {
        DeactivateAllParents();

        if (StageLoadController.Instance.gameType == GameType.Shoot)
            RandomizeCurrentBothHands();
        else
            SetCurrentInputHands(currentInputIndex);
    }

    // ── Shoot ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Escolhe aleatoriamente um BothHands de bothHandsList que ainda n�o foi usado no ciclo.
    /// Quando todos tiverem sido usados, reseta o ciclo e escolhe novamente.
    /// </summary>
    [ContextMenu("Teste Randomiza")]
    public void RandomizeCurrentBothHands()
    {
        if (bothHandsList == null || bothHandsList.Count == 0)
        {
            Debug.LogWarning("[SwitchHandsController] bothHandsList est� vazia.");
            return;
        }

        List<BothHands> available = new List<BothHands>();
        foreach (BothHands item in bothHandsList)
        {
            if (!usedBothHands.Contains(item))
                available.Add(item);
        }

        if (available.Count == 0)
        {
            usedBothHands.Clear();
            available.AddRange(bothHandsList);
        }

        BothHands chosen = available[Random.Range(0, available.Count)];
        usedBothHands.Add(chosen);
        SetCurrentBothHands(chosen);
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
