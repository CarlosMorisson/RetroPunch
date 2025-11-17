using UnityEngine;
using System; 

public class BossStateMachine : MonoBehaviour
{
    public static BossStateMachine Instance;
    public enum BossState
    {
        Idle,
        Attack,
    }
    private BossState _currentState;

    public BossState CurrentState
    {
        get { return _currentState; }
        set
        {
            if (_currentState != value)
            {
                _currentState = value;
                OnStateChange?.Invoke(_currentState); 
                Debug.Log($"Boss State Changed to: {_currentState}");
                if (_currentState == BossState.Idle)
                {
                    ResetAttackTimer();
                }
            }
        }
    }
    public static event Action<BossState> OnStateChange;

    [Header("Attack Timer Settings")]
    [SerializeField] private float minAttackCooldown = 3f; 
    [SerializeField] private float maxAttackCooldown = 7f; 

    private float _currentAttackTimer;
    private float _timeToNextAttack;

    void Start()
    {
        CurrentState = BossState.Idle; 
        Instance = this;

    }
    void Update()
    {
        if (CurrentState == BossState.Idle)
        {
            _currentAttackTimer -= Time.deltaTime;

            if (_currentAttackTimer <= 0)
            {
                CurrentState = BossState.Attack;
            }
        }

    }
    private void ResetAttackTimer()
    {
        _timeToNextAttack = UnityEngine.Random.Range(minAttackCooldown, maxAttackCooldown);
        _currentAttackTimer = _timeToNextAttack;
        Debug.Log($"Boss will attack in {_timeToNextAttack:F2} seconds.");
    }
}