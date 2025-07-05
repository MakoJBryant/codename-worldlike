using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PlanetDiagnostic : MonoBehaviour
{
    public Material fadeMaterial; // Assign your LODFade material here

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Material instanceMaterial;

    private float fadeAmount = 0f;
    private bool increasing = true;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        // Create a simple sphere mesh if none assigned
        if (meshFilter.sharedMesh == null)
        {
            meshFilter.sharedMesh = CreateSimpleSphere();
        }

        if (fadeMaterial == null)
        {
            Debug.LogWarning("Please assign a fade material to this diagnostic.");
            enabled = false;
            return;
        }

        // Clone the material so we can modify _FadeAmount independently
        instanceMaterial = new Material(fadeMaterial);
        meshRenderer.material = instanceMaterial;
    }

    void Update()
    {
        // Animate fadeAmount between 0 and 1 over 3 seconds
        if (increasing)
        {
            fadeAmount += Time.deltaTime / 3f;
            if (fadeAmount >= 1f)
            {
                fadeAmount = 1f;
                increasing = false;
            }
        }
        else
        {
            fadeAmount -= Time.deltaTime / 3f;
            if (fadeAmount <= 0f)
            {
                fadeAmount = 0f;
                increasing = true;
            }
        }

        // Set fade amount on the material
        instanceMaterial.SetFloat("_FadeAmount", fadeAmount);
    }

    Mesh CreateSimpleSphere()
    {
        GameObject tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Mesh mesh = tempSphere.GetComponent<MeshFilter>().sharedMesh;
        Destroy(tempSphere);
        return mesh;
    }
}
