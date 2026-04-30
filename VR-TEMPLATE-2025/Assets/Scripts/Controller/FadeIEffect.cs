using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class FadeIEffect : MonoBehaviour
{
    public GameObject RetroCanvas;
    [Tooltip("Arraste o material original aqui")]
    public Material SourceMaterial;

    [Header("Config")]
    public float punchScale = 0.3f;
    public float punchDuration = 0.2f;
    public float scaleDuration = 0.3f;
    public float fadeDuration = 0.5f;

    private Material _instancedMaterial;

    private const string GAME_SCENE= "BasicScene";
    private const string MENU_SCENE = "BasicScene";
    private void Awake()
    {
        if (SourceMaterial != null)
        {
            _instancedMaterial = new Material(SourceMaterial);
            GetComponent<Renderer>().material = _instancedMaterial;
        }
    }

    private void Start()
    {
        RetroCanvas.transform.localScale = Vector3.one;
        FadeOut();
    }

    [ContextMenu("FadeOut")]
    public void FadeOut()
    {
        Sequence seq = DOTween.Sequence();

        RetroCanvas.transform.localScale = Vector3.one;

        seq.Append(
            RetroCanvas.transform
                .DOPunchScale(Vector3.one * punchScale, punchDuration, 10, 1)
        );

        seq.Append(
            RetroCanvas.transform
                .DOScale(Vector3.zero, scaleDuration)
                .SetEase(Ease.InBack)
        );

        seq.OnComplete(() =>
        {
            _instancedMaterial.DOFade(0f, fadeDuration);
        });
    }

    [ContextMenu("FadeIn")]
    public void FadeIn()
    {
        Sequence seq = DOTween.Sequence();

        seq.Append(_instancedMaterial.DOFade(1f, fadeDuration));

        RetroCanvas.transform.localScale = Vector3.zero;

        seq.Append(
            RetroCanvas.transform
                .DOScale(Vector3.one, scaleDuration)
                .SetEase(Ease.OutBack)
        );
        seq.OnComplete(() =>
        {
            RestartScene();
        });
    }

    private void SetAlpha(float value)
    {
        if (_instancedMaterial == null) return;
        Color c = _instancedMaterial.color;
        c.a = value;
        _instancedMaterial.color = c;
    }
    public void RestartScene() => SceneManager.LoadScene(GAME_SCENE);

    private void OnDestroy()
    {
        if (_instancedMaterial != null)
        {
            Destroy(_instancedMaterial);
        }
    }
}