using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRSocketInteractor))]
public class SkillSocketController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField]
    private XRSocketInteractor _socketInteractor;
    [SerializeField]
    private SkillButton _skillButton;

    protected void Awake()
    {
        if (_socketInteractor == null)
        {
            _socketInteractor = GetComponent<XRSocketInteractor>();
        }

        if (_skillButton == null)
        {
            _skillButton = GetComponent<SkillButton>();
            if (_skillButton == null)
            {
                _skillButton = GetComponentInChildren<SkillButton>();
            }
            if (_skillButton == null)
            {
                Debug.LogError("SkillButton reference is missing in SkillSocketController or cannot be found automatically.", this);
            }
        }

        if (_socketInteractor == null)
        {
            Debug.LogError("XRSocketInteractor is missing in SkillSocketController. Please add it to this GameObject.", this);
        }
    }

    protected void OnEnable()
    {
        if (_socketInteractor != null)
        {
            _socketInteractor.selectEntered.AddListener(OnSocketSelectEntered);
            _socketInteractor.selectExited.AddListener(OnSocketSelectExited);
        }
    }

    protected void OnDisable()
    {
        if (_socketInteractor != null)
        {
            _socketInteractor.selectEntered.RemoveListener(OnSocketSelectEntered);
            _socketInteractor.selectExited.RemoveListener(OnSocketSelectExited);
        }


    }

    private void OnSocketSelectEntered(SelectEnterEventArgs args)
    {
        XRGrabInteractable grabInteractable = args.interactableObject as XRGrabInteractable;

        if (grabInteractable != null)
        {
            SkillGemBase skillGem = grabInteractable.GetComponent<SkillGemBase>();

            if (skillGem != null && _skillButton != null)
            {
                _skillButton.SetSkillGem(skillGem);
                Debug.Log($"SkillGem '{skillGem.name}' inserida no socket. SkillButton atualizado.");

            }
            else if (skillGem == null)
            {
                Debug.LogWarning($"O objeto '{grabInteractable.name}' inserido no socket NÃO possui um SkillGemBase. SkillButton não atualizado.", grabInteractable.transform);
            }
        }
        else
        {

            Debug.LogWarning("Objeto inserido no socket não é um XRGrabInteractable válido. SkillButton não atualizado.", args.interactableObject.transform);
        }
    }

    private void OnSocketSelectExited(SelectExitEventArgs args)
    {


        if (_skillButton != null)
        {
            _skillButton.SetSkillGem(null);
            Debug.Log("SkillGem removida do socket. SkillButton limpo.");
        }
    }

   
}