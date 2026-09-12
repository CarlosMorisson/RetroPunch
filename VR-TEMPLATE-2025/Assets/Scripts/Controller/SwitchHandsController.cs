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
    public ParticleSystem successParticle;
    public ParticleSystem failParticle;
    public ParticleSystem touchParticle;

    [Header("Identifica��o")]
    [Tooltip("Marque true se essa op��o representa um Controller. Deixe false se representa uma Hand (m�o rastreada).")]
    public bool isController;

    [HideInInspector]
    public Material instantiatedMaterial;
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
/// - Mant�m uma lista de todas as op��es dispon�veis (bothHandsList).
/// - Randomiza a op��o atual sem repetir at� esgotar todas (usedBothHands),
///   resetando o ciclo quando todas j� foram usadas.
/// - Ao definir o BothHands atual, ativa o parentObject do Left/Right escolhidos
///   e desativa o parentObject de todas as outras op��es da lista.
/// - Propaga os dados da op��o atual para o HandTouchFeedback, atualizando
///   o slot correto (rightHand / leftHand / rightController / leftController)
///   com base no isController da HandSwitch e na lateralidade (Left/Right do BothHands).
/// </summary>

public class SwitchHandsController : MonoBehaviour
{
    [Header("Op��es Dispon�veis")]
    public List<BothHands> bothHandsList = new List<BothHands>();

    [Header("Op��o Atual")]
    public BothHands currentBothHands;

    [Header("Controle de Randomiza��o (somente leitura)")]
    [SerializeField]
    private List<BothHands> usedBothHands = new List<BothHands>();

    void Awake()
    {
        if(GameType.Shoot!=StageLoadController.Instance.gameType)
            return;
        DeactivateAllParents();
        RandomizeCurrentBothHands();
    }

    /// <summary>
    /// Escolhe aleatoriamente um BothHands da lista que ainda n�o foi usado no ciclo atual.
    /// Quando todos j� tiverem sido usados, libera todos novamente (reseta o ciclo).
    /// </summary>
    [ContextMenu("Teste Randomiza")]
    public void RandomizeCurrentBothHands()
    {
        if(StageLoadController.Instance.gameType != GameType.Shoot)
            return;
        if (bothHandsList == null || bothHandsList.Count == 0)
        {
            Debug.LogWarning("[SwitchHandsController] bothHandsList est� vazia.");
            return;
        }

        List<BothHands> available = new List<BothHands>();
        foreach (BothHands item in bothHandsList)
        {
            if (!usedBothHands.Contains(item))
            {
                available.Add(item);
            }
        }
        if (available.Count == 0)
        {
            usedBothHands.Clear();
            available.AddRange(bothHandsList);
        }

        int randomIndex = Random.Range(0, available.Count);
        BothHands chosen = available[randomIndex];

        usedBothHands.Add(chosen);

        SetCurrentBothHands(chosen);
    }

    /// <summary>
    /// Define o BothHands atual: ativa seus parentObjects, desativa os das outras op��es
    /// e propaga os dados para o HandTouchFeedback.
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

        ApplyToHandTouchFeedback(newHands);
    }

    private void SetParentActive(BothHands hands, bool active)
    {
        if (hands == null) return;

        if (hands.LeftHand != null && hands.LeftHand.parentObject != null)
        {
            hands.LeftHand.parentObject.SetActive(active);
        }

        if (hands.RightHand != null && hands.RightHand.parentObject != null)
        {
            hands.RightHand.parentObject.SetActive(active);
        }
    }

    private void DeactivateAllParents()
    {
        if (bothHandsList == null) return;

        foreach (BothHands bh in bothHandsList)
        {
            SetParentActive(bh, false);
        }
    }

    /// <summary>
    /// Envia LeftHand e RightHand do BothHands atual para o HandTouchFeedback,
    /// que decide o slot correto (rightHand/leftHand/rightController/leftController)
    /// com base em isController + lateralidade.
    /// </summary>
    private void ApplyToHandTouchFeedback(BothHands hands)
    {
        if (HandTouchFeedback.Instance == null)
        {
            Debug.LogWarning("[SwitchHandsController] HandTouchFeedback.Instance n�o encontrado na cena.");
            return;
        }

        if (hands.LeftHand != null)
        {
            HandTouchFeedback.Instance.UpdateHandData(hands.LeftHand, isRight: false);
        }

        if (hands.RightHand != null)
        {
            HandTouchFeedback.Instance.UpdateHandData(hands.RightHand, isRight: true);
        }
    }
}