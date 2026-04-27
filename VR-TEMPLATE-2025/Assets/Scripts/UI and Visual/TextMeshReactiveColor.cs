using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(TMP_Text))]
public class TextMeshReactiveColor : MonoBehaviour
{
    private TMP_Text _textElement;
    private const float WAIT_TIME = 0.2f;

    private void Awake()
    {
        _textElement = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (ColorController.Instance == null || _textElement == null)
        {
            StartCoroutine(WaitToGetColor());
            return;
        }

        UpdateTextGradient();
    }

    private IEnumerator WaitToGetColor()
    {
        yield return new WaitForSeconds(WAIT_TIME);
        UpdateTextGradient();
    }
    public void UpdateWithNormalColor()
    {
        if (ColorController.Instance == null || _textElement == null)
            return;

        Color primary = ColorController.Instance.CurrentPrimary;
        Color secondary = ColorController.Instance.CurrentSecondary;

        _textElement.enableVertexGradient = true;
        _textElement.colorGradient = new VertexGradient(
            primary,
            primary,
            secondary,
            secondary
        );
    }
    private void UpdateTextGradient()
    {
        if (ColorController.Instance == null || _textElement == null)
            return;

        Color primary = ColorController.Instance.CurrentPrimary;
        Color secondary = ColorController.Instance.CurrentSecondary;

        Color complementaryPrimary = GetComplementaryColor(primary);
        Color complementarySecondary = GetComplementaryColor(secondary);

        _textElement.enableVertexGradient = true;
        _textElement.colorGradient = new VertexGradient(
            complementaryPrimary,  
            complementaryPrimary,  
            complementarySecondary, 
            complementarySecondary  
        );
    }

    /// <summary>
    /// Rotaciona o Matiz da cor em 180 graus (0.5f no Unity)
    /// </summary>
    private Color GetComplementaryColor(Color source)
    {
        float h, s, v;
        Color.RGBToHSV(source, out h, out s, out v);

        h = (h + 0.5f) % 1f;

        return Color.HSVToRGB(h, s, v);
    }
}