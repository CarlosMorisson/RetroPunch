using System.Collections.Generic;
using UnityEngine;

public class BreakCube : MonoBehaviour
{
    [Header("Explosion References")]
    public Transform explosionCenter;
    public Transform targetParents;
    private List<Rigidbody> targets = new();

    [Header("Explosion Settings")]
    public float explosionForce = 10f;
    public float explosionRadius = 5f;
    public float upwardsModifier = 0.5f;

    [Header("Debug")]
    public bool drawGizmos = true;

    [ContextMenu("Trigger")]
    public void TestExplosion()
    {
        TriggerExplosion(explosionCenter.position);
    }

    /// <summary>
    /// Dispara a explosão reposicionando o centro
    /// </summary>
    public void TriggerExplosion(Vector3 position)
    {
        if (explosionCenter != null)
            explosionCenter.position = position;
        GetTargets();
        Explode();
    }

    /// <summary>
    /// Aplica força de explosão nos objetos da lista
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
        foreach(Transform child in targetParents)
        {
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
