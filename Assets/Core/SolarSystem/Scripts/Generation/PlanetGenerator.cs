using UnityEngine;
using MakoJBryant.SolarSystem.Generation; // Import the namespace for NoiseLayer, PerlinNoise3D, ColorSettings, SphereCreator

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

    // This is the array that will hold all your different noise configurations
    [Header("Noise Layers")]
    public NoiseLayer[] noiseLayers;

    // Global offset for terrain height
    [Header("Global Terrain Settings")]
    [Tooltip("Adjusts the overall height of the terrain relative to the base radius. Negative values will create oceans.")]
    public float globalHeightOffset = 0f;

    // Instance of your external ColorSettings class
    [Header("Color Settings")]
    public ColorSettings colorSettings; // This is the instance of your ColorSettings class

    // --- Private References (Unity Components) ---
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private Mesh mesh;

    // To store the actual min/max elevation (distance from planet center) for shader
    private float minElevation;
    private float maxElevation;

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
        // If you need to ensure the mesh object exists for other editor-time logic (e.g., if debugging other parts
        // of the script in the editor that rely on 'mesh' not being null), you can do a minimal check here.
        // However, AVOID re-initializing components (like calling GetComponent or AddComponent)
        // or assigning the mesh to meshFilter.sharedMesh here, as these trigger the SendMessage error.
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "Generated Planet Mesh";
        }
        // No need to update gradient texture here anymore as ColorSettings now uses Biomes array.
        // The material properties will be set when GeneratePlanet is called.
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

        // Initialize min/max elevation before calculating displacement
        // These will store the actual min/max distance from the planet's center.
        minElevation = float.MaxValue;
        maxElevation = float.MinValue;

        // Step 2: Apply MULTI-LAYERED FRACTAL BROWNIAN MOTION (FBM) noise to displace vertices
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertex = vertices[i]; // Store original vertex for normal direction
            Vector3 normalDirection = vertex.normalized; // Direction away from planet center

            float totalDisplacement = 0; // Accumulates total displacement from all layers (relative to base radius)
            float firstLayerValue = 0; // To be used for masking by subsequent layers

            // Iterate through each defined NoiseLayer
            foreach (NoiseLayer noiseLayer in noiseLayers)
            {
                if (!noiseLayer.enabled)
                {
                    continue; // Skip this layer if it's disabled
                }

                float currentLayerNoise = 0;
                float currentFrequency = noiseLayer.roughness; // Start frequency for this layer
                float currentAmplitude = 1; // Start amplitude for this layer
                float totalLayerAmplitude = 0; // Used for normalizing this layer's FBM output

                // Calculate FBM for this individual noise layer
                for (int j = 0; j < noiseLayer.octaves; j++)
                {
                    // Sample point for this octave, combining normal direction, layer offset, and frequency
                    Vector3 samplePoint = (normalDirection + noiseLayer.offset) * currentFrequency;

                    float v = PerlinNoise3D.GenerateNoise(samplePoint.x, samplePoint.y, samplePoint.z);

                    // Apply noise type specific modification
                    if (noiseLayer.noiseType == NoiseType.Ridge)
                    {
                        // Ridge noise: maps original noise range [0,1] to [0,1] but with a sharper, creased effect.
                        // v * 2 - 1 maps to [-1, 1]. Mathf.Abs makes it [0, 1]. 1 - Abs inverts it (valleys become peaks).
                        v = 1 - Mathf.Abs(v * 2 - 1);
                    }
                    else // Standard noise (map 0-1 to -1 to 1 for displacement around the sphere surface)
                    {
                        v = v * 2 - 1;
                    }

                    currentLayerNoise += v * currentAmplitude; // Accumulate noise for this layer

                    totalLayerAmplitude += currentAmplitude; // Track total amplitude for normalization
                    currentAmplitude *= noiseLayer.persistence; // Decrease amplitude for next octave
                    currentFrequency *= noiseLayer.lacunarity; // Increase frequency for next octave
                }

                // Normalize the current layer's noise sum by its total accumulated amplitude
                float normalizedLayerNoise = (totalLayerAmplitude == 0) ? 0 : currentLayerNoise / totalLayerAmplitude;

                // Apply minValue: Ensure noise is always above a certain baseline (e.g., for ocean floor)
                float finalLayerNoise = normalizedLayerNoise + noiseLayer.minValue;

                // If this layer is designated as the mask, store its value.
                // Assuming the first enabled layer found is the intended mask.
                if (noiseLayer.useFirstLayerAsMask)
                {
                    firstLayerValue = finalLayerNoise;
                }

                // Apply masking: If this layer uses the mask, only add its effect if the first layer's value is positive.
                if (noiseLayer.useFirstLayerAsMask && firstLayerValue <= 0)
                {
                    finalLayerNoise = 0;
                }

                // Accumulate this layer's contribution to the total displacement
                totalDisplacement += finalLayerNoise * noiseLayer.strength;
            }

            // Displace the vertex along its normal direction by the total accumulated displacement
            // The actual vertex position is the base radius + total displacement * its normal direction.
            // **** CRITICAL CHANGE HERE: Apply globalHeightOffset AFTER all noise layers ****
            vertices[i] = vertex + normalDirection * (totalDisplacement + globalHeightOffset);

            // Update min/max elevation based on the current vertex's *absolute distance from the origin*
            float currentVertexHeight = vertices[i].magnitude; // This is the absolute distance from planet center

            if (currentVertexHeight < minElevation)
            {
                minElevation = currentVertexHeight;
            }
            if (currentVertexHeight > maxElevation)
            {
                maxElevation = currentVertexHeight;
            }
        }

        // Step 3: Assign the modified data to the Mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs; // Assign UVs for texturing

        // IMPORTANT: Update the mesh's bounding box after changing vertices
        mesh.RecalculateBounds();

        // --- CUSTOM NORMAL CALCULATION (REPLACES mesh.RecalculateNormals()) ---
        Vector3[] normals = new Vector3[vertices.Length];
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int i1 = triangles[i];
            int i2 = triangles[i + 1];
            int i3 = triangles[i + 2];

            Vector3 v1 = vertices[i1];
            Vector3 v2 = vertices[i2];
            Vector3 v3 = vertices[i3];

            Vector3 faceNormal = Vector3.Cross(v2 - v1, v3 - v1).normalized;

            normals[i1] += faceNormal;
            normals[i2] += faceNormal;
            normals[i3] += faceNormal;
        }

        for (int i = 0; i < normals.Length; i++)
        {
            normals[i].Normalize();
        }
        mesh.normals = normals;
        // --- END CUSTOM NORMAL CALCULATION ---

        // Optional: Recalculate tangents if you plan to use normal maps in your shader.
        // mesh.RecalculateTangents();

        // Step 5: Assign the mesh to the MeshFilter (already done in Awake, but good to ensure)
        meshFilter.sharedMesh = mesh;

        // NEW: Assign the material and pass elevation data and biome data to it
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null && colorSettings != null && colorSettings.planetMaterial != null)
        {
            meshRenderer.sharedMaterial = colorSettings.planetMaterial;

            // Pass the absolute heights and radius to the shader
            meshRenderer.sharedMaterial.SetFloat("_Radius", radius);
            meshRenderer.sharedMaterial.SetFloat("_MinHeight", minElevation); // Pass calculated min height
            meshRenderer.sharedMaterial.SetFloat("_MaxHeight", maxElevation); // Pass calculated max height
            meshRenderer.sharedMaterial.SetColor("_OceanColor", colorSettings.oceanColor); // Pass ocean color

            // Pass biome data to the shader
            if (colorSettings.biomes != null && colorSettings.biomes.Length > 0)
            {
                // Create arrays to hold biome data for the shader
                Vector4[] biomeColors = new Vector4[colorSettings.biomes.Length];
                float[] biomeStartHeights = new float[colorSettings.biomes.Length];
                float[] biomeBlendAmounts = new float[colorSettings.biomes.Length];

                for (int b = 0; b < colorSettings.biomes.Length; b++)
                {
                    biomeColors[b] = colorSettings.biomes[b].color;
                    biomeStartHeights[b] = colorSettings.biomes[b].startHeight;
                    biomeBlendAmounts[b] = colorSettings.biomes[b].blendAmount;
                }

                meshRenderer.sharedMaterial.SetInt("_BiomeCount", colorSettings.biomes.Length);
                meshRenderer.sharedMaterial.SetVectorArray("_BiomeColors", biomeColors);
                meshRenderer.sharedMaterial.SetFloatArray("_BiomeStartHeights", biomeStartHeights);
                meshRenderer.sharedMaterial.SetFloatArray("_BiomeBlendAmounts", biomeBlendAmounts);
            }
            else
            {
                Debug.LogWarning("No biomes defined in Color Settings for PlanetGenerator on " + gameObject.name);
                meshRenderer.sharedMaterial.SetInt("_BiomeCount", 0); // Ensure shader knows there are no biomes
            }

            Debug.Log($"Shader parameters set: Radius={radius}, MinHeight={minElevation}, MaxHeight={maxElevation}");
        }
        else
        {
            if (meshRenderer == null) Debug.LogError("MeshRenderer not found!");
            if (colorSettings == null) Debug.LogError("Color Settings not assigned!");
            if (colorSettings != null && colorSettings.planetMaterial == null) Debug.LogError("Planet Material not assigned within Color Settings!");
        }

        // Step 6: Assign the mesh to the MeshCollider for physics interaction.
        if (meshCollider == null) meshCollider = GetComponent<MeshCollider>();
        if (meshCollider != null)
        {
            meshCollider.sharedMesh = mesh;
        }

        Debug.Log($"PlanetGenerator: Mesh assigned. Vertices: {mesh.vertexCount}, Triangles: {mesh.triangles.Length / 3}");
    }

    // Clean up the generated texture when the object is destroyed
    void OnDestroy()
    {
        // No biomeGradientTexture to clean up anymore
    }
}
