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
        ApplySelectedColor();
    }

    private void OnDisable()
    {
        MenuMainButton.OnMainButtonChanged -= HandleMainButtonChanged;
    }

    /// <summary>
    /// Aplica a cor do botão selecionado instantaneamente (usado ao reativar o objeto)
    /// </summary>
    private void ApplySelectedColor()
    {
        MainButton selected = MenuMainButton.SelectedButton;
        if (selected == null) return;

        Color targetColor = selected.ButtonColor;

        if (cachedImage != null)
        {
            cachedImage.DOKill();
            cachedImage.color = targetColor;
        }

        if (cachedText != null)
        {
            cachedText.DOKill();
            cachedText.color = targetColor;
        }
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