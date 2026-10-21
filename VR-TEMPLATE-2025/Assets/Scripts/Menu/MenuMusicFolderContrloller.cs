using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class MenuMusicFolderContrloller : MonoBehaviour
{
    private const string OpenFolderKey = "OpenFolder";

    [SerializeField] private Transform foldersRoot;
    [SerializeField] private List<GameObject> folders = new List<GameObject>();

    [SerializeField] private int currentIndex = 0;

    // Quantidade de pastas liberadas (1 = apenas a primeira)
    [SerializeField] private int openFolder = 1;

    [Header("Indicadores")]
    [SerializeField] private Image indicatorPrefab;
    [SerializeField] private Transform indicatorsParent;
    [SerializeField] private Transform indicatorsStart;
    [SerializeField] private Transform indicatorsEnd;
    [SerializeField] private MenuMainButton menuMainButton;
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color unselectedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    [SerializeField] private Color lockedColor = new Color(0.35f, 0.35f, 0.35f, 1f);
    [SerializeField] private float selectedScale = 1.2f;
    [SerializeField] private float unselectedScale = 0.8f;
    [SerializeField] private float tweenDuration = 0.25f;

    private readonly List<Image> indicators = new List<Image>();

    public int CurrentIndex => currentIndex;
    public int OpenFolder => openFolder;

    void Awake()
    {
        CollectFolders();
        openFolder = Mathf.Clamp(PlayerPrefs.GetInt(OpenFolderKey, 1), 1, Mathf.Max(1, folders.Count));
        currentIndex = 0;
        CreateIndicators();
        RefreshFolders(false);
        CheckLastFolderCompleted();
    }

    // Verifica as musicas do ultimo folder liberado; se todas estiverem concluidas, libera o proximo
    private void CheckLastFolderCompleted()
    {
        if (openFolder >= folders.Count) return;

        GameObject lastFolder = folders[openFolder - 1];
        if (lastFolder == null) return;

        MenuSetMusic[] musics = lastFolder.GetComponentsInChildren<MenuSetMusic>(true);
        if (musics.Length == 0) return;

        foreach (MenuSetMusic music in musics)
        {
            if (!StageLoadController.IsMusicCompleted(music.MusicToSet))
                return;
        }

        UnlockNextFolder();
    }

    void OnEnable()
    {
        MenuMainButton.OnMainButtonChanged += HandleMainButtonChanged;

        if (menuMainButton != null && menuMainButton.CurrentButton != null)
        {
            selectedColor = menuMainButton.CurrentButton.ButtonColor;
            RefreshIndicators(false);
        }
    }

    void OnDisable()
    {
        MenuMainButton.OnMainButtonChanged -= HandleMainButtonChanged;
    }

    private void HandleMainButtonChanged(MainButton newActiveButton)
    {
        if (newActiveButton == null) return;

        selectedColor = newActiveButton.ButtonColor;
        RefreshIndicators(true);
    }

    private void CollectFolders()
    {
        folders.Clear();
        if (foldersRoot == null) return;

        foreach (Transform child in foldersRoot)
            folders.Add(child.gameObject);
    }

    private int AccessibleCount => Mathf.Min(openFolder, folders.Count);

    public void NextFolder()
    {
        if (AccessibleCount == 0) return;

        currentIndex++;
        if (currentIndex >= AccessibleCount)
            currentIndex = 0;

        RefreshFolders();
    }

    public void PreviousFolder()
    {
        if (AccessibleCount == 0) return;

        currentIndex--;
        if (currentIndex < 0)
            currentIndex = AccessibleCount - 1;

        RefreshFolders();
    }

    public void UnlockNextFolder()
    {
        if (openFolder >= folders.Count) return;

        openFolder++;
        PlayerPrefs.SetInt(OpenFolderKey, openFolder);
        PlayerPrefs.Save();

        RefreshIndicators(true);
    }

    public void CreateIndicators()
    {
        foreach (Image indicator in indicators)
        {
            if (indicator != null)
            {
                indicator.transform.DOKill();
                indicator.DOKill();
                Destroy(indicator.gameObject);
            }
        }
        indicators.Clear();

        if (indicatorPrefab == null || indicatorsStart == null || indicatorsEnd == null) return;

        Transform parent = indicatorsParent != null ? indicatorsParent : indicatorsStart.parent;

        for (int i = 0; i < folders.Count; i++)
        {
            float t = folders.Count > 1 ? (float)i / (folders.Count - 1) : 0.5f;

            Image indicator = Instantiate(indicatorPrefab, parent);
            indicator.transform.position = Vector3.Lerp(indicatorsStart.position, indicatorsEnd.position, t);
            indicators.Add(indicator);
        }
    }

    private void RefreshFolders(bool animate = true)
    {
        for (int i = 0; i < folders.Count; i++)
        {
            if (folders[i] != null)
                folders[i].SetActive(i == currentIndex);
        }

        RefreshIndicators(animate);
    }

    private void RefreshIndicators(bool animate)
    {
        for (int i = 0; i < indicators.Count; i++)
        {
            Image indicator = indicators[i];
            if (indicator == null) continue;

            bool selected = i == currentIndex;
            bool locked = i >= AccessibleCount;
            Color color = selected ? selectedColor : locked ? lockedColor : unselectedColor;
            Vector3 scale = Vector3.one * (selected ? selectedScale : unselectedScale);

            indicator.transform.DOKill();
            indicator.DOKill();

            if (animate)
            {
                indicator.transform.DOScale(scale, tweenDuration).SetEase(Ease.OutBack);
                indicator.DOColor(color, tweenDuration);
            }
            else
            {
                indicator.transform.localScale = scale;
                indicator.color = color;
            }
        }
    }
}
