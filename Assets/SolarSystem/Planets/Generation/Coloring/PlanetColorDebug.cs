using UnityEngine;
using SolarSystem.Planets.Generation;
using SolarSystem.Planets.Settings;
using SolarSystem.Planets.Generation.Coloring;

[RequireComponent(typeof(MeshFilter))]
public class PlanetColorDebug : MonoBehaviour
{
    public ShapeSettings shapeSettings;
    public ColorSettings colorSettings;

    private MeshFilter meshFilter;
    private Mesh mesh;
    private ColorGenerator colorGenerator;

    [Header("Visualize Mode")]
    public bool visualizeElevation = true;  // If true, colors based on grayscale height

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        mesh = meshFilter.sharedMesh;

        if (shapeSettings == null || colorSettings == null)
        {
            Debug.LogError("Assign ShapeSettings and ColorSettings!");
            enabled = false;
            return;
        }

        colorGenerator = new ColorGenerator(colorSettings);

        Debug.Log("Starting PlanetColorDebug...");

        LogElevationRange();

        AssignVertexColors();
    }

    void LogElevationRange()
    {
        float minElev = float.MaxValue;
        float maxElev = float.MinValue;

        foreach (var v in mesh.vertices)
        {
            float height = v.magnitude - shapeSettings.radius;
            if (height < minElev) minElev = height;
            if (height > maxElev) maxElev = height;
        }

        Debug.Log($"[PlanetColorDebug] Vertex Height Range: min={minElev:F4}, max={maxElev:F4}");
    }

    void AssignVertexColors()
    {
        Color[] colors = new Color[mesh.vertexCount];
        float minHeight = colorSettings.minHeight;
        float maxHeight = colorSettings.maxHeight;
        float radius = shapeSettings.radius;

        for (int i = 0; i < mesh.vertexCount; i++)
        {
            Vector3 vertex = mesh.vertices[i];
            float height = vertex.magnitude - radius;

            // Normalize height for color mapping
            float normalizedHeight = Mathf.InverseLerp(minHeight, maxHeight, height);

            if (visualizeElevation)
            {
                // Grayscale color based on elevation
                colors[i] = Color.Lerp(Color.black, Color.white, normalizedHeight);
            }
            else
            {
                // Use your ColorGenerator gradient + tint for color
                colors[i] = colorGenerator.CalculateColor(vertex, radius);
            }

            // Optional debug: Log first 10 vertex colors
            if (i < 10)
            {
                Debug.Log($"Vertex {i}: height={height:F4}, normalized={normalizedHeight:F4}, color={colors[i]}");
            }
        }

        mesh.colors = colors;
        meshFilter.mesh = mesh;
    }
}
