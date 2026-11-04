using UnityEngine;

public class FeedbackCubeManager : MonoBehaviour
{
    public string PrefabTag;
    [Tooltip("Desligado: ao terminar so desativa o feedback, sem devolver o pai ao pool (cubos do tutorial, que nao vem do pool).")]
    public bool ReturnToPoolOnFinish = true;
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

        if (!ReturnToPoolOnFinish)
        {
            gameObject.SetActive(false);
            return;
        }

        GameObject parent = transform.parent.gameObject;

        // BreakCube de um ponto do MultiPunchCube: o pai é o PunchArea, não o root do pool.
        // Devolver o pai desativava o PunchArea, que nunca era reativado, e o cubo voltava do pool sem o ponto.
        CubeMovemment poolRoot = GetComponentInParent<CubeMovemment>();
        if (poolRoot != null && poolRoot.gameObject != parent)
        {
            gameObject.SetActive(false);
            return;
        }

        ObjectPooler.Instance.ReturnToPool(PrefabTag, parent);
    }
}
