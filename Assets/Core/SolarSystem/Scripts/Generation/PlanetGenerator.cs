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

    // NEW: Public field for the scene's main directional light (now optional for manual assignment)
    [Tooltip("Assign your scene's main Directional Light (Sun) here. If left unassigned, the script will try to find one named 'Directional Light'.")]
    public Light sceneSunLight;

    // References to the new ScriptableObject assets
    [Header("Settings Assets")]
    public ShapeSettings shapeSettings; // Reference to the ShapeSettings ScriptableObject
    public ColorSettings colorSettings; // Reference to the ColorSettings ScriptableObject

    // --- Private References (Unity Components) ---
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private Mesh mesh;

    // --- Ocean Plane References ---
    private GameObject oceanGameObject;
    private MeshFilter oceanMeshFilter;
    private MeshRenderer oceanMeshRenderer;
    private Mesh oceanMesh;

    // --- Atmosphere Plane References ---
    private GameObject atmosphereGameObject;
    private MeshFilter atmosphereMeshFilter;
    private MeshRenderer atmosphereMeshRenderer;
    private Mesh atmosphereMesh;
    private AtmosphereController atmosphereController; // Reference to your existing AtmosphereController
    // --- END NEW ---

    // To store the actual min/max elevation (distance from planet center) for shader
    private float minElevation;
    private float maxElevation;

    // NEW: Texture to store biome gradient data
    private Texture2D biomeTexture;

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
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "Generated Planet Mesh";
        }

        // Only update biome texture if ColorSettings is assigned
        if (colorSettings != null)
        {
            UpdateBiomeTexture();
        }
    }

    // --- Context Menu allows right-clicking the component in the Inspector to trigger a method ---
    [ContextMenu("Generate Planet Now")]
    public void GeneratePlanet()
    {
        // Basic validation for settings assets
        if (shapeSettings == null)
        {
            Debug.LogError("Shape Settings asset is not assigned to PlanetGenerator!");
            return;
        }
        if (colorSettings == null)
        {
            Debug.LogError("Color Settings asset is not assigned to PlanetGenerator!");
            return;
        }

        Debug.Log("Generating Planet with Resolution: " + resolution + ", Radius: " + radius);

        // --- DEBUG LOGS TO VERIFY INPUTS FROM SCRIPTABLEOBJECTS ---
        Debug.Log($"DEBUG: globalHeightOffset read from ShapeSettings: {shapeSettings.globalHeightOffset}");
        if (shapeSettings.noiseLayers != null && shapeSettings.noiseLayers.Length > 0)
        {
            Debug.Log($"DEBUG: NoiseLayer[0] Strength read from ShapeSettings: {shapeSettings.noiseLayers[0].strength}");
            Debug.Log($"DEBUG: NoiseLayer[0] MinValue read from ShapeSettings: {shapeSettings.noiseLayers[0].minValue}");
        }
        else
        {
            Debug.LogWarning("DEBUG: NoiseLayers array in ShapeSettings is null or empty. Cannot log specific noise layer values.");
        }
        // --- END NEW DEBUG LOGS ---

        // Ensure mesh and meshFilter are initialized.
        if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null) { Debug.LogError("MeshFilter not found!"); return; }

        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "Generated Planet Mesh";
            meshFilter.sharedMesh = mesh;
        }

        // Clear any previous mesh data
        mesh.Clear();

        // Step 1: Get the base spherical mesh data from SphereCreator
        Vector3[] vertices;
        int[] triangles;
        Vector2[] uvs;
        SphereCreator.CreateSphereMesh(resolution, radius, out vertices, out triangles, out uvs);

        // Initialize min/max elevation before calculating displacement
        minElevation = float.MaxValue;
        maxElevation = float.MinValue;

        // Step 2: Apply MULTI-LAYERED FRACTAL BROWNIAN MOTION (FBM) noise to displace vertices
        int logCount = 0;
        int logMax = 10; // Log for the first 10 vertices

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertex = vertices[i];
            Vector3 normalDirection = vertex.normalized;

            float totalDisplacement = 0;
            float firstLayerValue = 0;

            // Iterate through each defined NoiseLayer from ShapeSettings
            foreach (NoiseLayer noiseLayer in shapeSettings.noiseLayers)
            {
                if (!noiseLayer.enabled)
                {
                    continue;
                }

                float currentLayerNoise = 0;
                float currentFrequency = noiseLayer.roughness;
                float currentAmplitude = 1;
                float totalLayerAmplitude = 0;

                for (int j = 0; j < noiseLayer.octaves; j++)
                {
                    Vector3 samplePoint = (normalDirection + noiseLayer.offset) * currentFrequency;
                    float v = PerlinNoise3D.GenerateNoise(samplePoint.x, samplePoint.y, samplePoint.z);

                    if (noiseLayer.noiseType == NoiseType.Ridge)
                    {
                        v = 1 - Mathf.Abs(v * 2 - 1);
                    }
                    else
                    {
                        v = v * 2 - 1;
                    }

                    currentLayerNoise += v * currentAmplitude;
                    totalLayerAmplitude += currentAmplitude;
                    currentAmplitude *= noiseLayer.persistence;
                    currentFrequency *= noiseLayer.lacunarity;
                }

                float normalizedLayerNoise = (totalLayerAmplitude == 0) ? 0 : currentLayerNoise / totalLayerAmplitude;
                float finalLayerNoise = normalizedLayerNoise + noiseLayer.minValue;

                if (noiseLayer.useFirstLayerAsMask)
                {
                    firstLayerValue = finalLayerNoise;
                }

                if (noiseLayer.useFirstLayerAsMask && firstLayerValue <= 0)
                {
                    finalLayerNoise = 0;
                }

                totalDisplacement += finalLayerNoise * noiseLayer.strength;
            }

            // Apply globalHeightOffset and totalDisplacement as a scaling factor to radius
            vertices[i] = normalDirection * radius * (1 + totalDisplacement + shapeSettings.globalHeightOffset);

            float currentVertexHeight = vertices[i].magnitude;

            // --- NEW DETAILED DEBUG LOGS FOR VERTEX HEIGHTS ---
            if (logCount < logMax)
            {
                Debug.Log($"DEBUG VERTEX {i}: Original Radius Vertex Magnitude: {vertex.magnitude}");
                Debug.Log($"DEBUG VERTEX {i}: Total Displacement (from NoiseLayers): {totalDisplacement}");
                Debug.Log($"DEBUG VERTEX {i}: Global Height Offset (from ShapeSettings): {shapeSettings.globalHeightOffset}");
                Debug.Log($"DEBUG VERTEX {i}: Final Vertex Height (magnitude): {currentVertexHeight}");
                logCount++;
            }
            // --- END NEW DETAILED DEBUG LOGS ---

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
        mesh.uv = uvs;

        mesh.RecalculateBounds();

        // --- CUSTOM NORMAL CALCULATION ---
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

        meshFilter.sharedMesh = mesh;

        // Assign the material and pass elevation data and biome texture to it
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null && colorSettings != null && colorSettings.planetMaterial != null)
        {
            meshRenderer.sharedMaterial = colorSettings.planetMaterial;

            // Pass the absolute heights and radius to the shader
            meshRenderer.sharedMaterial.SetFloat("_Radius", radius);
            meshRenderer.sharedMaterial.SetFloat("_MinHeight", minElevation);
            meshRenderer.sharedMaterial.SetFloat("_MaxHeight", maxElevation);
            meshRenderer.sharedMaterial.SetColor("_OceanColor", colorSettings.oceanColor);

            UpdateBiomeTexture();
            if (biomeTexture != null)
            {
                meshRenderer.sharedMaterial.SetTexture("_BiomeTexture", biomeTexture);
            }
            else
            {
                Debug.LogWarning("Biome Texture could not be generated for " + gameObject.name + "!");
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

        // --- Ocean Plane Generation ---
        GenerateOceanPlane();
        // --- Atmosphere Plane Generation ---
        GenerateAtmospherePlane();
        // --- END Atmosphere Plane Generation ---

        Debug.Log($"PlanetGenerator: Mesh assigned. Vertices: {mesh.vertexCount}, Triangles: {mesh.triangles.Length / 3}");
    }

    /// <summary>
    /// Generates or updates a Texture2D that encodes biome colors and their start heights.
    /// This texture is then passed to the shader for biome blending.
    /// </summary>
    void UpdateBiomeTexture()
    {
        if (colorSettings == null || colorSettings.biomes == null || colorSettings.biomes.Length == 0)
        {
            if (biomeTexture != null)
            {
                DestroyImmediate(biomeTexture); // Clean up old texture
                biomeTexture = null;
            }
            return;
        }

        int textureResolution = 256;

        // Corrected: Use biomeTexture.width for comparison
        if (biomeTexture == null || biomeTexture.width != textureResolution)
        {
            if (biomeTexture != null) DestroyImmediate(biomeTexture);
            biomeTexture = new Texture2D(textureResolution, 1, TextureFormat.RGBA32, false);
            biomeTexture.filterMode = FilterMode.Bilinear;
            biomeTexture.wrapMode = TextureWrapMode.Clamp;
        }

        Color[] pixels = new Color[textureResolution];

        System.Array.Sort(colorSettings.biomes, (b1, b2) => b1.startHeight.CompareTo(b2.startHeight));

        for (int i = 0; i < textureResolution; i++)
        {
            float normalizedHeight = (float)i / (textureResolution - 1);
            Color finalColor = colorSettings.oceanColor;

            for (int b = 0; b < colorSettings.biomes.Length; b++)
            {
                ColorSettings.Biome biome = colorSettings.biomes[b];
                float startHeight = biome.startHeight;
                float blendAmount = biome.blendAmount;

                float blendFactor = Mathf.Clamp01((normalizedHeight - startHeight) / blendAmount);
                finalColor = Color.Lerp(finalColor, biome.color, blendFactor);
            }
            pixels[i] = finalColor;
        }

        biomeTexture.SetPixels(pixels);
        biomeTexture.Apply();
    }

    /// <summary>
    /// Creates or updates a separate GameObject and mesh for the ocean plane.
    /// </summary>
    void GenerateOceanPlane()
    {
        // Find or create the ocean GameObject as a child of this planet
        // Check if oceanGameObject exists and is still valid (not destroyed externally)
        if (oceanGameObject == null)
        {
            // Try to find an existing child named "Ocean"
            Transform existingOceanTransform = transform.Find("Ocean");
            if (existingOceanTransform != null)
            {
                oceanGameObject = existingOceanTransform.gameObject;
                oceanMeshFilter = oceanGameObject.GetComponent<MeshFilter>();
                oceanMeshRenderer = oceanGameObject.GetComponent<MeshRenderer>();
                oceanMesh = oceanMeshFilter.sharedMesh;
                // Ensure AtmosphereController is also present if reusing
                atmosphereController = oceanGameObject.GetComponent<AtmosphereController>(); // This line is incorrect, should be on Atmosphere object
            }
            else
            {
                oceanGameObject = new GameObject("Ocean");
                oceanGameObject.transform.parent = transform; // Make it a child of the planet
                oceanGameObject.transform.localPosition = Vector3.zero; // Center it on the planet
                oceanGameObject.transform.localRotation = Quaternion.identity;
                oceanGameObject.transform.localScale = Vector3.one;

                oceanMeshFilter = oceanGameObject.AddComponent<MeshFilter>();
                oceanMeshRenderer = oceanGameObject.AddComponent<MeshRenderer>();
                oceanMesh = new Mesh();
                oceanMesh.name = "Generated Ocean Mesh";
                oceanMeshFilter.sharedMesh = oceanMesh;
            }
        }

        // Clear any previous ocean mesh data
        oceanMesh.Clear();

        // Generate a simple sphere mesh for the ocean
        Vector3[] oceanVertices;
        int[] oceanTriangles;
        Vector2[] oceanUVs;
        SphereCreator.CreateSphereMesh(resolution, radius, out oceanVertices, out oceanTriangles, out oceanUVs);

        // Assign ocean mesh data
        oceanMesh.vertices = oceanVertices;
        oceanMesh.triangles = oceanTriangles;
        oceanMesh.uv = oceanUVs;
        oceanMesh.RecalculateNormals(); // Simple normals are fine for a sphere
        oceanMesh.RecalculateBounds();

        // Assign the ocean material
        if (colorSettings != null && colorSettings.oceanMaterial != null) // Check for oceanMaterial now
        {
            oceanMeshRenderer.sharedMaterial = colorSettings.oceanMaterial; // Assign the dedicated ocean material

            // Pass the radius to the ocean shader so it knows its scale
            oceanMeshRenderer.sharedMaterial.SetFloat("_Radius", radius);
            // Pass the ocean color from ColorSettings to the ocean material's _Color property
            oceanMeshRenderer.sharedMaterial.SetColor("_Color", colorSettings.oceanColor);
        }
        else
        {
            Debug.LogWarning("Ocean Material not assigned in Color Settings for " + gameObject.name + "!");
        }
    }

    /// <summary>
    /// NEW: Creates or updates a separate GameObject and mesh for the atmosphere.
    /// </summary>
    void GenerateAtmospherePlane()
    {
        // Find or create the atmosphere GameObject as a child of this planet
        // Check if atmosphereGameObject exists and is still valid
        if (atmosphereGameObject == null)
        {
            // Try to find an existing child named "Atmosphere"
            Transform existingAtmosphereTransform = transform.Find("Atmosphere");
            if (existingAtmosphereTransform != null)
            {
                atmosphereGameObject = existingAtmosphereTransform.gameObject;
                atmosphereMeshFilter = atmosphereGameObject.GetComponent<MeshFilter>();
                atmosphereMeshRenderer = atmosphereGameObject.GetComponent<MeshRenderer>();
                atmosphereMesh = atmosphereMeshFilter.sharedMesh;
                atmosphereController = atmosphereGameObject.GetComponent<AtmosphereController>();
            }
            else
            {
                atmosphereGameObject = new GameObject("Atmosphere");
                atmosphereGameObject.transform.parent = transform; // Make it a child of the planet
                atmosphereGameObject.transform.localPosition = Vector3.zero; // Center it on the planet
                atmosphereGameObject.transform.localRotation = Quaternion.identity;
                atmosphereGameObject.transform.localScale = Vector3.one;

                atmosphereMeshFilter = atmosphereGameObject.AddComponent<MeshFilter>();
                atmosphereMeshRenderer = atmosphereGameObject.AddComponent<MeshRenderer>();
                atmosphereMesh = new Mesh();
                atmosphereMesh.name = "Generated Atmosphere Mesh";
                atmosphereMeshFilter.sharedMesh = atmosphereMesh;
                // NEW: Add the AtmosphereController component
                atmosphereController = atmosphereGameObject.AddComponent<AtmosphereController>();
            }
        }

        // Clear any previous atmosphere mesh data
        atmosphereMesh.Clear();

        // Generate a simple sphere mesh for the atmosphere
        Vector3[] atmosphereVertices;
        int[] atmosphereTriangles;
        Vector2[] atmosphereUVs;
        // The atmosphere should be a perfect sphere, slightly larger than the planet's radius.
        // We'll use a slightly larger radius for the atmosphere to ensure it encompasses the planet.
        float atmosphereRadius = radius * 1.05f; // 5% larger than the planet's radius for now.
        SphereCreator.CreateSphereMesh(resolution, atmosphereRadius, out atmosphereVertices, out atmosphereTriangles, out atmosphereUVs);

        // Assign atmosphere mesh data
        atmosphereMesh.vertices = atmosphereVertices;
        atmosphereMesh.triangles = atmosphereTriangles;
        atmosphereMesh.uv = atmosphereUVs;
        atmosphereMesh.RecalculateNormals(); // Simple normals are fine for a sphere
        atmosphereMesh.RecalculateBounds();

        // Assign the atmosphere material
        if (colorSettings != null && colorSettings.atmosphereMaterial != null) // Check for atmosphereMaterial now
        {
            atmosphereMeshRenderer.sharedMaterial = colorSettings.atmosphereMaterial; // Assign the dedicated atmosphere material

            // Pass the atmosphere radius to the shader via the AtmosphereController
            // The AtmosphereController will handle setting _SunDirection and potentially _AtmosphereRadius
            // So, we just need to ensure its material reference is set.
            if (atmosphereController != null)
            {
                atmosphereController.atmosphereMaterial = colorSettings.atmosphereMaterial;
                // NEW: Pass the scene's main directional light to the AtmosphereController
                // If sceneSunLight is null, try to find it programmatically.
                if (sceneSunLight == null)
                {
                    GameObject sunGO = GameObject.Find("Directional Light"); // Common default name for Unity's directional light
                    if (sunGO != null)
                    {
                        sceneSunLight = sunGO.GetComponent<Light>();
                        if (sceneSunLight == null)
                        {
                            Debug.LogWarning("Directional Light GameObject found but no Light component on it!");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Directional Light GameObject not found in scene for automatic assignment!");
                    }
                }
                atmosphereController.sunLight = sceneSunLight; // Assign the found/assigned light
                // If your S_Atmosphere shader graph uses a _Radius property, you'd pass it here.
                // For now, we assume AtmosphereController or the shader itself handles its scale.
                // If you want the shader to know the planet's base radius, you could pass it:
                // atmosphereMeshRenderer.sharedMaterial.SetFloat("_PlanetRadius", radius);
            }
        }
        else
        {
            Debug.LogWarning("Atmosphere Material not assigned in Color Settings for " + gameObject.name + "!");
        }
    }

    // Clean up the generated texture and ocean/atmosphere GameObjects when the object is destroyed
    void OnDestroy()
    {
        if (biomeTexture != null)
        {
            DestroyImmediate(biomeTexture);
            biomeTexture = null;
        }
        if (oceanGameObject != null)
        {
            DestroyImmediate(oceanGameObject); // Destroy the ocean child GameObject
            oceanGameObject = null;
        }
        // NEW: Destroy atmosphere GameObject
        if (atmosphereGameObject != null)
        {
            DestroyImmediate(atmosphereGameObject);
            atmosphereGameObject = null;
        }
    }
}
