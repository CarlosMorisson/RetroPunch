using UnityEngine;

public class MeshTrailGhost : MonoBehaviour
{
    private Material mat;
    private float lifetime;
    private float minAlpha;
    private float timer;

    public void Init(float life, float minA)
    {
        lifetime = life;
        minAlpha = minA;

        mat = GetComponent<MeshRenderer>().material;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = timer / lifetime;

        float alpha = Mathf.Lerp(1f, 0f, t);
        if (alpha <= minAlpha)
            alpha = 0f;

        Color c = mat.color;
        c.a = alpha;
        mat.color = c;

        if (alpha <= 0f)
            Destroy(gameObject);
    }
}
