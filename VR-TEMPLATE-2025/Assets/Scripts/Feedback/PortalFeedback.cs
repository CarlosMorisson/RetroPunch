using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class PortalFeedback : MonoBehaviour
{
    public Transform PortalTransform;
    public Transform IconTransform;
    public List<ParticleSystem> FeedbackParticle;

    [Tooltip("Se deixado em branco, o script pegar� o material do MeshRenderer automaticamente.")]
    public Material PortalMaterial;
    public PushType PushType;

    private Vector3 initialPortalScale;
    private Vector3 initialIconScale;
    private const float FAIL_SCALE = 0.8f;
    private const float SUCESS_SCALE = 1.5f;

    [Header("Animation Settings")]
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private float colorFadeBackDuration = 0.4f;

    [Header("Black Hole Pull")]
    [Tooltip("Centro da sucção. Se vazio, usa o PortalTransform (ou este transform).")]
    [SerializeField] private Transform pullCenter;
    [Tooltip("Normal da boca do portal em espaço local do centro (veja a linha azul do Gizmo)")]
    [SerializeField] private Vector3 localPortalNormal = Vector3.forward;
    [Tooltip("Tamanho (em metros) da caixa de sucção; só cubos tocados dentro dela são puxados")]
    [SerializeField] private Vector3 pullBoxSize = new Vector3(3f, 3f, 4f);
    [Tooltip("Deslocamento da caixa em relação ao centro, no espaço local (ex: Z positivo empurra a caixa para a frente do portal)")]
    [SerializeField] private Vector3 pullBoxOffset = new Vector3(0f, 0f, 1.5f);
    [Tooltip("Aceleração máxima (m/s²) em direção ao centro, atingida bem perto do portal")]
    [SerializeField] private float pullStrength = 40f;
    [Tooltip("Quanto maior, mais a força se concentra perto do portal (2 = quadrático)")]
    [SerializeField] private float pullFalloffExponent = 2f;

    [Header("Side Pull (só cubos do mesmo PushType)")]
    [Tooltip("Tamanho (em metros) da área de sucção lateral")]
    [SerializeField] private Vector3 sideBoxSize = new Vector3(6f, 3f, 3f);
    [Tooltip("Deslocamento da área lateral em relação ao centro, no espaço local")]
    [SerializeField] private Vector3 sideBoxOffset = new Vector3(0f, 0f, 1f);
    [Tooltip("Aceleração (m/s²) que puxa o cubo de volta para o eixo do portal")]
    [SerializeField] private float sidePullStrength = 80f;
    [Tooltip("Freia a velocidade lateral para o cubo não orbitar nem escapar pelo lado")]
    [SerializeField] private float sideDamping = 12f;

    private Color origOuterColor;
    private Color origOuterGlowColor;
    private Color origInnerFrameColor;
    private Color origInnerColorA;
    private Color origInnerColorB;

    // Defini��o das strings exatas das propriedades para o DOTween mapear sem erros
    private const string OuterColorProp = "_OuterColor";
    private const string OuterGlowColorProp = "_OuterGlowColor";
    private const string InnerFrameColorProp = "_InnerFrameColor";
    private const string InnerColorAProp = "_InnerColorA";
    private const string InnerColorBProp = "_InnerColorB";

    private void Start()
    {
        if (PortalTransform != null)
        {
            initialPortalScale = PortalTransform.localScale;
            MeshRenderer meshRenderer = PortalTransform.GetComponent<MeshRenderer>();

            if (meshRenderer != null)
            {
                if (PortalMaterial != null)
                {
                    PortalMaterial = new Material(PortalMaterial);
                    meshRenderer.material = PortalMaterial;
                }
                else
                {
                    PortalMaterial = meshRenderer.material;
                }

                CacheOriginalColors();
            }
        }

        if (IconTransform != null)
            initialIconScale = IconTransform.localScale;
    }

    #region Black Hole Pull

    private Transform PullCenter => pullCenter != null ? pullCenter : (PortalTransform != null ? PortalTransform : transform);

    // Usa só posição e rotação do centro (sem escala), para as caixas não pulsarem com os tweens do portal
    private static Vector3 BoxWorldCenter(Transform center, Vector3 boxOffset) => center.position + center.rotation * boxOffset;

    private static bool IsInsideBox(Transform center, Vector3 boxOffset, Vector3 boxSize, Vector3 worldPoint)
    {
        Vector3 local = Quaternion.Inverse(center.rotation) * (worldPoint - BoxWorldCenter(center, boxOffset));
        Vector3 half = boxSize * 0.5f;
        return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
    }

    // Distância do portal até o canto mais distante da caixa: é onde a força chega a zero
    private static float MaxReach(Vector3 boxOffset, Vector3 boxSize) => boxOffset.magnitude + boxSize.magnitude * 0.5f;

    private void FixedUpdate()
    {
        Transform center = PullCenter;
        Vector3 normal = center.TransformDirection(localPortalNormal).normalized;

        float pullReach = MaxReach(pullBoxOffset, pullBoxSize);
        float sideReach = MaxReach(sideBoxOffset, sideBoxSize);

        foreach (PushCube cube in PushCube.TouchedCubes)
        {
            if (cube == null || !cube.CanBePulled) continue;
            Pull(cube.Body, cube.pushDirection, center, normal, pullReach, sideReach);
        }

        foreach (TutorialPushBall ball in TutorialPushBall.TouchedBalls)
        {
            if (ball == null || !ball.CanBePulled) continue;
            Pull(ball.Body, ball.pushDirection, center, normal, pullReach, sideReach);
        }
    }

    private void Pull(Rigidbody body, PushType pushDirection, Transform center, Vector3 normal, float pullReach, float sideReach)
    {
        Vector3 centerPos = center.position;
        Vector3 position = body.worldCenterOfMass;
        Vector3 offset = position - centerPos;
        float distance = offset.magnitude;
        if (distance < 0.001f) return;

        Vector3 acceleration = Vector3.zero;

        // Sucção principal (qualquer cubo tocado): cresce conforme o cubo se aproxima
        if (pullReach > 0.001f && IsInsideBox(center, pullBoxOffset, pullBoxSize, position))
        {
            float proximity = Mathf.Clamp01(1f - distance / pullReach);
            acceleration += -offset / distance * (pullStrength * Mathf.Pow(proximity, pullFalloffExponent));
        }

        // Sucção lateral (só cubos do mesmo tipo): puxa de volta para o eixo do portal para não passar pelo lado
        if (pushDirection == PushType && sideReach > 0.001f && IsInsideBox(center, sideBoxOffset, sideBoxSize, position))
        {
            float proximity = Mathf.Clamp01(1f - distance / sideReach);

            // Parte do deslocamento que está "ao lado" da boca do portal (fora do eixo da normal)
            Vector3 lateralOffset = offset - normal * Vector3.Dot(offset, normal);
            float sideFactor = lateralOffset.magnitude / distance;

            if (lateralOffset.sqrMagnitude > 0.0001f)
                acceleration += -lateralOffset.normalized * (sidePullStrength * proximity * sideFactor);

            Vector3 lateralVelocity = body.linearVelocity - normal * Vector3.Dot(body.linearVelocity, normal);
            acceleration += -lateralVelocity * (sideDamping * proximity);
        }

        if (acceleration != Vector3.zero)
            body.AddForce(acceleration, ForceMode.Acceleration);
    }

    private static void DrawBoxGizmo(Transform center, Vector3 boxOffset, Vector3 boxSize, Color color)
    {
        Gizmos.matrix = Matrix4x4.TRS(BoxWorldCenter(center, boxOffset), center.rotation, Vector3.one);
        Gizmos.color = new Color(color.r, color.g, color.b, 0.15f);
        Gizmos.DrawCube(Vector3.zero, boxSize);
        Gizmos.color = new Color(color.r, color.g, color.b, 0.9f);
        Gizmos.DrawWireCube(Vector3.zero, boxSize);
        Gizmos.matrix = Matrix4x4.identity;
    }

    private void OnDrawGizmosSelected()
    {
        Transform center = PullCenter;

        DrawBoxGizmo(center, pullBoxOffset, pullBoxSize, new Color(0.6f, 0.2f, 1f));
        DrawBoxGizmo(center, sideBoxOffset, sideBoxSize, new Color(1f, 0.6f, 0.1f));

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(center.position, center.position + center.TransformDirection(localPortalNormal).normalized * 1.5f);
    }

    #endregion

    private void CacheOriginalColors()
    {
        if (PortalMaterial == null) return;

        origOuterColor = PortalMaterial.GetColor(OuterColorProp);
        origOuterGlowColor = PortalMaterial.GetColor(OuterGlowColorProp);
        origInnerFrameColor = PortalMaterial.GetColor(InnerFrameColorProp);
        origInnerColorA = PortalMaterial.GetColor(InnerColorAProp);
        origInnerColorB = PortalMaterial.GetColor(InnerColorBProp);
    }

    [ContextMenu("Handle Success")]
    public void HandleSuccess()
    {
        if (PortalTransform == null || IconTransform == null) return;

        PortalTransform.DOKill(false);
        IconTransform.DOKill(false);
        if (PortalMaterial != null) PortalMaterial.DOKill(false);

        ResetScales();

        // Aplica o flash e inicia a transi��o controlada via Sequence
        foreach(ParticleSystem particle in FeedbackParticle)
            particle.Play();
        ApplyInstantColorFlash(Color.white);
        AnimateColorBack();

        float halfDuration = duration * 0.5f;

        PortalTransform.DOScale(initialPortalScale * SUCESS_SCALE, halfDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                PortalTransform.DOScale(initialPortalScale, halfDuration).SetEase(Ease.OutBack);

                IconTransform.DOScale(initialIconScale * SUCESS_SCALE, halfDuration)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        IconTransform.DOScale(initialIconScale, halfDuration).SetEase(Ease.OutBack)
                            .OnComplete(() => ResetScales());
                    });
            });
    }

    [ContextMenu("Handle Fail")]
    public void HandleFail()
    {
        if (PortalTransform == null || IconTransform == null) return;

        PortalTransform.DOKill(false);
        IconTransform.DOKill(false);
        if (PortalMaterial != null) PortalMaterial.DOKill(false);

        ResetScales();

        // Aplica o flash e inicia a transi��o controlada via Sequence
        ApplyInstantColorFlash(Color.black);
        AnimateColorBack();

        float halfDuration = duration * 0.5f;

        PortalTransform.DOScale(initialPortalScale * FAIL_SCALE, halfDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                PortalTransform.DOScale(initialPortalScale, halfDuration).SetEase(Ease.OutBack);

                IconTransform.DOScale(initialIconScale * FAIL_SCALE, halfDuration)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        IconTransform.DOScale(initialIconScale, halfDuration).SetEase(Ease.OutBack)
                            .OnComplete(() => ResetScales());
                    });
            });
    }

    private void ApplyInstantColorFlash(Color targetColor)
    {
        if (PortalMaterial == null) return;

        PortalMaterial.SetColor(OuterColorProp, targetColor);
        PortalMaterial.SetColor(OuterGlowColorProp, targetColor);
        PortalMaterial.SetColor(InnerFrameColorProp, targetColor);
        PortalMaterial.SetColor(InnerColorAProp, targetColor);
        PortalMaterial.SetColor(InnerColorBProp, targetColor);
    }

    private void AnimateColorBack()
    {
        if (PortalMaterial == null) return;

        Sequence colorSequence = DOTween.Sequence();

        // Passando a string exata da propriedade do Shader no segundo par�metro,
        // o DOTween garante que a interpola��o funcione sem resetar instantaneamente.
        colorSequence.Join(PortalMaterial.DOColor(origOuterColor, OuterColorProp, colorFadeBackDuration));
        colorSequence.Join(PortalMaterial.DOColor(origOuterGlowColor, OuterGlowColorProp, colorFadeBackDuration));
        colorSequence.Join(PortalMaterial.DOColor(origInnerFrameColor, InnerFrameColorProp, colorFadeBackDuration));
        colorSequence.Join(PortalMaterial.DOColor(origInnerColorA, InnerColorAProp, colorFadeBackDuration));
        colorSequence.Join(PortalMaterial.DOColor(origInnerColorB, InnerColorBProp, colorFadeBackDuration));

        colorSequence.SetEase(Ease.OutQuad);
        colorSequence.Play();
    }

    private void ResetScales()
    {
        if (PortalTransform != null) PortalTransform.localScale = initialPortalScale;
        if (IconTransform != null) IconTransform.localScale = initialIconScale;
    }

    private void OnDestroy()
    {
        if (PortalMaterial != null)
        {
            Destroy(PortalMaterial);
        }
    }
}