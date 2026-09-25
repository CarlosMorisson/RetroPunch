using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuShowValues : MonoBehaviour
{
    private const string MAIN_TOTAL_SCORE = "MainTotalScore";
    private const string MAIN_TOTAL_CALORIES = "MainTotalCalories";
    private const string MAIN_TOTAL_ERRORS = "MainTotalErrors";
    private const string MAIN_BEST_CONSECUTIVE = "BestConsecutive";
    private const string MAIN_TOTAL_ACCURACY = "MainTotalAccuracy";

    [Header("Anality Text")]
    [SerializeField]
    private TextMeshProUGUI TotalScore,
        TotalCalories,
        TotalErrors,
        TotalConsecutive,
        TotalAccuracy;
    [Header("Accuracy Image")]
    [SerializeField]
    private Image AccuracyImage;
    void Start()
    {
        ScoreUpdate();
        CaloriesUpdate();
        ConsecutiveUpdate();
        ErrorUpdate();
        AccuracyUpdate();
    }
    public void ScoreUpdate()=>TotalScore.text=PlayerPrefs.GetInt(MAIN_TOTAL_SCORE).ToString();
    public void ErrorUpdate()=>TotalErrors.text=PlayerPrefs.GetInt(MAIN_TOTAL_ERRORS).ToString();
    public void ConsecutiveUpdate()=>TotalConsecutive.text=PlayerPrefs.GetInt(MAIN_BEST_CONSECUTIVE).ToString();
    public void CaloriesUpdate()=>TotalCalories.text=PlayerPrefs.GetFloat(MAIN_TOTAL_CALORIES).ToString();

    public void AccuracyUpdate()
    {
        TotalAccuracy.text=PlayerPrefs.GetFloat(MAIN_TOTAL_ACCURACY).ToString();
        AccuracyImage.fillAmount = PlayerPrefs.GetFloat(MAIN_TOTAL_ACCURACY) / 100;
    }
}
