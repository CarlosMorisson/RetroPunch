using UnityEngine;
using System.Collections.Generic; // Para List
using System.Linq; // Para ToArray e outras operações LINQ
using System; // Para Action

public class BossSkillController : MonoBehaviour
{
    [SerializeField] private BossStateMachine bossStateMachine; 
    [SerializeField] private Transform playerTransform; 

    [Header("Available Skills")]
    [SerializeField] private List<BossBaseSkill> availableSkills = new List<BossBaseSkill>();

    [SerializeField] private Animator BossAnimator;

    private BossBaseSkill _currentActiveSkill = null; 

    private 

    void OnEnable()
    {
        BossStateMachine.OnStateChange += HandleBossStateChange;
    }

    void OnDisable()
    {
        BossStateMachine.OnStateChange -= HandleBossStateChange;
    }

    void Start()
    {
        if (bossStateMachine == null)
        {
            bossStateMachine = GetComponent<BossStateMachine>();
            if (bossStateMachine == null)
            {
                Debug.LogError("BossStateMachine not found on this GameObject. Please assign it!");
            }
        }
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogError("Player not found! Please assign playerTransform in the Inspector or ensure the player has the 'Player' tag.");
            }
        }

        availableSkills = GetComponents<BossBaseSkill>().ToList();
        if (availableSkills.Count == 0)
        {
            Debug.LogWarning("No BossBaseSkill components found on this GameObject. Boss will not be able to attack.");
        }
    }

    private void HandleBossStateChange(BossStateMachine.BossState newState)
    {
        if (newState == BossStateMachine.BossState.Attack)
        {
            SelectAndActivateRandomSkill();
        }
        else if (newState == BossStateMachine.BossState.Idle)
        {
            _currentActiveSkill = null;
        }
    }
    private void SelectAndActivateRandomSkill()
    {
        if (availableSkills.Count == 0)
        {
            Debug.LogWarning("No skills available to attack!");
            bossStateMachine.CurrentState = BossStateMachine.BossState.Idle; 
            return;
        }

        int randomIndex = UnityEngine.Random.Range(0, availableSkills.Count);
        _currentActiveSkill = availableSkills[randomIndex];

        Debug.Log($"Boss selected skill: {_currentActiveSkill.skillData.skillName}");

        _currentActiveSkill.OnSkillActivated(transform, playerTransform);

        Invoke("OnSkillFinished", _currentActiveSkill.skillData.duration);
    }
    private void OnSkillFinished()
    {
        if (_currentActiveSkill != null)
        {
            Debug.Log($"Skill {_currentActiveSkill.skillData.skillName} finished.");
            _currentActiveSkill = null; 
        }
        bossStateMachine.CurrentState = BossStateMachine.BossState.Idle;
    }
}