using UnityEngine;

public class FeedbackCubeManager : MonoBehaviour
{
    public string PrefabTag;
    private FeedbackCube[] cubes;
    private int finishedCount;

    private void Awake()
    {
        cubes = GetComponentsInChildren<FeedbackCube>(true);
    }

    private void OnEnable()
    {
        finishedCount = 0;
    }

    public void NotifyFinished()
    {
        finishedCount++;

        if (finishedCount >= cubes.Length)
        {
            ResetAndDisable();
        }
    }

    private void ResetAndDisable()
    {
        for (int i = 0; i < cubes.Length; i++)
        {
            cubes[i].ResetCube();
        }
        GameObject parent = transform.parent.gameObject;
        ObjectPooler.Instance.ReturnToPool(PrefabTag, parent);
    }
}
