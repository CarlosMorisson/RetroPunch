using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Content.Interaction;


[Serializable]
public class SkillButtonBindingXR
{
    [Header("Associação de botão e skill")]
    public string id;
    public XRPushButton button;
    public SkillBase skill;
    public SkillButton skillButton;
    public void UpdateBind(SkillBase skillBase) => skill = skillBase;
}

public class SkillController : MonoBehaviour
{
    public static SkillController Instance;
    [Header("Local de instanciar")]
    [SerializeField]
    private Transform instantiatePosition;

    [Header("Configuração de Skills")]
    [Space(15)]
    [SerializeField]
    private List<SkillButtonBindingXR> skillBindings = new();
    [SerializeField]
    private float currentMana = 100f;

    private Dictionary<XRPushButton, SkillBase> skillDictionary = new();

    [Header("Eventos Globais de Skill")]
    [Space(15)]
    public UnityEvent<SkillBase> onSkillPressed;
    public UnityEvent<SkillBase> onSkillCastSuccess;
    public UnityEvent<SkillBase> onSkillCastFailed;

    private void Awake()
    {
        Instance = this;
        InitializeSkillDictionary();
    }

    private void Update()
    {
        foreach (var binding in skillBindings)
        {
            binding.skill?.TickCooldown(Time.deltaTime);
        }
    }

    private void InitializeSkillDictionary()
    {
        UpdateSkillDictionary();
    }

    public void UpdateSkillDictionary()
    {
        foreach (var binding in skillBindings)
        {
            if (binding.button != null)
            {
                binding.button.onPress.RemoveAllListeners();
            }
            binding.skillButton?.UnsubscribeFromSkillCooldown(binding.skill);
        }

        skillDictionary.Clear();

        foreach (var binding in skillBindings)
        {
            if (binding.button != null && binding.skill != null)
            {
                skillDictionary[binding.button] = binding.skill;
                binding.button.onPress.AddListener(() => OnButtonPressed(binding.button));
                binding.skillButton?.SetupSkillBinding(binding.skill);
            }
            else if (binding.skillButton != null)
            {
                binding.skillButton.SetupSkillBinding(null);
            }
        }
        Debug.Log("SkillController: Dicionário de skills atualizado e binds reconfigurados.");
    }

    public void UpdateSkillList(SkillButton updatedSkillButton)
    {
        SkillButtonBindingXR bindToUpdate = skillBindings.FirstOrDefault(bind => bind.skillButton == updatedSkillButton);

        SkillBase skillBase = updatedSkillButton.SkillGemB.SkillBaseGem;

        if (bindToUpdate != null)
        {

            bindToUpdate.UpdateBind(skillBase); 

            bindToUpdate.skillButton?.SetupSkillBinding(skillBase);

            UpdateSkillDictionary();

            Debug.Log($"SkillController: Bind para '{updatedSkillButton.name}' atualizado. Nova Skill: {(skillBase != null ? skillBase.skillName : "NULA")}");
        }
        else
        {
            Debug.LogWarning($"SkillController: SkillButton '{updatedSkillButton.name}' não encontrado na lista de binds. Adicione-o ao SkillController no Inspector.");
        }
    }

    public SkillBase GetSkillInSlot(int index)
    {
        if (index >= 0 && index < skillBindings.Count)
        {
            return skillBindings[index].skill;
        }
        return null;
    }
    public List<SkillBase> GetAllEquippedSkills()
    {
        return skillBindings.Select(bind => bind.skill).Where(skill => skill != null).ToList();
    }
    private void OnButtonPressed(XRPushButton button)
    {
        if (!skillDictionary.TryGetValue(button, out var skill))
        {
            Debug.LogWarning($"Nenhuma skill associada ao botão {button.name}");
            return;
        }

        onSkillPressed?.Invoke(skill);

        TryUseSkill(skill);
    }

    private void TryUseSkill(SkillBase skill)
    {
        if (skill.TryCast(instantiatePosition.gameObject, currentMana))
        {
            currentMana -= skill.manaCost;
            Debug.Log($"Skill '{skill.skillName}' usada com sucesso!");

            onSkillCastSuccess?.Invoke(skill);
        }
        else
        {
            Debug.Log($"Falha ao usar skill '{skill.skillName}' (sem mana ou cooldown)");
            onSkillCastFailed?.Invoke(skill);
        }
    }

    private void OnDestroy()
    {
        foreach (var binding in skillBindings)
        {
            if (binding.button != null)
                binding.button.onPress.RemoveAllListeners();
            binding.skillButton?.UnsubscribeFromSkillCooldown(binding.skill);
        }
    }
}