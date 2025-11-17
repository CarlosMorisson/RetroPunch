using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class SubBossMovemment : MonoBehaviour
{
    [SerializeField]
    private List<Transform> Path = new();

    private const float TIME_TO_RUN=5;
    void Start()
    {
        Vector3[] pathPositions = new Vector3[Path.Count];
        for (int i = 0; i < Path.Count; i++)
        {
            pathPositions[i] = Path[i].position;
        }

        transform.DOPath(pathPositions, TIME_TO_RUN, PathType.CatmullRom, PathMode.Full3D)
                .SetLookAt(0.01f)
                .SetEase(Ease.InSine)
                .SetLoops(-1, LoopType.Restart);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
