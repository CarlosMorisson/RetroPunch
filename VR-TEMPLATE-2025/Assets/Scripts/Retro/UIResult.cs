using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class UIResult : MonoBehaviour
{
    public static UIResult Instance;
    [Header("Result")]
    [SerializeField]
    private TextMeshProUGUI resultText;
    [SerializeField]
    private TextMeshProUGUI consecutiveText;
    [SerializeField]
    private TextMeshProUGUI errorText;
    [Header("Music")]
    [SerializeField]
    private TextMeshProUGUI musicNameText;
    [SerializeField]
    private float musicLength;
    [SerializeField]
    private Slider musicSlider;
    private void Awake()
    {
        Instance = this;
    }
    public void UpdateSlider(float time) => musicSlider.value = time;
    public void SetSlider()
    {
        musicSlider.minValue = 0;
        musicSlider.maxValue = musicLength;
    }
    public void UpdateMusicLenght(float length)
    { 
        musicLength = length;
        SetSlider();
    }
    public void UpdateResult(float result) => resultText.text = result.ToString();
    public void UpdateConsecutive(float consecutive) => consecutiveText.text = consecutive.ToString();
    public void UpdateError(float error) => errorText.text = error.ToString();
    public void UpdateMusicName(string name) => musicNameText.text = name;
}
