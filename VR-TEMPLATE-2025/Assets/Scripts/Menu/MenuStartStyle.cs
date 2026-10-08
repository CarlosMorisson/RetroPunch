using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class MenuStartStyle : MonoBehaviour
{
    public SO_LoadGame MenuStyle;

    public List<MeshRenderer> MenuRendererList;

    [Header("Base Color Reativo a Cor")]
    [Tooltip("Cria um material instanciado pra cada renderer e coloca o Primary Base Color do MenuStyle como base color.")]
    public List<MeshRenderer> PrimaryEmissiveRendererList;
    [Tooltip("Cria um material instanciado pra cada renderer e coloca o Secondary Base Color do MenuStyle como base color.")]
    public List<MeshRenderer> SecondaryEmissiveRendererList;

    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");

    void Start()
    {
        int randomNumber = Random.Range(0, StageLoadController.Instance.RandomColors.Count);
        MenuStyle = StageLoadController.Instance.RandomColors[randomNumber];
        foreach(MeshRenderer menuRenderer in MenuRendererList)
        {
            menuRenderer.material = MenuStyle.BuildSettings.BaseSceneSettings.GroundMaterial;
        }
        RenderSettings.skybox = MenuStyle.BuildSettings.BaseSceneSettings.SkyboxMaterial ;

        ApplyBaseColor(PrimaryEmissiveRendererList, MenuStyle.BuildSettings.PrimaryBaseColor);
        ApplyBaseColor(SecondaryEmissiveRendererList, MenuStyle.BuildSettings.SecondaryBaseColor);
    }

    // Instancia o material de cada renderer (pra nao alterar o asset compartilhado/os outros renderers
    // que usam o mesmo material) e troca o base color nessa copia.
    private void ApplyBaseColor(List<MeshRenderer> renderers, Color baseColor)
    {
        foreach (MeshRenderer meshRenderer in renderers)
        {
            if (meshRenderer == null)
                continue;

            Material instancedMaterial = new Material(meshRenderer.sharedMaterial);

            if (instancedMaterial.HasProperty(BaseColorProperty))
                instancedMaterial.SetColor(BaseColorProperty, baseColor);
            else if (instancedMaterial.HasProperty("_Color"))
                instancedMaterial.SetColor("_Color", baseColor);

            meshRenderer.material = instancedMaterial;
        }
    }
}
