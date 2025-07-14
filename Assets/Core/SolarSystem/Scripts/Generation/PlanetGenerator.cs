using UnityEngine;
using MakoJBryant.SolarSystem.Generation;

// Ensure these core components are always present on the GameObject
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
// Prevent multiple instances of this script on the same GameObject
[DisallowMultipleComponent]
[ExecuteInEditMode]
public class PlanetGenerator : MonoBehaviour
{
    [Range(2, 256)] public int resolution = 64;
    public float radius = 1f;

    [Range(0f, 1f), Tooltip("Controls ocean height between min and max elevation.")]
    public float seaLevel = 0.5f;

    public Light sceneSunLight;

    [Header("Settings Assets")]
    [Tooltip("Assign your ShapeSettings ScriptableObject here.")]
    public ShapeSettings shapeSettings;
    [Tooltip("Assign your ColorSettings ScriptableObject here.")]
    public ColorSettings colorSettings;

    [Range(0.5f, 1.5f), Tooltip("Factor by which the atmosphere radius expands/contracts relative to the planet's max elevation.")]
    public float atmosphereExpansionFactor = 1.02f;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private Mesh mesh;

    private GameObject oceanGameObject;
    private MeshFilter oceanMeshFilter;
    private MeshRenderer oceanMeshRenderer;
    private Mesh oceanMesh;

    private GameObject atmosphereGameObject;
    private MeshFilter atmosphereMeshFilter;
    private MeshRenderer atmosphereMeshRenderer;
    private Mesh atmosphereMesh;
    private AtmosphereController atmosphereController;

    private float minElevation;
    private float maxElevation; // Corrected variable name from floatmaxElevation
    private Texture2D biomeTexture;

    void Awake()
    {
        // Get existing components. RequireComponent ensures they are there.
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();

        // Added a specific check for MeshCollider in Awake
        if (meshCollider == null)
        {
            Debug.LogError("PlanetGenerator: MeshCollider component not found on this GameObject. Please ensure it has a MeshCollider, not a SphereCollider or other collider type.", this);
        }

        if (mesh == null)
            mesh = new Mesh { name = "Generated Planet Mesh" };

        meshFilter.sharedMesh = mesh;
        GeneratePlanet();
    }

    void OnValidate()
    {
        // OnValidate is called when a script is loaded or a value is changed in the Inspector.
        // We'll use this to regenerate the planet when settings change.
        if (mesh == null)
            mesh = new Mesh { name = "Generated Planet Mesh" };

        // Only attempt to update if color settings are assigned
        if (colorSettings != null)
            UpdateBiomeTexture();

        // ONLY generate the planet if BOTH shape and color settings are assigned.
        // This prevents NullReferenceExceptions if settings are temporarily unassigned in editor.
        if (shapeSettings != null && colorSettings != null)
        {
            GeneratePlanet();
        }
        else
        {
            Debug.LogWarning("PlanetGenerator: ShapeSettings or ColorSettings are missing. Planet generation skipped in OnValidate.", this);
        }
    }

    [ContextMenu("Generate Planet Now")]
    public void GeneratePlanet()
    {
        Debug.Log($"Generating Planet '{gameObject.name}' at world position: {transform.position}", this);

        // Explicitly check for null settings before proceeding with any generation logic
        if (shapeSettings == null)
        {
            Debug.LogError("Missing ShapeSettings. Cannot generate planet.", this);
            return;
        }
        if (colorSettings == null)
        {
            Debug.LogError("Missing ColorSettings. Cannot generate planet.", this);
            return;
        }

        mesh.Clear();
        // IMPORTANT: SphereCreator.CreateSphereMesh must generate vertices in LOCAL SPACE (around 0,0,0)
        // It must NOT add transform.position or any world-space offsets.
        SphereCreator.CreateSphereMesh(resolution, radius, out Vector3[] vertices, out int[] triangles, out Vector2[] uvs);

        minElevation = float.MaxValue;
        maxElevation = float.MinValue; // Corrected variable name

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 normal = vertices[i].normalized;
            float totalDisplacement = 0;
            float firstLayerValue = 0;

            foreach (NoiseLayer layer in shapeSettings.noiseLayers)
            {
                if (!layer.enabled) continue;

                float noiseSum = 0;
                float frequency = layer.roughness;
                float amplitude = 1;
                float amplitudeSum = 0;

                for (int j = 0; j < layer.octaves; j++)
                {
                    Vector3 samplePoint = (normal + layer.offset) * frequency;
                    float value = PerlinNoise3D.GenerateNoise(samplePoint.x, samplePoint.y, samplePoint.z);
                    value = layer.noiseType == NoiseType.Ridge ? 1 - Mathf.Abs(value * 2 - 1) : value * 2 - 1;
                    noiseSum += value * amplitude;
                    amplitudeSum += amplitude;
                    amplitude *= layer.persistence;
                    frequency *= layer.lacunarity;
                }

                float finalNoise = (amplitudeSum == 0 ? 0 : noiseSum / amplitudeSum) + layer.minValue;
                if (layer.useFirstLayerAsMask) firstLayerValue = finalNoise;
                if (layer.useFirstLayerAsMask && firstLayerValue <= 0) finalNoise = 0;
                totalDisplacement += finalNoise * layer.strength;
            }

            // Vertices are calculated here in local space relative to the mesh's origin (0,0,0)
            vertices[i] = normal * radius * (1 + totalDisplacement + shapeSettings.globalHeightOffset);
            float height = vertices[i].magnitude;
            minElevation = Mathf.Min(minElevation, height);
            maxElevation = Mathf.Max(maxElevation, height); // Corrected variable name
        }

        Vector3[] normals = new Vector3[vertices.Length];
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
            Vector3 normal = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]).normalized;
            normals[a] += normal;
            normals[b] += normal;
            normals[c] += normal;
        }

        for (int i = 0; i < normals.Length; i++) normals[i].Normalize();

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.normals = normals;
        mesh.RecalculateBounds();

        // Ensure meshRenderer and planetMaterial are not null before assigning
        if (meshRenderer != null && colorSettings.planetMaterial != null)
        {
            meshRenderer.sharedMaterial = colorSettings.planetMaterial;

            // Remove green tint in fallback
            meshRenderer.sharedMaterial.color = Color.white;

            // These properties are typically relative to the object's local space
            meshRenderer.sharedMaterial.SetFloat("_Radius", radius);
            meshRenderer.sharedMaterial.SetFloat("_MinHeight", minElevation);
            meshRenderer.sharedMaterial.SetFloat("_MaxHeight", maxElevation); // Corrected variable name
            meshRenderer.sharedMaterial.SetColor("_OceanColor", colorSettings.oceanColor);

            UpdateBiomeTexture();

            if (biomeTexture != null)
                meshRenderer.sharedMaterial.SetTexture("_BiomeTexture", biomeTexture);
        }
        else if (meshRenderer == null)
        {
            Debug.LogError("PlanetGenerator: MeshRenderer is null, cannot apply planet material. Ensure it's on the GameObject.", this);
        }
        else if (colorSettings.planetMaterial == null)
        {
            Debug.LogError("PlanetGenerator: colorSettings.planetMaterial is null, cannot apply to planet mesh. Assign it in ColorSettings asset.", this);
        }


        if (meshCollider != null) // This check is now crucial
        {
            meshCollider.sharedMesh = mesh;
        }
        else
        {
            Debug.LogError("PlanetGenerator: MeshCollider is null, cannot assign mesh. Ensure it's on the GameObject and is a MeshCollider, not a SphereCollider.", this);
            return; // Added return to prevent further null reference errors if collider is missing
        }


        GenerateOceanPlane();
        GenerateAtmospherePlane();
    }

    void GenerateOceanPlane()
    {
        // Destroy any existing ocean GameObject that is a child of this transform and named "Ocean"
        // but is not the current oceanGameObject reference. This handles regeneration.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name == "Ocean" && child.gameObject != oceanGameObject)
            {
                DestroyImmediate(child.gameObject);
            }
        }

        if (oceanGameObject == null)
        {
            oceanGameObject = new GameObject("Ocean");
            oceanGameObject.transform.parent = transform; // Set parent: Makes "Ocean" a child of the planet
            // Ensure local position is zero relative to the parent
            oceanGameObject.transform.localPosition = Vector3.zero;
            oceanGameObject.transform.localRotation = Quaternion.identity;
            oceanGameObject.transform.localScale = Vector3.one;
            oceanMeshFilter = oceanGameObject.AddComponent<MeshFilter>();
            oceanMeshRenderer = oceanGameObject.AddComponent<MeshRenderer>();
            oceanMesh = new Mesh { name = "Generated Ocean Mesh" };
            oceanMeshFilter.sharedMesh = oceanMesh;
        }
        else
        {
            // If oceanGameObject already exists, just ensure its parent and local position are correct
            oceanGameObject.transform.parent = transform;
            oceanGameObject.transform.localPosition = Vector3.zero;
        }

        oceanMesh.Clear();

        float oceanRadius = Mathf.Lerp(minElevation, maxElevation * 0.999f, seaLevel); // Corrected variable name

        // IMPORTANT: SphereCreator.CreateSphereMesh must generate vertices in LOCAL SPACE (around 0,0,0)
        // for the ocean mesh as well.
        SphereCreator.CreateSphereMesh(resolution, oceanRadius, out Vector3[] v, out int[] t, out Vector2[] uv);

        oceanMesh.vertices = v;
        oceanMesh.triangles = t;
        oceanMesh.uv = uv;
        oceanMesh.RecalculateNormals();
        oceanMesh.RecalculateBounds();

        // Ensure oceanMeshRenderer and oceanMaterial are not null before assigning
        if (oceanMeshRenderer != null && colorSettings.oceanMaterial != null)
        {
            oceanMeshRenderer.sharedMaterial = colorSettings.oceanMaterial;
            // _Radius is probably fine as it's a size parameter, not a position.
            oceanMeshRenderer.sharedMaterial.SetFloat("_Radius", oceanRadius);
            oceanMeshRenderer.sharedMaterial.SetColor("_Color", colorSettings.oceanColor);

            // If your ocean shader relies on the planet's world position,
            // you might need to pass transform.position here too, similar to atmosphere.
            // oceanMeshRenderer.sharedMaterial.SetVector("_PlanetCenter", transform.position);
        }
        else if (oceanMeshRenderer == null)
        {
            Debug.LogError("PlanetGenerator: OceanMeshRenderer is null, cannot apply ocean material. Ensure it's on the GameObject.", this);
        }
        else if (colorSettings.oceanMaterial == null)
        {
            Debug.LogError("PlanetGenerator: colorSettings.oceanMaterial is null, cannot apply to ocean mesh. Assign it in ColorSettings asset.", this);
        }
    }

    void GenerateAtmospherePlane()
    {
        // Destroy any existing atmosphere GameObject that is a child of this transform and named "Atmosphere"
        // but is not the current atmosphereGameObject reference. This handles regeneration.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name == "Atmosphere" && child.gameObject != atmosphereGameObject)
            {
                DestroyImmediate(child.gameObject);
            }
        }

        if (atmosphereGameObject == null)
        {
            atmosphereGameObject = new GameObject("Atmosphere");
            atmosphereGameObject.transform.parent = transform; // Set parent
            // Ensure local position is zero relative to the parent
            atmosphereGameObject.transform.localPosition = Vector3.zero;
            atmosphereGameObject.transform.localRotation = Quaternion.identity;
            atmosphereGameObject.transform.localScale = Vector3.one;
            atmosphereMeshFilter = atmosphereGameObject.AddComponent<MeshFilter>();
            atmosphereMeshRenderer = atmosphereGameObject.AddComponent<MeshRenderer>();
            atmosphereController = atmosphereGameObject.AddComponent<AtmosphereController>(); // This line is critical
            atmosphereMesh = new Mesh { name = "Generated Atmosphere Mesh" };
            atmosphereMeshFilter.sharedMesh = atmosphereMesh;
        }
        else
        {
            // If atmosphereGameObject already exists, just ensure its parent and local position are correct
            atmosphereGameObject.transform.parent = transform;
            atmosphereGameObject.transform.localPosition = Vector3.zero;
            // Also ensure atmosphereController is still valid if the GameObject was just reused
            atmosphereController = atmosphereGameObject.GetComponent<AtmosphereController>();
        }

        // IMPORTANT: Check if atmosphereController is null AFTER trying to add/get it.
        if (atmosphereController == null)
        {
            Debug.LogError("PlanetGenerator: AtmosphereController component is missing or failed to add to Atmosphere GameObject. Cannot configure atmosphere. Check if AtmosphereController.cs has compile errors.", this);
            return; // Exit if controller is not available
        }


        atmosphereMesh.Clear();
        float atmosphereRadius = maxElevation * atmosphereExpansionFactor; // Corrected variable name

        // IMPORTANT: SphereCreator.CreateSphereMesh must generate vertices in LOCAL SPACE (around 0,0,0)
        // for the atmosphere mesh as well.
        SphereCreator.CreateSphereMesh(resolution, atmosphereRadius, out Vector3[] v, out int[] t, out Vector2[] uv);

        atmosphereMesh.vertices = v;
        atmosphereMesh.triangles = t;
        atmosphereMesh.uv = uv;
        atmosphereMesh.RecalculateNormals();
        atmosphereMesh.RecalculateBounds();

        // --- ADDED DEBUGGING CODE ---
        Debug.Log($"Atmosphere Mesh Vertices for '{atmosphereGameObject.name}' (first 5):");
        for (int i = 0; i < Mathf.Min(5, v.Length); i++)
        {
            Debug.Log($"  Vertex {i}: {v[i]}");
        }
        Debug.Log($"Atmosphere Mesh Bounds Center: {atmosphereMesh.bounds.center}");
        Debug.Log($"Atmosphere Mesh Bounds Extents: {atmosphereMesh.bounds.extents}");
        // --- END ADDED DEBUGGING CODE ---


        // Ensure atmosphereMeshRenderer and atmosphereMaterial are not null before assigning
        if (atmosphereMeshRenderer != null && colorSettings.atmosphereMaterial != null)
        {
            atmosphereMeshRenderer.sharedMaterial = colorSettings.atmosphereMaterial;

            // Check if sceneSunLight is assigned, otherwise try to find it
            if (sceneSunLight == null)
                sceneSunLight = GameObject.FindFirstObjectByType<Light>();

            // Assign properties to the atmosphereController
            atmosphereController.sunLight = sceneSunLight;
            atmosphereController.atmosphereMaterial = colorSettings.atmosphereMaterial;
            atmosphereController.atmosphereRadius = atmosphereRadius;
            atmosphereController.atmosphereColor = colorSettings.atmosphereColor;
            atmosphereController.density = colorSettings.atmosphereDensity;
            atmosphereController.power = colorSettings.atmospherePower;
            atmosphereController.ambientLightInfluence = colorSettings.atmosphereAmbientLightInfluence;
            atmosphereController.rimPower = colorSettings.atmosphereRimPower;
        }
        else if (atmosphereMeshRenderer == null)
        {
            Debug.LogError("PlanetGenerator: AtmosphereMeshRenderer is null, cannot apply atmosphere material. Ensure it's on the GameObject.", this);
        }
        else if (colorSettings.atmosphereMaterial == null)
        {
            Debug.LogError("PlanetGenerator: colorSettings.atmosphereMaterial is null, cannot apply to atmosphere mesh. Assign it in ColorSettings asset.", this);
        }
    }

    void UpdateBiomeTexture()
    {
        if (colorSettings.biomes == null || colorSettings.biomes.Length == 0) return;

        int texRes = 256;
        biomeTexture = new Texture2D(texRes, 1, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color[] pixels = new Color[texRes];
        System.Array.Sort(colorSettings.biomes, (a, b) => a.startHeight.CompareTo(b.startHeight));

        for (int i = 0; i < texRes; i++)
        {
            float h = i / (float)(texRes - 1);
            Color col = colorSettings.biomes[0].color;

            foreach (var biome in colorSettings.biomes)
            {
                float blend = Mathf.Clamp01((h - biome.startHeight) / biome.blendAmount);
                col = Color.Lerp(col, biome.color, blend);
            }

            pixels[i] = col;
        }

        biomeTexture.SetPixels(pixels);
        biomeTexture.Apply();
    }

    void OnDestroy()
    {
        if (biomeTexture != null) DestroyImmediate(biomeTexture);
        // Using `!= null` check for GameObject references that might have been destroyed by scene unload
        if (oceanGameObject) DestroyImmediate(oceanGameObject);
        if (atmosphereGameObject) DestroyImmediate(atmosphereGameObject);
    }
}
