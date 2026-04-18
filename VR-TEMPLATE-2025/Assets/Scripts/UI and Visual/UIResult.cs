using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
public class UIResult : MonoBehaviour
{
    public static UIResult Instance;
    [Header("Result")]
    [SerializeField]
    private GameObject resultPanel;
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
    [Header("FinalResult UI")]
    [SerializeField]
    private GameObject FinalResultPanel;
    [SerializeField]
    private TextMeshProUGUI finalResultText;
    [SerializeField]
    private TextMeshProUGUI finalConsecutiveText;
    [SerializeField]
    private TextMeshProUGUI highConsecutiveText;
    [SerializeField]
    private TextMeshProUGUI caloriesText;
    [SerializeField]
    private TextMeshProUGUI blockDestructedPercentText;


    private const float MULTIPLIER_VALUE = 1.2f;
    private const float PUNCH_TIME=0.5f;
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
    public void UpdateResult(int result)
    {
        resultText.text = result.ToString();
        resultText.transform.DOPunchScale(resultText.transform.localScale * MULTIPLIER_VALUE, PUNCH_TIME);
    }
    public void UpdateConsecutive(int consecutive)
    {
        consecutiveText.text = consecutive.ToString();
        consecutiveText.transform.DOPunchScale(consecutiveText.transform.localScale * MULTIPLIER_VALUE, PUNCH_TIME);
    }
    public void UpdateError(int error)
    {
        errorText.text = error.ToString();
        errorText.transform.DOPunchScale(errorText.transform.localScale * MULTIPLIER_VALUE, PUNCH_TIME);
    }
    public void UpdateMusicName(string name)
    {
        musicNameText.text = name;
        musicNameText.transform.DOPunchScale(musicNameText.transform.localScale * MULTIPLIER_VALUE, PUNCH_TIME);
    }
    public void UpdateCalorie(float calorie)=>caloriesText.text = calorie.ToString();
    private void Start()
    {
        PointController.Instance.OnAcceptInt += UpdateResult;
        PointController.Instance.OnConsecutiveInt += UpdateConsecutive;
        PointController.Instance.OnErrorInt += UpdateError;
    }
}
