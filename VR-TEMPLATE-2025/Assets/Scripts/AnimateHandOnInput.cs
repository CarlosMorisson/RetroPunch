using UnityEngine;
using UnityEngine.InputSystem;

public enum HandPoseOverride
{
    None,
    Grip
}

public class AnimateHandOnInput : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public InputActionProperty triggerValue;
    public InputActionProperty gripValue;

    [Tooltip("Grip: mantém a mão fechada (Grip = 1). None: usa o valor do controle.")]
    public HandPoseOverride poseOverride = HandPoseOverride.None;

    public Animator handAnimator;
    void Start()
    {
        handAnimator=GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        float trigger = triggerValue.action.ReadValue<float>();
        float grip = poseOverride == HandPoseOverride.Grip ? 1f : gripValue.action.ReadValue<float>();

        handAnimator.SetFloat("Trigger", trigger);
        handAnimator.SetFloat("Grip", grip);
    }
}
