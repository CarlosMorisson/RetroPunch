using UnityEngine;
using DG.Tweening; 

public class PushCubeFeedback : MonoBehaviour
{
    [Header("Mesh Renderers & Materials")]
    [SerializeField] private MeshRenderer meshRenderer1;
    [SerializeField] private MeshRenderer meshRenderer2;
    [SerializeField] private float blinkDuration = 0.2f;
    [SerializeField] private Color blinkEmissionColor = Color.white;

    [Header("Line Renderer Settings")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float lineLength = 5f;
    [SerializeField] private Color lineColor = Color.red;
    [SerializeField] private float lineWidth = 0.05f;

    private Material mat1;
    private Material mat2;

    private Color originalEmission1;
    private Color originalEmission2;

    private bool originalEmissionKeyword1;
    private bool originalEmissionKeyword2;

    private void Awake()    {
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;

            if (lineRenderer.material == null)
            {
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            }

            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;
            lineRenderer.enabled = false; 
        }

        if (meshRenderer1 != null)
        {
            mat1 = meshRenderer1.material;
            originalEmissionKeyword1 = mat1.IsKeywordEnabled("_EMISSION");
            if (mat1.HasProperty("_EmissionColor"))
            {
                originalEmission1 = mat1.GetColor("_EmissionColor");
            }
        }

        if (meshRenderer2 != null)
        {
            mat2 = meshRenderer2.material;
            originalEmissionKeyword2 = mat2.IsKeywordEnabled("_EMISSION");
            if (mat2.HasProperty("_EmissionColor"))
            {
                originalEmission2 = mat2.GetColor("_EmissionColor");
            }
        }
    }

    private void OnEnable()
    {
        ResetMaterial(mat1, originalEmission1, originalEmissionKeyword1);
        ResetMaterial(mat2, originalEmission2, originalEmissionKeyword2);

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    /// <summary>
    /// Cancela o blink em andamento e restaura a cor de emissão original do material.
    /// </summary>
    private void ResetMaterial(Material mat, Color originalColor, bool emissionKeywordEnabled)
    {
        if (mat == null) return;

        mat.DOKill();

        if (mat.HasProperty("_EmissionColor"))
        {
            mat.SetColor("_EmissionColor", originalColor);
        }

        if (emissionKeywordEnabled)
            mat.EnableKeyword("_EMISSION");
        else
            mat.DisableKeyword("_EMISSION");
    }

    /// <summary>
    /// Fun��o que recebe a posi��o do objeto e a dire��o do movimento para gerar o feedback visual.
    /// </summary>
    /// <param name="targetPosition">Posi��o central do cubo.</param>
    /// <param name="moveDirection">Vetor de dire��o para onde o cubo est� indo.</param>
    public void HandlePushFeedback(Vector3 targetPosition, Vector3 moveDirection)
    {
        BlinkMaterial(mat1, originalEmission1);
        BlinkMaterial(mat2, originalEmission2);

        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;

            Vector3 direction = moveDirection.normalized;

            lineRenderer.SetPosition(0, targetPosition);

            lineRenderer.SetPosition(1, targetPosition + (direction * lineLength));

            DOVirtual.DelayedCall(blinkDuration, () =>
            {
                if (lineRenderer != null)
                {
                    lineRenderer.enabled = false;
                }
            }).SetUpdate(true); 
        }
    }

    private void BlinkMaterial(Material mat, Color originalColor)
    {
        if (mat == null) return;

        mat.EnableKeyword("_EMISSION");

        mat.DOColor(blinkEmissionColor, "_EmissionColor", blinkDuration / 2f)
            .SetLoops(2, LoopType.Yoyo)
            .SetEase(Ease.Flash);
    }

    private void OnDestroy()
    {
        if (mat1 != null) Destroy(mat1);
        if (mat2 != null) Destroy(mat2);
    }
}