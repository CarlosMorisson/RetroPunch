using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Representa uma opção de mão (mão nua, controle, luva, etc).
/// isController define se essa opção representa um Controller (true) ou uma Hand/mão rastreada (false).
/// A lateralidade (Left/Right) é definida pela posição dela dentro de um BothHands (LeftHand ou RightHand),
/// não por um campo aqui.
/// </summary>
[System.Serializable]
public class HandSwitch
{
    public string name;

    [Tooltip("GameObject 'pai' dessa opção. É ativado quando essa opção se torna a atual, e desativado quando não é.")]
    public GameObject parentObject;

    public GameObject handObject;

    [Tooltip("Lista de objetos com Renderer que recebem o material instanciado.")]
    public List<GameObject> targetRenderer;

    public Material handMaterial;
    public ParticleSystem successParticle;
    public ParticleSystem failParticle;
    public ParticleSystem touchParticle;

    [Header("Identificação")]
    [Tooltip("Marque true se essa opção representa um Controller. Deixe false se representa uma Hand (mão rastreada).")]
    public bool isController;

    [HideInInspector]
    public Material instantiatedMaterial;
    [HideInInspector]
    public Color originalEmissionColor;
}

/// <summary>
/// Par de opções (esquerda e direita) que representam um mesmo "conjunto" de mãos
/// (ex: "Mãos Nuas", "Controles", "Mãos com Luva").
/// </summary>
[System.Serializable]
public class BothHands
{
    public string name;
    public HandSwitch LeftHand;
    public HandSwitch RightHand;
}

/// <summary>
/// Controla qual BothHands está ativo no momento.
/// - Mantém uma lista de todas as opções disponíveis (bothHandsList).
/// - Randomiza a opção atual sem repetir até esgotar todas (usedBothHands),
///   resetando o ciclo quando todas já foram usadas.
/// - Ao definir o BothHands atual, ativa o parentObject do Left/Right escolhidos
///   e desativa o parentObject de todas as outras opções da lista.
/// - Propaga os dados da opção atual para o HandTouchFeedback, atualizando
///   o slot correto (rightHand / leftHand / rightController / leftController)
///   com base no isController da HandSwitch e na lateralidade (Left/Right do BothHands).
/// </summary>
public class SwitchHandsController : MonoBehaviour
{
    [Header("Opções Disponíveis")]
    public List<BothHands> bothHandsList = new List<BothHands>();

    [Header("Opção Atual")]
    public BothHands currentBothHands;

    [Header("Controle de Randomização (somente leitura)")]
    [SerializeField]
    private List<BothHands> usedBothHands = new List<BothHands>();

    void Awake()
    {
        DeactivateAllParents();
    }

    /// <summary>
    /// Escolhe aleatoriamente um BothHands da lista que ainda não foi usado no ciclo atual.
    /// Quando todos já tiverem sido usados, libera todos novamente (reseta o ciclo).
    /// </summary>
    public void RandomizeCurrentBothHands()
    {
        if (bothHandsList == null || bothHandsList.Count == 0)
        {
            Debug.LogWarning("[SwitchHandsController] bothHandsList está vazia.");
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
    /// Define o BothHands atual: ativa seus parentObjects, desativa os das outras opções
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
            Debug.LogWarning("[SwitchHandsController] HandTouchFeedback.Instance não encontrado na cena.");
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