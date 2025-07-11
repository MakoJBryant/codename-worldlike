using UnityEngine;
// No need for System.Collections.Generic here if not using Lists directly anymore.
// Using 'NoiseLayer' directly implies the file containing 'NoiseLayer' is accessible.

// [ExecuteInEditMode] allows the script to run even when the game is not playing.
// This is incredibly useful for procedural generation as you can see changes instantly
// when adjusting parameters in the Inspector.
[ExecuteInEditMode]
public class PlanetGenerator : MonoBehaviour
{
    // --- Public Parameters - Adjustable in the Inspector ---

    [Range(2, 256)] // Clamp resolution for reasonable performance in Editor
    public int resolution = 64; // Controls the detail of the planet mesh. Higher = more detailed.
    public float radius = 1f;    // The base radius of the planet.

    // This is the array that will hold all your different noise configurations
    [Header("Noise Layers")]
    public NoiseLayer[] noiseLayers;

    // NEW: Global offset for terrain height
    [Header("Global Terrain Settings")] // <--- NEW HEADER
    [Tooltip("Adjusts the overall height of the terrain relative to the base radius. Negative values will create oceans.")] // <--- TOOLTIP
    public float globalHeightOffset = 0f; // <--- NEW VARIABLE: THIS IS THE ONE YOU NEED TO ADJUST IN INSPECTOR!

    // NEW: Color settings for the planet's surface
    // This is the [System.Serializable] class that will appear in the Inspector
    [System.Serializable]
    public class ColorSettings // <--- DEFINITION OF COLORSETTINGS CLASS
    {
        public Material planetMaterial; // Drag your Shader Graph Material here
        public Gradient biomeGradient;    // Define your biome colors here

        // Add a resolution for the gradient texture. 256 is usually good.
        public int gradientResolution = 256;
    }

    [Header("Color Settings")]
    public ColorSettings colorSettings; // This is the instance of your ColorSettings class

    // --- Private References (Unity Components) ---
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private Mesh mesh;

    // NEW: To store the actual min/max elevation (distance from planet center) for shader
    private float minElevation;
    private float maxElevation;

    // --- Private Texture for the Gradient ---
    private Texture2D biomeGradientTexture; // Will hold the converted gradient

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

        // Ensure the gradient texture is updated if the gradient changes in the editor
        UpdateBiomeGradientTexture();
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

        // NEW: Initialize min/max elevation before calculating displacement
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

            // --- DEBUG: Only log for the first vertex to avoid spamming console ---
            bool isFirstVertex = (i == 0); // Boolean flag for debugging
            if (isFirstVertex) Debug.Log("--- Debugging Noise for Vertex 0 ---");

            int noiseLayerIndex = 0; // Keep track of current noise layer for debugging output
            // Iterate through each defined NoiseLayer
            foreach (NoiseLayer noiseLayer in noiseLayers)
            {
                if (!noiseLayer.enabled)
                {
                    if (isFirstVertex) Debug.Log($"  Layer {noiseLayerIndex}: Disabled, Skipping.");
                    noiseLayerIndex++;
                    continue; // Skip this layer if it's disabled
                }

                float currentLayerNoise = 0;
                float currentFrequency = noiseLayer.roughness; // Start frequency for this layer
                float currentAmplitude = 1; // Start amplitude for this layer
                float totalLayerAmplitude = 0; // Used for normalizing this layer's FBM output

                if (isFirstVertex) Debug.Log($"  Layer {noiseLayerIndex}: (Enabled: {noiseLayer.enabled}, Strength: {noiseLayer.strength}, Min Value: {noiseLayer.minValue})");

                // Calculate FBM for this individual noise layer
                for (int j = 0; j < noiseLayer.octaves; j++)
                {
                    // Sample point for this octave, combining normal direction, layer offset, and frequency
                    Vector3 samplePoint = (normalDirection + noiseLayer.offset) * currentFrequency;

                    float v = PerlinNoise3D.GenerateNoise(samplePoint.x, samplePoint.y, samplePoint.z);

                    if (isFirstVertex) Debug.Log($"    Octave {j}: Sample Point={samplePoint}, Raw Noise (0-1)={v}");

                    // Apply noise type specific modification
                    if (noiseLayer.noiseType == NoiseType.Ridge)
                    {
                        // Ridge noise: maps original noise range [0,1] to [0,1] but with a sharper, creased effect.
                        // v * 2 - 1 maps to [-1, 1]. Mathf.Abs makes it [0, 1]. 1 - Abs inverts it (valleys become peaks).
                        v = 1 - Mathf.Abs(v * 2 - 1);
                        if (isFirstVertex) Debug.Log($"      Ridge Noise (0-1)={v}");
                    }
                    else // Standard noise (map 0-1 to -1 to 1 for displacement around the sphere surface)
                    {
                        v = v * 2 - 1;
                        if (isFirstVertex) Debug.Log($"      Standard Noise (-1-1)={v}");
                    }

                    currentLayerNoise += v * currentAmplitude; // Accumulate noise for this layer

                    totalLayerAmplitude += currentAmplitude; // Track total amplitude for normalization
                    currentAmplitude *= noiseLayer.persistence; // Decrease amplitude for next octave
                    currentFrequency *= noiseLayer.lacunarity; // Increase frequency for next octave
                }

                // Normalize the current layer's noise sum by its total accumulated amplitude
                float normalizedLayerNoise = (totalLayerAmplitude == 0) ? 0 : currentLayerNoise / totalLayerAmplitude;
                if (isFirstVertex) Debug.Log($"  Normalized Layer Noise: {normalizedLayerNoise}");

                // Apply minValue: Ensure noise is always above a certain baseline (e.g., for ocean floor)
                // IMPORTANT: Removed Mathf.Max(0, ...) here to allow negative displacement
                float finalLayerNoise = normalizedLayerNoise + noiseLayer.minValue;
                if (isFirstVertex) Debug.Log($"  Final Layer Noise (after MinValue, NO Max0): {finalLayerNoise}");

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
                if (isFirstVertex) Debug.Log($"  Current Total Displacement: {totalDisplacement}");
                noiseLayerIndex++; // Increment layer index for the next iteration
            }

            // Displace the vertex along its normal direction by the total accumulated displacement
            // The actual vertex position is the base radius + total displacement * its normal direction.
            // **** CRITICAL CHANGE HERE: Apply globalHeightOffset AFTER all noise layers ****
            vertices[i] = vertex + normalDirection * (totalDisplacement + globalHeightOffset);

            // NEW: Update min/max elevation based on the current vertex's *absolute distance from the origin*
            float currentVertexHeight = vertices[i].magnitude; // This is the absolute distance from planet center
            if (isFirstVertex) Debug.Log($"  Vertex {i} Final Height (Magnitude) after Global Offset: {currentVertexHeight}"); // Log final height for this vertex

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

        // NEW: Assign the material and pass elevation data to it
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null && colorSettings != null && colorSettings.planetMaterial != null)
        {
            meshRenderer.sharedMaterial = colorSettings.planetMaterial;

            // Pass the absolute heights and radius to the shader
            meshRenderer.sharedMaterial.SetFloat("_Radius", radius);
            meshRenderer.sharedMaterial.SetFloat("_MinHeight", minElevation); // Pass calculated min height
            meshRenderer.sharedMaterial.SetFloat("_MaxHeight", maxElevation); // Pass calculated max height

            // NEW: Update and assign the biome gradient texture
            UpdateBiomeGradientTexture();
            if (biomeGradientTexture != null)
            {
                // "_BiomeGradientTex" is the common reference name for a Texture2D property in your Shader Graph
                // that samples a gradient. You MUST make sure your Shader Graph has a 'Texture2D' property
                // on its Blackboard with this Reference name.
                meshRenderer.sharedMaterial.SetTexture("_BiomeGradientTex", biomeGradientTexture);
            }
            else
            {
                Debug.LogWarning("Biome Gradient Texture could not be generated!");
            }

            Debug.Log($"Shader parameters set: Radius={radius}, MinHeight={minElevation}, MaxHeight={maxElevation}");
        }
        else
        {
            if (meshRenderer == null) Debug.LogError("MeshRenderer not found!");
            if (colorSettings == null) Debug.LogError("Color Settings not assigned!");
            // Fix: Changed 'settingsMaterial' to 'planetMaterial'
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

    // NEW Helper Method: Converts the Gradient to a Texture2D
    void UpdateBiomeGradientTexture()
    {
        if (colorSettings == null || colorSettings.biomeGradient == null)
        {
            if (biomeGradientTexture != null)
            {
                DestroyImmediate(biomeGradientTexture); // Clean up old texture
                biomeGradientTexture = null;
            }
            return;
        }

        // Create a new texture if it doesn't exist or if resolution changed
        if (biomeGradientTexture == null || biomeGradientTexture.width != colorSettings.gradientResolution)
        {
            if (biomeGradientTexture != null) DestroyImmediate(biomeGradientTexture); // Destroy old texture if resolution changed
            biomeGradientTexture = new Texture2D(colorSettings.gradientResolution, 1);
            biomeGradientTexture.filterMode = FilterMode.Bilinear; // Smooth transitions
            biomeGradientTexture.wrapMode = TextureWrapMode.Clamp;  // Prevent repeating
        }

        // Populate the texture with colors from the gradient
        for (int i = 0; i < colorSettings.gradientResolution; i++)
        {
            // Sample the gradient from 0 to 1
            Color color = colorSettings.biomeGradient.Evaluate((float)i / (colorSettings.gradientResolution - 1));
            biomeGradientTexture.SetPixel(i, 0, color);
        }
        biomeGradientTexture.Apply(); // Apply pixel changes to the texture
    }

    // Clean up the generated texture when the object is destroyed
    void OnDestroy()
    {
        if (biomeGradientTexture != null)
        {
            DestroyImmediate(biomeGradientTexture);
            biomeGradientTexture = null;
        }
    }
}