using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class MenuTween : MonoBehaviour
{
    [Header("LeftCanva")]
    public GameObject LeftCanva;
    public GameObject MusicSelection;
    public GameObject DifficultySelection;
    public GameObject TutorialSelection;
    public GameObject GameSelection;

    [Header("Right Canva")]
    public GameObject RightCanva;
    public GameObject MasterSong;
    public GameObject MusicSong;
    public GameObject SfxSong;

    [Header("Tween Config")]
    [SerializeField] private float animDuration = 0.4f;
    [SerializeField] private float delayBetweenItems = 0.08f;
    [SerializeField] private Vector3 punchAmount = new Vector3(0.15f, 0.15f, 0.15f);

    private Dictionary<GameObject, Vector3> initialScales = new Dictionary<GameObject, Vector3>();
    private List<GameObject> subElements = new List<GameObject>();

    private Sequence menuSequence;

    void Start()
    {
        if (LeftCanva != null)
        {
            initialScales[LeftCanva] = LeftCanva.transform.localScale;
            LeftCanva.transform.localScale = Vector3.zero; 
        }

        subElements.Add(MusicSelection);
        subElements.Add(DifficultySelection);
        subElements.Add(TutorialSelection);
        subElements.Add(GameSelection);

        foreach (GameObject obj in subElements)
        {
            if (obj != null)
            {
                initialScales[obj] = obj.transform.localScale;
                obj.transform.localScale = Vector3.zero;
            }
        }

        MenuMainButton.OnMainButtonChanged += AnimateLeftCanva;
    }

    private void OnDestroy()
    {
        MenuMainButton.OnMainButtonChanged -= AnimateLeftCanva;
        menuSequence?.Kill();
    }

    private void AnimateLeftCanva(MainButton currentMainButton)
    {
        menuSequence?.Kill();
        menuSequence = DOTween.Sequence().SetUpdate(true);

        float currentDelay = 0f;

        if (LeftCanva != null)
        {
            Vector3 originalLeftScale = initialScales[LeftCanva];

            if (LeftCanva.transform.localScale.magnitude < 0.01f)
            {
                menuSequence.Append(LeftCanva.transform.DOScale(originalLeftScale, animDuration).SetEase(Ease.OutCubic));
                currentDelay = animDuration * 0.5f;
            }
            else
            {
                LeftCanva.transform.localScale = originalLeftScale; 
                menuSequence.Append(LeftCanva.transform.DOPunchScale(punchAmount, animDuration, 4, 0.5f));
                currentDelay = animDuration * 2; 
            }
        }
        foreach (GameObject obj in subElements)
        {
            if (obj != null) obj.transform.localScale = Vector3.zero;
        }
        for (int i = 0; i < subElements.Count; i++)
        {
            GameObject obj = subElements[i];
            if (obj == null) continue;

            Vector3 originalScale = initialScales[obj];
            float startTime = currentDelay + (i * delayBetweenItems);
            menuSequence.Insert(startTime, obj.transform.DOScale(originalScale, animDuration).SetEase(Ease.OutBack));
        }
    }
}