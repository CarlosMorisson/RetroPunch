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
        // Tween de fragmento terminando depois que o cubo já voltou ao pool (ou foi reutilizado):
        // não devolver o cubo novo ao pool.
        if (!gameObject.activeInHierarchy)
            return;

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
