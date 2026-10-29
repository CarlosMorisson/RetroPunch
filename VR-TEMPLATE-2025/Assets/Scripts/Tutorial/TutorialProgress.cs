using System.Collections.Generic;
using UnityEngine;

// Guarda (PlayerPrefs) se o tutorial de cada game type ja foi concluido.
// Enquanto o tutorial do game type atual nao foi concluido (false), o StageLoadController fica com IsTutorial = true;
// depois de concluido (true), IsTutorial fica false.
public class TutorialProgress : MonoBehaviour
{
    public static TutorialProgress Instance { get; private set; }

    // Mesmos nomes dos botoes do menu (MainButton.name) usados pelo StageLoadController.
    public const string PUNCHER_NAME = "puncher";
    public const string PUSHER_NAME = "pusher";
    public const string SHOOTER_NAME = "shooter";

    private const string PREFS_PREFIX = "TutorialCompleted_";

    private readonly Dictionary<string, bool> completed = new()
    {
        { PUNCHER_NAME, false },
        { PUSHER_NAME, false },
        { SHOOTER_NAME, false },
    };

    private void Awake()
    {
        Instance = this;
        Load();
    }

    private void Start()
    {
        UpdateStageTutorial();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    #region Concluir

    public void CompletePuncher() => Complete(PUNCHER_NAME);
    public void CompletePusher() => Complete(PUSHER_NAME);
    public void CompleteShooter() => Complete(SHOOTER_NAME);

    // Conclui o tutorial do game type selecionado no menu (ultimo MainButton escolhido).
    public void Complete()
    {
        MainButton selected = MenuMainButton.SelectedButton;
        if (selected == null)
        {
            Debug.LogWarning("[TutorialProgress] Nenhum MainButton selecionado no menu.");
            return;
        }

        Complete(selected.name.ToLowerInvariant());
    }

    private void Complete(string gameTypeName)
    {
        if (!completed.ContainsKey(gameTypeName))
        {
            Debug.LogWarning($"[TutorialProgress] Game type '{gameTypeName}' desconhecido.");
            return;
        }

        completed[gameTypeName] = true;
        PlayerPrefs.SetInt(PREFS_PREFIX + gameTypeName, 1);
        PlayerPrefs.Save();

        UpdateStageTutorial();
    }

    #endregion

    public bool IsCompleted(string gameTypeName)
    {
        return completed.TryGetValue(gameTypeName, out bool done) && done;
    }

    public bool IsCompleted(GameType gameType) => IsCompleted(GetName(gameType));

    private void Load()
    {
        foreach (string key in new List<string>(completed.Keys))
            completed[key] = PlayerPrefs.GetInt(PREFS_PREFIX + key, 0) == 1;
    }

    /// <summary>Marca o IsTutorial do StageLoadController de acordo com o game type atual.</summary>
    public void UpdateStageTutorial()
    {
        if (StageLoadController.Instance == null) return;

        StageLoadController.Instance.IsTutorial = !IsCompleted(StageLoadController.Instance.gameType);
    }

    private static string GetName(GameType gameType)
    {
        switch (gameType)
        {
            case GameType.Push: return PUSHER_NAME;
            case GameType.Shoot: return SHOOTER_NAME;
            case GameType.Punch:
            default: return PUNCHER_NAME;
        }
    }
}
