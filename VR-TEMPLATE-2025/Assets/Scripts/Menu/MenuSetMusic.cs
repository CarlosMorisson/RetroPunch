using UnityEngine;

public class MenuSetMusic : MonoBehaviour
{
    [Header("Music Name")]
    public AudioClip MusicToSet;
    public void SetMusic()=>StageLoadController.Instance.ChooseMusic=MusicToSet;
}
