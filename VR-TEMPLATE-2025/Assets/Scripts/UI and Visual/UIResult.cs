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

    private Vector3 _resultTextInitialScale,
        _errorTextInitialScale,
        _consecutiveTextInitialScale;

    private Vector3 _resultScale,
        _errorScale,
        _consecutiveScale;


    private const float MULTIPLIER_VALUE = 1.2f;
    private const float PUNCH_TIME=0.1f;
    private void Awake()
    {
        Instance = this;
        CacheInitialScale();
    }
    private void CacheInitialScale()
    {
        _resultTextInitialScale=resultText.transform.localScale;
        _errorTextInitialScale=errorText.transform.localScale; 
        _consecutiveTextInitialScale=consecutiveText.transform.localScale;

        _resultScale= new Vector3(resultText.transform.localScale.x*MULTIPLIER_VALUE,
            resultText.transform.localScale.y * MULTIPLIER_VALUE,
            resultText.transform.localScale.z * MULTIPLIER_VALUE);

        _errorScale = new Vector3(errorText.transform.localScale.x * MULTIPLIER_VALUE,
            errorText.transform.localScale.y * MULTIPLIER_VALUE,
            errorText.transform.localScale.z * MULTIPLIER_VALUE);

        _consecutiveScale = new Vector3(consecutiveText.transform.localScale.x * MULTIPLIER_VALUE,
            consecutiveText.transform.localScale.y * MULTIPLIER_VALUE,
            consecutiveText.transform.localScale.z * MULTIPLIER_VALUE);
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
        resultText.transform.DOPunchScale(_resultScale, PUNCH_TIME).
            OnComplete(()=> resultText.transform.localScale=_resultTextInitialScale);
    }
    public void UpdateConsecutive(int consecutive)
    {
        consecutiveText.text = consecutive.ToString();
        consecutiveText.transform.DOPunchScale(_consecutiveScale, PUNCH_TIME).
             OnComplete(() => consecutiveText.transform.localScale = _consecutiveTextInitialScale); ;
    }
    public void UpdateError(int error)
    {
        errorText.text = error.ToString();
        errorText.transform.DOPunchScale(_errorScale, PUNCH_TIME).
                    OnComplete(() => errorText.transform.localScale = _errorTextInitialScale);
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
