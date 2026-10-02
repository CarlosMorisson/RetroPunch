using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MeshTrailEmitter : MonoBehaviour
{
    [Header("Trail Settings")]
    public float spawnInterval = 0.05f;
    public float trailLifetime = 0.4f;
    public float minAlpha = 0.05f;
    [Tooltip("Tempo de espera antes de voltar a emitir quando o emitter é desativado e reativado.")]
    public float reactivationDelay = 2f;

    [Header("Hierarchy")]
    public Transform trailParent;

    [Header("Material Source")]
    [Tooltip("Renderer do cubo dono desse emitter. Se definido, os ghosts usam o material atual desse renderer.")]
    public Renderer materialSource;

    private SkinnedMeshRenderer skinnedMesh;
    private MeshRenderer meshRenderer;
    private bool emitting;
    private Coroutine emitRoutine;
    private bool wasDisabled;
    private float pendingDelay;

    private readonly List<GameObject> activeGhosts = new List<GameObject>();

    void Awake()
    {
        skinnedMesh = GetComponentInChildren<SkinnedMeshRenderer>();
        meshRenderer = GetComponentInChildren<MeshRenderer>();
    }

    private void OnEnable()
    {
        // Na reativação, destrói qualquer ghost remanescente e aguarda o delay antes de emitir de novo.
        if (wasDisabled)
        {
            StopAndKillGhosts();
            pendingDelay = reactivationDelay;
        }

        StartTrail();
    }

    // Ao desativar (componente ou GameObject), encerra a emissão e destrói todos os ghosts.
    private void OnDisable()
    {
        wasDisabled = true;
        StopAndKillGhosts();
    }

    private void OnDestroy()
    {
        StopAndKillGhosts();
    }

    public void StartTrail()
    {
        if (emitting || !isActiveAndEnabled)
            return;

        emitting = true;
        emitRoutine = StartCoroutine(EmitTrail(pendingDelay));
        pendingDelay = 0f;
    }

    public void StopTrail()
    {
        emitting = false;

        if (emitRoutine != null)
        {
            StopCoroutine(emitRoutine);
            emitRoutine = null;
        }
    }

    /// <summary>
    /// Para a emissão e destrói todos os ghosts ainda ativos provenientes deste emitter.
    /// </summary>
    public void StopAndKillGhosts()
    {
        StopTrail();

        foreach (GameObject ghost in activeGhosts)
        {
            if (ghost == null) continue;

            // Esconde já neste frame; o Destroy só efetiva no fim do frame.
            ghost.SetActive(false);
            Destroy(ghost);
        }

        activeGhosts.Clear();
    }

    IEnumerator EmitTrail(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        while (emitting && isActiveAndEnabled)
        {
            CreateAfterImage();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void CreateAfterImage()
    {
        if (!emitting || !isActiveAndEnabled)
            return;

        GameObject ghost = new GameObject("MeshTrailGhost");
        ghost.transform.SetParent(trailParent != null ? trailParent : null);
        ghost.transform.SetPositionAndRotation(transform.position, transform.rotation);
        ghost.transform.localScale = transform.localScale;

        Mesh bakedMesh = null;

        if (skinnedMesh != null)
        {
            Mesh mesh = new Mesh();
            skinnedMesh.BakeMesh(mesh);
            bakedMesh = mesh;
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

        ghost.AddComponent<MeshTrailGhost>().Init(trailLifetime, minAlpha, bakedMesh);

        // Remove referências de ghosts que já se autodestruíram para a lista não crescer indefinidamente.
        activeGhosts.RemoveAll(g => g == null);
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
