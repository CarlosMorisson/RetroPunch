using TMPro; // Obrigatório para o TextMeshPro
using DG.Tweening; // Obrigatório para o DOColor
using UnityEngine;
using UnityEngine.UI;

public class MenuColorText : MonoBehaviour
{
    [Header("Transition Settings")]
    [SerializeField] private float duration = 0.25f;

    private Image cachedImage;
    private TextMeshProUGUI cachedText;

    private void Awake()
    {
        TryGetComponent(out cachedImage);
        TryGetComponent(out cachedText);
    }

    private void OnEnable()
    {
        MenuMainButton.OnMainButtonChanged += HandleMainButtonChanged;
    }

    private void OnDisable()
    {
        MenuMainButton.OnMainButtonChanged -= HandleMainButtonChanged;
    }

    private void HandleMainButtonChanged(MainButton newActiveButton)
    {
        if (newActiveButton == null) return;

        Color targetColor = newActiveButton.ButtonColor;

        if (cachedImage != null)
        {
            cachedImage.DOColor(targetColor, duration).SetUpdate(true);
        }

        if (cachedText != null)
        {
            cachedText.DOColor(targetColor, duration).SetUpdate(true);
        }
    }
}