using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;

public class ChangeCharacterScale : MonoBehaviour
{
    [SerializeField]
    GameObject _characterModel;
    [SerializeField]
    GameObject _playerReference;
    [SerializeField]
    Transform _baseModel, _topModel;
    //
    private XROrigin _xrOrigin;
    private CharacterController _characterController;
    bool _checked=false;
    [SerializeField]
    private float ExampleValue;
    void Start()
    {
        _xrOrigin = _playerReference.GetComponent<XROrigin>();
        _characterController = _playerReference.GetComponent<CharacterController>();
        _characterController.height = ExampleValue;
        StartCoroutine(AdjustScaleGradually());
    }

    IEnumerator AdjustScaleGradually()
    {
        if (_characterModel == null || _characterController == null || _baseModel == null || _topModel == null)
        {
            Debug.LogError("Certifique-se de que todos os campos estão atribuídos.");
            yield break;
        }

        float modelHeight = CalculateHeight();
        Debug.Log($"Altura inicial do modelo: {modelHeight}");
       // if (!_checked)
       // {
            while (modelHeight > ExampleValue)
            {
                float scaleFactor = ExampleValue / modelHeight;

                float incrementalScale = Mathf.Lerp(1f, scaleFactor, 0.01f);
                _characterModel.transform.localScale *= incrementalScale;

                modelHeight = CalculateHeight();

                UpdateCharacterController(incrementalScale);

                yield return null;
            }
            if (modelHeight <= ExampleValue)
            {
                //_checked = true;
            }
       // }

        Debug.Log("Modelo ajustado para a altura desejada.");
    }

    float CalculateHeight()
    {
        float height = Mathf.Abs(_topModel.position.y - _baseModel.position.y);
        return height;
    }

    void UpdateCharacterController(float scaleFactor)
    {
        _characterController.height *= scaleFactor;
        _characterController.radius *= scaleFactor;

        _characterController.center = new Vector3(
            _characterController.center.x,
            _characterController.center.y * scaleFactor,
            _characterController.center.z
        );

        Debug.Log("CharacterController ajustado para a nova escala.");
    }
}
