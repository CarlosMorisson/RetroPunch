using System;
using System.Collections.Generic;
using System.Linq; // Obrigatório para usar o Linq
using UnityEngine;
using UnityEngine.UI;

public class MenuMainButton : MonoBehaviour
{
    [Header("Button List")]
    public List<MainButton> mainButtons = new List<MainButton>();

    public static event Action<MainButton> OnMainButtonChanged;

    private MainButton currentButton;

    public MainButton CurrentButton
    {
        get => currentButton;
        set
        {
            if (value != currentButton)
            {
                currentButton = value;

                if (currentButton != null)
                {
                    OnMainButtonChanged?.Invoke(currentButton);
                }
            }
        }
    }
    [ContextMenu("Pusher")]
    public void SetPusher()
    {
        RedirectToButtonByName("Pusher");
    }
    [ContextMenu("Puncher")]
    public void SetPuncher()
    {
        RedirectToButtonByName("Puncher");
    }
    [ContextMenu("Shooter")]
    public void SetShooter()
    {
        RedirectToButtonByName("Shooter");
    }

    /// <summary>
    /// Busca o botão na lista usando System.Linq e joga na propriedade CurrentButton
    /// </summary>
    private void RedirectToButtonByName(string targetName)
    {
        MainButton foundButton = mainButtons.FirstOrDefault(b => b.name.Equals(targetName, StringComparison.OrdinalIgnoreCase));

        if (foundButton != null)
        {
            CurrentButton = foundButton;
            CurrentButton.ButtonToggle.isOn = true;
        }
        else
        {
            Debug.LogWarning($"[MenuMainButton] Nenhum botão com o nome '{targetName}' foi encontrado na lista!");
        }
    }

}

[System.Serializable]
public class MainButton
{
    public string name;
    public Color ButtonColor;
    public Toggle ButtonToggle;
}