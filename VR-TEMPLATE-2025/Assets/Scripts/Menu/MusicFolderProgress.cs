using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MusicFolderProgress : MonoBehaviour
{
    // Mesma chave usada antes pelo StageLoadController, para manter o progresso ja salvo
    private const string CompletedMusicKey = "CompletedMusic_";

    // Disparado sempre que uma musica e finalizada com sucesso (nome da musica)
    public static event Action<string> OnMusicCompleted;

    [SerializeField] private MenuMusicFolderContrloller folderController;

    [Tooltip("Mesma ordem dos folders do MenuMusicFolderContrloller (filhos do foldersRoot)")]
    [SerializeField] private List<FolderProgress> folders = new List<FolderProgress>();

    public IReadOnlyList<FolderProgress> Folders => folders;

    // Chamar quando o game termina com sucesso: salva a musica como concluida
    public static void CompleteMusic(string musicName)
    {
        if (string.IsNullOrEmpty(musicName)) return;

        PlayerPrefs.SetInt(CompletedMusicKey + musicName, 1);
        PlayerPrefs.Save();
        Debug.Log($"[MusicFolderProgress] Musica concluida: {musicName}");

        OnMusicCompleted?.Invoke(musicName);
    }

    public static bool IsMusicCompleted(string musicName)
    {
        return !string.IsNullOrEmpty(musicName) && PlayerPrefs.GetInt(CompletedMusicKey + musicName, 0) == 1;
    }

    void OnEnable()
    {
        OnMusicCompleted += HandleMusicCompleted;
    }

    void OnDisable()
    {
        OnMusicCompleted -= HandleMusicCompleted;
    }

    // Start roda depois do Awake do controller, que ja carregou os folders liberados
    void Start()
    {
        RefreshProgress();
    }

    private void HandleMusicCompleted(string musicName)
    {
        RefreshProgress();
    }

    // Retorna o indice do folder que contem a musica, ou -1 se nao encontrar
    public int GetFolderIndex(string musicName)
    {
        for (int i = 0; i < folders.Count; i++)
        {
            if (folders[i].Contains(musicName))
                return i;
        }
        return -1;
    }

    // Atualiza as cores e libera o proximo folder quando todas as musicas do anterior foram feitas
    public void RefreshProgress()
    {
        int unlockedCount = 1;

        for (int i = 0; i < folders.Count; i++)
        {
            folders[i].RefreshColors();

            if (unlockedCount == i + 1 && folders[i].IsCompleted)
                unlockedCount = i + 2;
        }

        if (folderController != null)
            folderController.SetOpenFolder(unlockedCount);
    }

    [ContextMenu("Reset Progress")]
    private void ResetProgress()
    {
        foreach (FolderProgress folder in folders)
        {
            foreach (MusicProgress music in folder.musics)
                PlayerPrefs.DeleteKey(CompletedMusicKey + music.musicName);
        }
        PlayerPrefs.Save();
    }
}

[Serializable]
public class FolderProgress
{
    public string folderName;
    public List<MusicProgress> musics = new List<MusicProgress>();

    public bool IsCompleted => musics.Count > 0 && musics.TrueForAll(m => m.IsCompleted);

    public bool Contains(string musicName) => musics.Exists(m => m.musicName == musicName);

    public void RefreshColors()
    {
        foreach (MusicProgress music in musics)
            music.RefreshColor();
    }
}

[Serializable]
public class MusicProgress
{
    [Tooltip("Nome do AudioClip da musica")]
    public string musicName;
    public Color doneColor = Color.green;
    public Color notDoneColor = Color.gray;
    [Tooltip("Imagem/Texto que recebe a cor de feito/nao feito (opcional)")]
    public Graphic indicator;

    public bool IsCompleted => MusicFolderProgress.IsMusicCompleted(musicName);

    public void RefreshColor()
    {
        if (indicator != null)
            indicator.color = IsCompleted ? doneColor : notDoneColor;
    }
}
