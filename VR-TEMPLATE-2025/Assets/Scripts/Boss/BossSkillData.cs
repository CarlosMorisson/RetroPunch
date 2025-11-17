using UnityEngine;

[CreateAssetMenu(fileName = "NewBossSkillData", menuName = "Boss/Boss Skill Data")]
public class BossSkillData : ScriptableObject
{
    [Header("Skill Information")]
    public string skillName = "New Skill";
    [TextArea(3, 5)]
    public string description = "Description of the skill.";

    [Header("Attack Settings")]
    public float damage = 10f;
    public float duration = 1.5f; 
    public GameObject visualEffectPrefab; 
    public AudioClip soundEffect; 
}