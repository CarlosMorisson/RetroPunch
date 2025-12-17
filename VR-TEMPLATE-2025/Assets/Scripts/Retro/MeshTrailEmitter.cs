using UnityEngine;
using System.Collections;

public class MeshTrailEmitter : MonoBehaviour
{
    [Header("Trail Settings")]
    public float spawnInterval = 0.05f;
    public float trailLifetime = 0.4f;
    public float minAlpha = 0.05f;
    [Header("Hierarchy")]
    public Transform trailParent;

    private SkinnedMeshRenderer skinnedMesh;
    private MeshRenderer meshRenderer;
    private bool emitting;

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
            mr.material = skinnedMesh.material;
        }
        else if (meshRenderer != null)
        {
            var sourceMF = meshRenderer.GetComponent<MeshFilter>();
            if (!sourceMF) return;

            var mf = ghost.AddComponent<MeshFilter>();
            mf.mesh = sourceMF.mesh;

            var mr = ghost.AddComponent<MeshRenderer>();
            mr.material = meshRenderer.material;
        }

        ghost.AddComponent<MeshTrailGhost>()
             .Init(trailLifetime, minAlpha);
    }
}
