using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events; // Necessário para UnityEvent, se ainda usar.

public class SkillButton : MonoBehaviour
{
    [Header("Icone da skill")]
    [SerializeField]
    private Image skillImage;
    [Header("Socket Material")]
    [SerializeField]
    private MeshRenderer meshRenderer;
    [SerializeField]
    private GameObject socketPivot;
    [Header("Overlay de Cooldown")]
    [SerializeField]
    private Image cooldownOverlay; 
    [SerializeField]
    private Material cooldownMaterial; 
    [SerializeField]
    private Material defaultEmptyMaterial; 

    public System.Action<SkillGemBase> OnSkillGemSet;
    public System.Action OnSkillGemCleared;

    private SkillGemBase _skillGem;
    private SkillBase _currentBoundSkill; 
    private Material _originalSocketMaterial; 

    public SkillGemBase SkillGemB
    {
        get => _skillGem;
        set
        {
            if (_skillGem != value)
            {
                _skillGem = value;
                UpdateSkillButtonProperties();

                if (_skillGem != null)
                {
                    OnSkillGemSet?.Invoke(_skillGem);
                    SkillController.Instance.UpdateSkillList(this);
                }
                else
                {
                    OnSkillGemCleared?.Invoke();
                }
            }
        }
    }

    private void Awake()
    {
        if (meshRenderer != null && meshRenderer.sharedMaterial != null)
        {
            _originalSocketMaterial = meshRenderer.sharedMaterial;
        }

        if (cooldownOverlay != null)
        {
            cooldownOverlay.fillAmount = 0;
            cooldownOverlay.gameObject.SetActive(false);
        }
    }


    public void SetSkillGem(SkillGemBase skillGem) => SkillGemB = skillGem;

    public void SetupSkillBinding(SkillBase skill)
    {
        if (_currentBoundSkill != null)
        {
            _currentBoundSkill.OnCooldownUpdated -= HandleSkillCooldownUpdated;
        }

        _currentBoundSkill = skill; 

        if (_currentBoundSkill != null)
        {
            _currentBoundSkill.OnCooldownUpdated += HandleSkillCooldownUpdated;

            HandleSkillCooldownUpdated(_currentBoundSkill.CurrentCooldown, _currentBoundSkill.TotalCooldown);
        }
        else
        {
            ResetCooldownFeedback();
        }
    }

    public void UnsubscribeFromSkillCooldown(SkillBase skill)
    {
        if (skill != null)
        {
            skill.OnCooldownUpdated -= HandleSkillCooldownUpdated;
        }
    }

    private void UpdateSkillButtonProperties()
    {
        if (_skillGem != null)
        {
            if (skillImage != null)
            {
                if (_skillGem.SkillBaseGem != null && _skillGem.SkillBaseGem.icon != null)
                {
                    skillImage.sprite = _skillGem.SkillBaseGem.icon;
                    skillImage.enabled = true;
                }
                else if (_skillGem.SkillGemIcon != null)
                {
                    skillImage.sprite = _skillGem.SkillGemIcon;
                    skillImage.enabled = true;
                }
                else
                {
                    skillImage.enabled = false;
                }
            }

            if (meshRenderer != null)
            {
                _originalSocketMaterial = _skillGem.GetRenderMaterial();
                meshRenderer.material = _originalSocketMaterial;
            }
        }
        else 
        {
            if (skillImage != null)
            {
                skillImage.sprite = null;
                skillImage.enabled = false;
            }
            if (meshRenderer != null)
            {
                meshRenderer.material = defaultEmptyMaterial; 
                _originalSocketMaterial = defaultEmptyMaterial; 
            }
            ResetCooldownFeedback();
            _currentBoundSkill = null; 
        }
    }
    private void HandleSkillCooldownUpdated(float currentCooldown, float totalCooldown)
    {
        if (cooldownOverlay == null || cooldownMaterial == null)
        {
            Debug.LogWarning("SkillButton: Cooldown overlay ou material de cooldown não configurado.");
            return;
        }

        if (currentCooldown > 0)
        {
            cooldownOverlay.gameObject.SetActive(true);
            float fillAmount = currentCooldown / totalCooldown;
            cooldownOverlay.fillAmount = fillAmount;

            float timeElapsed = totalCooldown - currentCooldown;
            float newFillAmount = timeElapsed / totalCooldown;
            socketPivot.gameObject.transform.localScale = 
                new Vector3(socketPivot.gameObject.transform.localScale.x, newFillAmount, socketPivot.gameObject.transform.localScale.z);

            if (meshRenderer != null && meshRenderer.material != cooldownMaterial)
            {
                meshRenderer.material = cooldownMaterial;
            }
        }
        else 
        {
            ResetCooldownFeedback();
        }
    }
    private void ResetCooldownFeedback()
    {
        if (cooldownOverlay != null)
        {
            cooldownOverlay.fillAmount = 0;
            cooldownOverlay.gameObject.SetActive(false);
        }
        if (meshRenderer != null)
        {
            meshRenderer.material = _originalSocketMaterial != null ? _originalSocketMaterial : defaultEmptyMaterial;
        }
    }
}