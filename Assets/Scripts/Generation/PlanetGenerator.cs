using UnityEngine;
using System.Collections.Generic; // Although not strictly used now, good to have for potential future lists

// [ExecuteInEditMode] allows the script to run even when the game is not playing.
// This is incredibly useful for procedural generation as you can see changes instantly
// when adjusting parameters in the Inspector.
[ExecuteInEditMode]
public class PlanetGenerator : MonoBehaviour
{
    // --- Public Parameters - Adjustable in the Inspector ---

    [Range(2, 256)] // Clamp resolution for reasonable performance in Editor
    public int resolution = 64; // Controls the detail of the planet mesh. Higher = more detailed.
    public float radius = 1f;   // The base radius of the planet.

    [Header("Noise Settings")]
    public float noiseStrength = 1.0f; // How much the noise displaces the terrain.
    public float noiseRoughness = 1.0f; // Frequency of the noise. Higher = more jagged/detailed.
    public Vector3 noiseOffset = Vector3.zero; // Offset for the noise (can be used to change patterns)

    // --- Private References (Unity Components) ---
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider; // For physics interactions
    private Mesh mesh; // The actual mesh data we will generate

    // --- Awake is called when the script instance is being loaded ---
    void Awake()
    {
        // Get references to components on this GameObject.
        // If they don't exist, add them programmatically.
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();

        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

        meshCollider = GetComponent<MeshCollider>();
        if (meshCollider == null) meshCollider = gameObject.AddComponent<MeshCollider>();

        // Initialize a new Mesh object.
        mesh = new Mesh();
        mesh.name = "Generated Planet Mesh"; // Give it a name for easier debugging
        meshFilter.sharedMesh = mesh; // Assign the mesh to the MeshFilter

        // Immediately generate the planet when the game starts or in editor.
        GeneratePlanet();
    }

    // --- OnValidate is called in the editor when a script is loaded or a value is changed in the Inspector ---
    // This allows us to regenerate the planet whenever we tweak parameters.
    void OnValidate()
    {
        // Ensure components are initialized before generating in OnValidate (important for Edit Mode)
        if (meshFilter == null || meshRenderer == null || meshCollider == null || mesh == null)
        {
            Awake(); // Re-initialize components if needed (e.g., after script recompilation)
        }
        else
        {
            GeneratePlanet();
        }
    }

    // --- Context Menu allows right-clicking the component in the Inspector to trigger a method ---
    [ContextMenu("Generate Planet")]
    public void GeneratePlanet()
    {
        Debug.Log("Generating Planet with Resolution: " + resolution + ", Radius: " + radius);

        // Clear any previous mesh data
        mesh.Clear();

        // Step 1: Get the base spherical mesh data from SphereCreator
        Vector3[] vertices;
        int[] triangles;
        Vector2[] uvs;
        SphereCreator.CreateSphereMesh(resolution, radius, out vertices, out triangles, out uvs);

        // Step 2: Apply noise to displace vertices
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertex = vertices[i];

            // Get the direction from the planet's center to the current vertex.
            // For a sphere generated at the origin, this is just the normalized vertex position.
            Vector3 normalDirection = vertex.normalized;

            // Sample 3D Perlin noise based on the vertex position (scaled by roughness and offset)
            // We use the normalized position here so that noise looks continuous over the sphere surface.
            float noiseValue = PerlinNoise3D.GenerateNoise(
                (normalDirection.x + noiseOffset.x) * noiseRoughness,
                (normalDirection.y + noiseOffset.y) * noiseRoughness,
                (normalDirection.z + noiseOffset.z) * noiseRoughness
            );

            // Normalize noise from 0-1 to -1 to 1 range (or any other desired range for displacement)
            // (noiseValue * 2 - 1) makes 0-1 range into -1 to 1 range.
            // This allows for both inward and outward displacement.
            float displacement = (noiseValue * 2 - 1) * noiseStrength;

            // Displace the vertex along its normal direction
            vertices[i] = vertex + normalDirection * displacement;
        }

        // Step 3: Assign the modified data to the Mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs; // Assign UVs for texturing

        // Step 4: Recalculate normals for proper lighting. This is crucial!
        mesh.RecalculateNormals();

        // Optional: Recalculate tangents if you plan to use normal maps in your shader.
        // mesh.RecalculateTangents(); 

        // Step 5: Assign the mesh to the MeshFilter (already done in Awake, but good to ensure)
        meshFilter.sharedMesh = mesh;

        // Step 6: Assign the mesh to the MeshCollider for physics interaction.
        meshCollider.sharedMesh = mesh;

        Debug.Log($"PlanetGenerator: Mesh assigned. Vertices: {mesh.vertexCount}, Triangles: {mesh.triangles.Length / 3}");
    }
}