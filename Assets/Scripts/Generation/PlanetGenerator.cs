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
    public float noiseRoughness = 1.0f; // This will now act as the base frequency for FBM
    public Vector3 noiseOffset = Vector3.zero;

    [Range(1, 8)] // Number of noise layers (octaves)
    public int octaves = 4;

    [Range(0.01f, 1.0f)] // How much amplitude decreases with each octave
    public float persistence = 0.5f;

    [Range(1.0f, 4.0f)] // How much frequency increases with each octave
    public float lacunarity = 2.0f;

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

        // Step 2: Apply FRACTAL BROWNIAN MOTION (FBM) noise to displace vertices
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertex = vertices[i];

            // Get the direction from the planet's center to the current vertex.
            // For a sphere generated at the origin, this is just the normalized vertex position.
            Vector3 normalDirection = vertex.normalized;

            // --- FRACTAL BROWNIAN MOTION (FBM) NOISE CALCULATION ---
            float noiseSum = 0;
            float currentAmplitude = 1; // Starts at full amplitude for the first octave
            float currentFrequency = 1; // Starts at base frequency for the first octave
            float totalAmplitude = 0;   // Used for normalizing the final noise value

            // Loop through multiple octaves (layers) of noise
            for (int j = 0; j < octaves; j++)
            {
                // Sample the 3D Perlin noise for the current octave
                // The position is scaled by noiseRoughness (base frequency) and currentFrequency (octave's specific frequency)
                float sampleX = (normalDirection.x + noiseOffset.x) * (noiseRoughness * currentFrequency);
                float sampleY = (normalDirection.y + noiseOffset.y) * (noiseRoughness * currentFrequency);
                float sampleZ = (normalDirection.z + noiseOffset.z) * (noiseRoughness * currentFrequency);

                float octaveNoise = PerlinNoise3D.GenerateNoise(sampleX, sampleY, sampleZ);

                // PerlinNoise3D returns a value between 0.0 and 1.0.
                // We map it to a range between -1.0 and 1.0 for terrain displacement (0.5 becomes 0).
                float scaledOctaveNoise = (octaveNoise * 2.0f - 1.0f);

                // Add this octave's scaled noise, weighted by its current amplitude, to the total sum
                noiseSum += scaledOctaveNoise * currentAmplitude;

                // Accumulate the amplitude to normalize the final noise sum later
                totalAmplitude += currentAmplitude;

                // Decrease amplitude (persistence) and increase frequency (lacunarity) for the next octave
                currentAmplitude *= persistence;
                currentFrequency *= lacunarity;
            }

            // Normalize the final noise sum by the sum of amplitudes.
            // This brings the FBM output to a more predictable range, typically centered around 0.
            float finalNormalizedNoise = noiseSum / totalAmplitude;

            // Apply the overall noise strength to determine the final displacement amount
            float displacement = finalNormalizedNoise * noiseStrength;
            // --- END OF FBM CALCULATION ---

            // Displace the vertex along its normal direction by the calculated amount
            vertices[i] = vertex + normalDirection * displacement;
        }

        // Step 3: Assign the modified data to the Mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs; // Assign UVs for texturing

        // IMPORTANT: Update the mesh's bounding box after changing vertices
        mesh.RecalculateBounds();

        // --- CUSTOM NORMAL CALCULATION (REPLACES mesh.RecalculateNormals()) ---
        Vector3[] normals = new Vector3[vertices.Length];
        // Loop through each triangle to calculate face normals and accumulate them per vertex
        for (int i = 0; i < triangles.Length; i += 3)
        {
            // Get the indices of the three vertices of the current triangle
            int i1 = triangles[i];
            int i2 = triangles[i + 1];
            int i3 = triangles[i + 2];

            // Get the actual vertex positions
            Vector3 v1 = vertices[i1];
            Vector3 v2 = vertices[i2];
            Vector3 v3 = vertices[i3];

            // Calculate the face normal using the cross product
            // The order matters for direction: (v2 - v1) x (v3 - v1) gives the normal pointing outwards from the face
            Vector3 faceNormal = Vector3.Cross(v2 - v1, v3 - v1).normalized;

            // Add this face normal to the normal of each vertex in the triangle.
            // Vertices shared by multiple triangles will have their normals averaged this way.
            normals[i1] += faceNormal;
            normals[i2] += faceNormal;
            normals[i3] += faceNormal;
        }

        // After accumulating all face normals, normalize each vertex normal to get the final smoothed normal
        for (int i = 0; i < normals.Length; i++)
        {
            // Normalize each accumulated normal. This averages the directions of contributing face normals.
            normals[i].Normalize();
        }
        mesh.normals = normals; // Assign the custom calculated normals to the mesh
        // --- END CUSTOM NORMAL CALCULATION ---

        // Optional: Recalculate tangents if you plan to use normal maps in your shader.
        // mesh.RecalculateTangents(); // Not needed if you're not using normal maps derived from texture

        // Step 5: Assign the mesh to the MeshFilter (already done in Awake, but good to ensure)
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