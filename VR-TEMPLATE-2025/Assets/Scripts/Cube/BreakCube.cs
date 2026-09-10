using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreakCube : MonoBehaviour
{
    [Header("Explosion References")]
    public Transform explosionCenter;
    public Transform targetParents;
    private List<Rigidbody> targets = new();

    private struct FragmentState
    {
        public Transform transform;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    private List<FragmentState> fragmentInitialStates;

    [Header("Explosion Settings")]
    public float explosionForce = 10f;
    public float explosionRadius = 5f;
    public float upwardsModifier = 0.5f;

    [Header("Color")]
    public ParticleSystem FeedbackParticle;
    private Material particleMaterial;
    public Color ParticleColor;

    [Header("Debug")]
    public bool drawGizmos = true;

    private const float SHADOW_TIME = 1f;

    private const float ACTIVE_TIME = 3f;

    private static readonly int EmissionColorProperty = Shader.PropertyToID("_EmissionColor");


    /// <summary>
    /// Dispara a explos�o reposicionando o centro
    /// </summary>
    public void TriggerExplosion(Vector3 position, Transform objectTransform)
    {
        transform.position = objectTransform.position;
        transform.localRotation=objectTransform.localRotation;
        if (explosionCenter != null)
            explosionCenter.position = position;
        ResetFragments();
        GetTargets();
        if (ColorController.Instance != null)
        {
            Color primaryColor = ColorController.Instance.CurrentPrimary;
            SetupParticleMaterial(primaryColor);
        }
        if (FeedbackParticle != null)
            FeedbackParticle.Play();
        Explode();
        StartCoroutine(WaitToActive());
    }
    /// <summary>
    /// Instancia o material do ParticleSystemRenderer e muda a cor da emiss�o
    /// </summary>
    private void SetupParticleMaterial(Color colorToApply)
    {
        if (FeedbackParticle == null) return;

        if (FeedbackParticle.TryGetComponent<ParticleSystemRenderer>(out ParticleSystemRenderer psRenderer))
        {
            if (particleMaterial == null && psRenderer.material != null)
            {
                particleMaterial = new Material(psRenderer.material);
                psRenderer.material = particleMaterial;
            }
            if (particleMaterial != null)
            {
                particleMaterial.EnableKeyword("_EMISSION");
                particleMaterial.SetColor(EmissionColorProperty, colorToApply);
            }
        }
    }
    private IEnumerator WaitToActive()
    {
        yield return new WaitForSeconds(ACTIVE_TIME);
        foreach (Transform child in targetParents)
        {
            if (child.gameObject.TryGetComponent<MeshTrailEmitter>(out MeshTrailEmitter trailEmitter))
            {
                trailEmitter.enabled = true;
            }
        }
        yield return new WaitForSeconds(ACTIVE_TIME);
        foreach (Transform child in targetParents)
        {
            child.gameObject.SetActive(false);
        }
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Reativa os fragmentos e devolve cada um para sua posi��o, rota��o e escala iniciais,
    /// alem de zerar a velocidade dos rigidbodies, antes de uma nova explosao.
    /// </summary>
    private void ResetFragments()
    {
        if (targetParents == null) return;

        if (fragmentInitialStates == null)
        {
            fragmentInitialStates = new List<FragmentState>();
            foreach (Transform child in targetParents)
            {
                fragmentInitialStates.Add(new FragmentState
                {
                    transform = child,
                    position = child.localPosition,
                    rotation = child.localRotation,
                    scale = child.localScale
                });
            }
        }

        foreach (var state in fragmentInitialStates)
        {
            if (state.transform == null) continue;

            state.transform.gameObject.SetActive(true);
            state.transform.localPosition = state.position;
            state.transform.localRotation = state.rotation;
            state.transform.localScale = state.scale;

            if (state.transform.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
    /// <summary>
    /// Aplica for�a de explos�o nos objetos da lista
    /// </summary>
    private void Explode()
    {
        Vector3 center = explosionCenter.position;

        foreach (var rb in targets)
        {
            if (rb == null) continue;

            rb.useGravity = true;

            Vector3 dir = rb.worldCenterOfMass - center;
            float distance = dir.magnitude;

            if (distance > explosionRadius || distance <= 0.001f)
                continue;

            float strength = Mathf.Pow(1f - (distance / explosionRadius), 2f);
            Vector3 forceDir = dir.normalized;

            Vector3 force = forceDir * explosionForce * strength;

            force += Vector3.up * upwardsModifier * strength;

            rb.AddForceAtPosition(force, rb.worldCenterOfMass, ForceMode.Impulse);
        }
    }

    /// <summary>
    /// Preenche a lista de rigidbodies
    /// </summary>
    private void GetTargets()
    {
        targets.Clear();

        foreach(Transform child in targetParents)
        {
            if (child.gameObject.TryGetComponent<MeshTrailEmitter>(out MeshTrailEmitter trailEmitter))
            {
                trailEmitter.enabled = false;
            }
            targets.Add(child.GetComponent<Rigidbody>());
        }
    }
    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos || explosionCenter == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(explosionCenter.position, explosionRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(
            explosionCenter.position,
            explosionCenter.position + Vector3.up * upwardsModifier
        );
    }

    #endregion

}
