using UnityEngine;

public class MeshTrailGhost : MonoBehaviour
{
    private const string SHADER_NAME_PROCEDURAL = "Custom/RetroWave Grid Procedural";
    private const string SHADER_NAME_URP = "Custom/RetroWave Grid";

    private Material mat;
    private string colorProperty;
    private float lifetime;
    private float minAlpha;
    private float timer;

    public void Init(float life, float minA)
    {
        lifetime = life;
        minAlpha = minA;

        mat = GetComponent<MeshRenderer>().material;
        if (mat == null)
        {
            Destroy(gameObject);
            return;
        }

        colorProperty = ResolveColorProperty(mat);
    }

    private static string ResolveColorProperty(Material material)
    {
        switch (material.shader.name)
        {
            case SHADER_NAME_PROCEDURAL:
                return "_BaseColor";
            case SHADER_NAME_URP:
                return "_PrimaryColor";
        }

        return material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
    }

    void Update()
    {
        if (mat == null)
        {
            Destroy(gameObject);
            return;
        }

        timer += Time.deltaTime;
        float t = timer / lifetime;

        float alpha = Mathf.Lerp(1f, 0f, t);
        if (alpha <= minAlpha)
            alpha = 0f;

        Color c = mat.GetColor(colorProperty);
        c.a = alpha;
        mat.SetColor(colorProperty, c);

        if (alpha <= 0f)
            Destroy(gameObject);
    }
}
