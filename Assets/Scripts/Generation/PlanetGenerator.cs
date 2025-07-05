using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class PlanetGenerator : MonoBehaviour
{
    // --- Public Parameters - Adjustable in the Inspector ---

    [Range(2, 256)]
    public int resolution = 64;
    public float radius = 1f;

    [Header("Noise Settings")]
    public float noiseStrength = 1.0f;
    public float noiseRoughness = 1.0f;
    public Vector3 noiseOffset = Vector3.zero;

    // --- Private References (Unity Components) ---
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private Mesh mesh;

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

        // Initialize a new Mesh object if it's null (e.g., first time Awake is called)
        // or if it was cleared previously.
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "Generated Planet Mesh";
        }

        // Assign the mesh to the MeshFilter. This can cause SendMessage issues
        // if done repeatedly or from OnValidate, but is fine here in Awake.
        if (meshFilter.sharedMesh != mesh) // Only assign if different to reduce redundant calls
        {
            meshFilter.sharedMesh = mesh;
        }


        // Immediately generate the planet when the game starts or in editor's Awake.
        GeneratePlanet();
    }

    // --- OnValidate is called in the editor when a script is loaded or a value is changed in the Inspector ---
    // IMPORTANT: This method's content has been specifically designed to AVOID the SendMessage error.
    // It *must not* call Awake() or GeneratePlanet() directly or indirectly.
    // Its purpose here is minimal initialization for editor-time context,
    // relying on Awake() for runtime and the Context Menu for editor-time generation.
    void OnValidate()
    {
        // For immediate feedback on property changes in the editor,
        // you should rely on the "Generate Planet Now" context menu button.

        // If you need to ensure the mesh object exists for other editor-time logic (e.g., if debugging other parts
        // of the script in the editor that rely on 'mesh' not being null), you can do a minimal check here.
        // However, AVOID re-initializing components (like calling GetComponent or AddComponent)
        // or assigning the mesh to meshFilter.sharedMesh here, as these trigger the SendMessage error.
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "Generated Planet Mesh";
        }

        // DO NOT uncomment or add calls to GeneratePlanet() or Awake() here.
        // Any such calls here will reintroduce the "SendMessage" error.
    }

    // --- Context Menu allows right-clicking the component in the Inspector to trigger a method ---
    [ContextMenu("Generate Planet Now")]
    public void GeneratePlanet()
    {
        Debug.Log("Generating Planet with Resolution: " + resolution + ", Radius: " + radius);

        // Ensure mesh and meshFilter are initialized. This is crucial for the context menu
        // if GeneratePlanet is called before Awake has run (e.g., right after script recompile).
        if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null) { Debug.LogError("MeshFilter not found!"); return; } // Safety check

        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "Generated Planet Mesh";
            meshFilter.sharedMesh = mesh; // Ensure the filter has the mesh if we just created it
        }


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
        // This line (meshFilter.sharedMesh = mesh;) is at the heart of the "SendMessage" error
        // when called from OnValidate. It is now only called from Awake or via the Context Menu.
        meshFilter.sharedMesh = mesh;

        // Step 6: Assign the mesh to the MeshCollider for physics interaction.
        // Ensure meshCollider exists before assigning to it
        if (meshCollider == null) meshCollider = GetComponent<MeshCollider>();
        if (meshCollider != null) // Only assign if collider is present
        {
            meshCollider.sharedMesh = mesh;
        }


        Debug.Log($"PlanetGenerator: Mesh assigned. Vertices: {mesh.vertexCount}, Triangles: {mesh.triangles.Length / 3}");
    }
}