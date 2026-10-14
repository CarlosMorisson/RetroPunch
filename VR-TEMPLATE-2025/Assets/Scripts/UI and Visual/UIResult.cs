using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;
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
    [Header("Consecutive Errors (quanto falta pra perder)")]
    [SerializeField]
    private Image consecutiveErrorFillImage;
    [SerializeField]
    private float consecutiveErrorFillDuration = 0.2f;
    [Header("Music")]
    [SerializeField]
    private TextMeshProUGUI musicNameText;
    [SerializeField]
    private float musicLength;
    [SerializeField]
    private Slider musicSlider;
    [Header("FinalResult UI")]
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
    [SerializeField]
    private Image blockImagePorcent;
    [SerializeField]
    private TextMeshProUGUI musicFinalNameText;
    [SerializeField]
    private TextMeshProUGUI finalErrorText;
    [Header("Final Result To Animate")]
    [SerializeField]
    private GameObject FinalResultPanel;
    [SerializeField]
    private GameObject PointsPanel;
    [SerializeField]
    private GameObject ConsecutivePanel;
    [SerializeField]
    private GameObject BestConsecutivePanel;
    [SerializeField]
    private GameObject CaloriesPanel;
    [SerializeField]
    private GameObject PercentPanel;
    [SerializeField]    
    private GameObject MusicPanel;
    [SerializeField]
    private GameObject ErrorPanel;
    [SerializeField]
    private GameObject ResultPanel;
    [SerializeField]
    private GameObject StructurePanel;
    [SerializeField]
    private GameObject RestartButton;
    [SerializeField]
    private GameObject MenuButton;
    [Header("Settings de Anima��o")]
    [SerializeField] private float dropDuration = 0.5f;
    [SerializeField] private float scaleDuration = 0.3f;
    [SerializeField] private float startYOffset = 1000f;
    [Header("Pause Animation")]
    [SerializeField]
    private GameObject PausePanel;
    [SerializeField]
    private GameObject PauseText;
    [SerializeField]
    private GameObject ResumeButton;
    private Vector3 _resultTextInitialScale,
        _errorTextInitialScale,
        _consecutiveTextInitialScale,
        _retartButtonInitialScale,
        _menuButtonInitialScale;

    private Vector3 _resultScale,
        _errorScale,
        _consecutiveScale,
        _retartScale,
        _menuButtonScale;


    private const float MULTIPLIER_VALUE = 1.2f;
    private const float PUNCH_TIME=0.1f;
    private void Awake()
    {
        Instance = this;
        CacheInitialScale();
    }
    [ContextMenu("Testar Pause")]
    public void AnimatePausePanel()
    {
        PausePanel.SetActive(true);
        Sequence resultSequence = DOTween.Sequence();
        ResumeButton.transform.localScale = Vector3.zero;
        MenuButton.transform.localScale = Vector3.zero;
        RestartButton.transform.localScale = Vector3.zero;
        PauseText.transform.localScale = Vector3.zero;
        AppendScaleAnim(resultSequence, PauseText);
        AppendScaleAnim(resultSequence, ResumeButton);
        AppendScaleAnim(resultSequence, MenuButton);
        AppendScaleAnim(resultSequence, RestartButton);
    }
    [ContextMenu("Testar Pause")]
    public void AnimateDePausePanel()
    {
        Sequence resultSequence = DOTween.Sequence();
        resultSequence.Append(PauseText.transform.DOScale(Vector3.zero, scaleDuration)
            .SetEase(Ease.OutBack));
        resultSequence.Append(ResumeButton.transform.DOScale(Vector3.zero, scaleDuration)
    .SetEase(Ease.OutBack));
        resultSequence.Append(MenuButton.transform.DOScale(Vector3.zero, scaleDuration)
    .SetEase(Ease.OutBack));
        resultSequence.Append(RestartButton.transform.DOScale(Vector3.zero, scaleDuration)
    .SetEase(Ease.OutBack));
        resultSequence.OnComplete(() =>
        {
            PausePanel.SetActive(false);
        });
    }
    [ContextMenu("Testar Animacao")]
    public void AnimateResultPanel()
    {
        PreparePanelsForAnimation();

        PointController.Instance.CheckBestConsecutive();
        PointController.Instance.UpdateMainValues();

        finalResultText.text = PointController.Instance.Accept.ToString();
        finalConsecutiveText.text = PointController.Instance.GetBestConsecutiveInScene().ToString();
        highConsecutiveText.text = PointController.Instance.GetBestConsecutive().ToString();
        finalErrorText.text = PointController.Instance.Errors.ToString();

        FinalResultPanel.SetActive(true);
        ResultPanel.SetActive(false);
        StructurePanel.SetActive(false);

        UpdateCalorie(VRCalorieEstimator.Instance.GetTotalCalories());
        UpdatePercentAccuracy();

        Sequence resultSequence = DOTween.Sequence();

        resultSequence.Append(FinalResultPanel.transform.DOLocalMoveY(0, dropDuration)
            .From(new Vector3(0, startYOffset, 0))
            .SetEase(Ease.OutBack));

        AppendScaleAnim(resultSequence, PointsPanel);
        AppendScaleAnim(resultSequence, CaloriesPanel);
        AppendScaleAnim(resultSequence, ErrorPanel);
        AppendScaleAnim(resultSequence, ConsecutivePanel);
        AppendScaleAnim(resultSequence, BestConsecutivePanel);
        AppendScaleAnim(resultSequence, PercentPanel);
        AppendScaleAnim(resultSequence, MusicPanel);
        AppendScaleAnim(resultSequence, MenuButton);
        AppendScaleAnim(resultSequence, RestartButton);
        if (finalConsecutiveText.text == highConsecutiveText.text)
        {
            finalConsecutiveText.GetComponent<TextMeshReactiveColor>().UpdateWithNormalColor();
            highConsecutiveText.GetComponent<TextMeshReactiveColor>().UpdateWithNormalColor();
        }

    }

    private void PreparePanelsForAnimation()
    {
        GameObject[] panels = {
            PointsPanel, CaloriesPanel, ResultPanel,
            ConsecutivePanel, BestConsecutivePanel, PercentPanel, MusicPanel
        };

        foreach (var p in panels)
        {
            if (p != null)
            {
                p.transform.localScale = Vector3.zero;
                p.SetActive(true);
            }
        }
    }

    private void AppendScaleAnim(Sequence seq, GameObject panel)
    {
        if (panel == null) return;
        if(panel== RestartButton)
            RestartButton.SetActive(true);
        if(panel==MenuButton) 
            MenuButton.SetActive(true);
        if(panel==ResumeButton)
            ResumeButton.SetActive(true);
        seq.Append(panel.transform.DOScale(Vector3.one, scaleDuration)
            .SetEase(Ease.OutBack));
    }
    private void CacheInitialScale()
    {
        _resultTextInitialScale=resultText.transform.localScale;
        _errorTextInitialScale=errorText.transform.localScale; 
        _consecutiveTextInitialScale=consecutiveText.transform.localScale;
        _retartButtonInitialScale= RestartButton.transform.localScale;
        _menuButtonInitialScale= MenuButton.transform.localScale;


        _resultScale = new Vector3(resultText.transform.localScale.x*MULTIPLIER_VALUE,
            resultText.transform.localScale.y * MULTIPLIER_VALUE,
            resultText.transform.localScale.z * MULTIPLIER_VALUE);

        _errorScale = new Vector3(errorText.transform.localScale.x * MULTIPLIER_VALUE,
            errorText.transform.localScale.y * MULTIPLIER_VALUE,
            errorText.transform.localScale.z * MULTIPLIER_VALUE);

        _resultTextInitialScale = new Vector3(consecutiveText.transform.localScale.x * MULTIPLIER_VALUE,
            consecutiveText.transform.localScale.y * MULTIPLIER_VALUE,
            consecutiveText.transform.localScale.z * MULTIPLIER_VALUE);

        _menuButtonInitialScale = new Vector3(MenuButton.transform.localScale.x * MULTIPLIER_VALUE,
            MenuButton.transform.localScale.y * MULTIPLIER_VALUE,
            MenuButton.transform.localScale.z * MULTIPLIER_VALUE);

        _retartScale = new Vector3(RestartButton.transform.localScale.x * MULTIPLIER_VALUE,
            RestartButton.transform.localScale.y * MULTIPLIER_VALUE,
            RestartButton.transform.localScale.z * MULTIPLIER_VALUE);

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
        finalResultText.text = result.ToString();
        UpdateConsecutiveErrorFill(); // acerto zera os erros consecutivos
    }
    public void UpdateConsecutive(int consecutive)
    {
        consecutiveText.text = consecutive.ToString();
        consecutiveText.transform.DOPunchScale(_consecutiveScale, PUNCH_TIME).
             OnComplete(() => consecutiveText.transform.localScale = _consecutiveTextInitialScale); ;
        UpdateConsecutiveInScene(consecutive);
    }
    public void UpdateConsecutiveInScene(int consecutive)
    {
        if (consecutive >= PointController.Instance.GetBestConsecutiveInScene())
        {
            finalConsecutiveText.text=consecutive.ToString();
            UpdateBestConsecutive(consecutive);
        }
    }
    public void UpdatePercentAccuracy()
    {
        blockDestructedPercentText.text=PointController.Instance.GetPercentageSucessString();
        blockImagePorcent.fillAmount=PointController.Instance.GetPercentageSucess()/100;
    }
    public void UpdateBestConsecutive(int consecutive)
    {
        if (consecutive >= PointController.Instance.GetBestConsecutive())
        {
            highConsecutiveText.text=consecutive.ToString();
        }
    }
    // O evento envia os erros consecutivos; os textos mostram sempre o total
    public void UpdateError(int consecutiveErrors)
    {
        int totalErrors = PointController.Instance.Errors;
        errorText.text = totalErrors.ToString();
        errorText.transform.DOPunchScale(_errorScale, PUNCH_TIME).
                    OnComplete(() => errorText.transform.localScale = _errorTextInitialScale);
        finalErrorText.text = totalErrors.ToString();
        UpdateConsecutiveErrorFill();
    }
    // Preenche a imagem conforme os erros consecutivos se aproximam da tolerancia
    public void UpdateConsecutiveErrorFill()
    {
        if (consecutiveErrorFillImage == null)
            return;

        int tolerance = StageLoadController.Instance.CurrentDifficulty.ErrorTolerance;
        float fill = tolerance > 0
            ? Mathf.Clamp01((float)PointController.Instance.GetConsecutiveErrors() / tolerance)
            : 0f;

        consecutiveErrorFillImage.DOKill();
        consecutiveErrorFillImage.DOFillAmount(fill, consecutiveErrorFillDuration);
        consecutiveErrorFillImage.DOColor(GetConsecutiveErrorColor(fill), consecutiveErrorFillDuration);
    }
    // Tolerancia dividida em 3 faixas: verde, amarelo, vermelho
    private Color GetConsecutiveErrorColor(float fill)
    {
        if (fill <= 1f / 3f)
            return Color.green;
        if (fill <= 2f / 3f)
            return Color.yellow;
        return Color.red;
    }
    public void UpdateMusicName(string name)
    {
        musicNameText.text = name;
        musicFinalNameText.text= name;
        musicNameText.transform.DOPunchScale(musicNameText.transform.localScale * MULTIPLIER_VALUE, PUNCH_TIME);
    }
    public void UpdateCalorie(float calorie)=>caloriesText.text = DecimalFormat.OneDecimal(calorie);
    private void Start()
    {
        PointController.Instance.OnAcceptInt += UpdateResult;
        PointController.Instance.OnConsecutiveInt += UpdateConsecutive;
        PointController.Instance.OnErrorInt += UpdateError;

        if (consecutiveErrorFillImage != null)
        {
            consecutiveErrorFillImage.fillAmount = 0f;
            consecutiveErrorFillImage.color = GetConsecutiveErrorColor(0f);
        }
    }
}
