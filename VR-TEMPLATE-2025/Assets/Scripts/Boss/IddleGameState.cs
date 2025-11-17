using UnityEngine;

public class IddleGameState : MonoBehaviour
{
    public Transform playerTransform; 
    [Header("Rotação")]
    [Range(0,10)]
    public float rotationSpeed = 5f;

    public bool isCheckingY;

    private BossStateMachine.BossState _currentBossState;

    public Animator SkillAnimation;

    private const string IDLE_ANIM = "Idle";

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
    }

    private void HandleBossStateChange(BossStateMachine.BossState newState)
    {
        _currentBossState = newState;
        Debug.Log($"GameStateValidator received new boss state: {newState} and {gameObject.name}");
        SkillAnimation.CrossFade(IDLE_ANIM, 0);
    }

    void Update()
    {
        if (_currentBossState == BossStateMachine.BossState.Idle && playerTransform != null)
        {
            LookAtPlayer();
        }
    }

    private void LookAtPlayer()
    {
        Vector3 directionToPlayer = playerTransform.position - transform.position;

        if(!isCheckingY)
            directionToPlayer.y = 0; 

        if (directionToPlayer == Vector3.zero) return; 

        Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}