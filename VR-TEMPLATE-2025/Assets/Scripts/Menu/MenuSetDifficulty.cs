using UnityEngine;

public class MenuSetDifficulty : MonoBehaviour
{
    public void SetEasy()=>StageLoadController.Instance.SetEasy();
    public void SetNormal()=>StageLoadController.Instance.SetNormal();
    public void SetHard() => StageLoadController.Instance.SetHard();
    public void SetTutorial()=>StageLoadController.Instance?.SetTutorial();
}
