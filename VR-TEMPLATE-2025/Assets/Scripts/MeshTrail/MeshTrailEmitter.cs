using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MeshTrailEmitter : MonoBehaviour
{
    [Header("Trail Settings")]
    public float spawnInterval = 0.05f;
    public float trailLifetime = 0.4f;
    public float minAlpha = 0.05f;

    [Header("Hierarchy")]
    public Transform trailParent;

    [Header("Material Source")]
    [Tooltip("Renderer do cubo dono desse emitter. Se definido, os ghosts usam o material atual desse renderer.")]
    public Renderer materialSource;

    private SkinnedMeshRenderer skinnedMesh;
    private MeshRenderer meshRenderer;
    private bool emitting;

    private readonly List<GameObject> activeGhosts = new List<GameObject>();

    void Awake()
    {
        skinnedMesh = GetComponentInChildren<SkinnedMeshRenderer>();
        meshRenderer = GetComponentInChildren<MeshRenderer>();
    }

    private void OnEnable()
    {
        StartTrail();
    }

    private void OnDisable()
    {
        StopTrail();
    }

    public void StartTrail()
    {
        if (!emitting)
            StartCoroutine(EmitTrail());
    }

    public void StopTrail()
    {
        emitting = false;
    }

    /// <summary>
    /// Para a emissão e destrói todos os ghosts ainda ativos provenientes deste emitter.
    /// </summary>
    public void StopAndKillGhosts()
    {
        StopTrail();

        activeGhosts.RemoveAll(g => g == null);

        foreach (GameObject ghost in activeGhosts)
            Destroy(ghost);

        activeGhosts.Clear();
    }

    IEnumerator EmitTrail()
    {
        emitting = true;

        while (emitting)
        {
            CreateAfterImage();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void CreateAfterImage()
    {
        GameObject ghost = new GameObject("MeshTrailGhost");
        ghost.transform.SetParent(trailParent != null ? trailParent : null);
        ghost.transform.SetPositionAndRotation(transform.position, transform.rotation);
        ghost.transform.localScale = transform.localScale;

        Mesh mesh = new Mesh();

        if (skinnedMesh != null)
        {
            skinnedMesh.BakeMesh(mesh);
            var mf = ghost.AddComponent<MeshFilter>();
            mf.mesh = mesh;

            var mr = ghost.AddComponent<MeshRenderer>();
            mr.material = ResolveMaterial(skinnedMesh.material);
        }
        else if (meshRenderer != null)
        {
            var sourceMF = meshRenderer.GetComponent<MeshFilter>();
            if (!sourceMF)
            {
                Destroy(ghost);
                return;
            }

            var mf = ghost.AddComponent<MeshFilter>();
            mf.mesh = sourceMF.mesh;

            var mr = ghost.AddComponent<MeshRenderer>();
            mr.material = ResolveMaterial(meshRenderer.material);
        }
        else
        {
            Destroy(ghost);
            return;
        }

        ghost.AddComponent<MeshTrailGhost>().Init(trailLifetime, minAlpha);
        activeGhosts.Add(ghost);
    }

    // Retorna o material do materialSource se definido, senão usa o fallback.
    private Material ResolveMaterial(Material fallback)
    {
        if (materialSource != null)
            return materialSource.material;
        return fallback;
    }
}
