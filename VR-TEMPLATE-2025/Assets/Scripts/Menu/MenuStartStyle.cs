using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class MenuStartStyle : MonoBehaviour
{
    public SO_LoadGame MenuStyle;

    public List<MeshRenderer> MenuRendererList;
    void Start()
    {
        int randomNumber = Random.Range(0, StageLoadController.Instance.RandomColors.Count);
        MenuStyle = StageLoadController.Instance.RandomColors[randomNumber];
        foreach(MeshRenderer menuRenderer in MenuRendererList)
        {
            menuRenderer.material = MenuStyle.BuildSettings.BaseSceneSettings.GroundMaterial;
        }
        RenderSettings.skybox = MenuStyle.BuildSettings.BaseSceneSettings.SkyboxMaterial ;
    }

}
