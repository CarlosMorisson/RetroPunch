using UnityEngine;

public class CubeMovemment : MonoBehaviour
{
    public enum MoveAxis
    {
        X,
        Y,
        Z
    }

    [Header("Movement Axis")]
    public MoveAxis axis = MoveAxis.X;
    public bool useLocalSpace = true;

    [Header("Speed")]
    public float normalSpeed = 2f;
    public float boostSpeed = 10f;
    [HideInInspector]
    public float enableSpeed = 2f;

    [Header("Boost Control")]
    public Transform boostEndPoint;

    private bool boostFinished = false;

    private const string FINAL_BOOST = "FinalBoost";
    private void Awake()=> enableSpeed = normalSpeed;

    private void Start() => boostEndPoint = GameObject.FindGameObjectWithTag(FINAL_BOOST).transform;

    private void OnEnable()
    {
        boostFinished = false;
    }

    void Update()
    {
        float currentSpeed = boostFinished ? normalSpeed : boostSpeed;

        Vector3 direction = GetDirection();
        Vector3 movement = direction * currentSpeed * Time.deltaTime;

        if (useLocalSpace)
            transform.Translate(movement, Space.Self);
        else
            transform.Translate(movement, Space.World);

        CheckBoostEnd();
    }

    Vector3 GetDirection()
    {
        switch (axis)
        {
            case MoveAxis.Y: return Vector3.up;
            case MoveAxis.Z: return Vector3.forward;
            default: return Vector3.right;
        }
    }

    void CheckBoostEnd()
    {
        if (boostFinished || boostEndPoint == null)
            return;

        float currentPos = GetAxisValue(transform.position);
        float targetPos = GetAxisValue(boostEndPoint.position);

        if (currentPos >= targetPos)
        {
            boostFinished = true;
        }
    }

    float GetAxisValue(Vector3 pos)
    {
        switch (axis)
        {
            case MoveAxis.Y: return pos.y;
            case MoveAxis.Z: return pos.z;
            default: return pos.x;
        }
    }
}
