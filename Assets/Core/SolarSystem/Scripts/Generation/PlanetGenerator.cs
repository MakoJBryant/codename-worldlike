using UnityEngine;
using MakoJBryant.SolarSystem.Generation;

[ExecuteInEditMode]
public class PlanetGenerator : MonoBehaviour
{
    [Range(2, 256)] public int resolution = 64;
    public float radius = 1f;

    [Range(0f, 1f), Tooltip("Controls ocean height between min and max elevation.")]
    public float seaLevel = 0.5f;

    public Light sceneSunLight;

    [Header("Settings Assets")]
    public ShapeSettings shapeSettings;
    public ColorSettings colorSettings;

    // Range updated to 0.5f to 1.5f as requested.
    // Be cautious with values below 1.0 as the atmosphere mesh will be smaller than the planet's highest points.
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
    private float maxElevation;
    private Texture2D biomeTexture;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>() ?? gameObject.AddComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>() ?? gameObject.AddComponent<MeshCollider>();

        if (mesh == null)
            mesh = new Mesh { name = "Generated Planet Mesh" };

        meshFilter.sharedMesh = mesh;
        GeneratePlanet();
    }

    void OnValidate()
    {
        if (mesh == null)
            mesh = new Mesh { name = "Generated Planet Mesh" };

        if (colorSettings != null)
            UpdateBiomeTexture();
    }

    [ContextMenu("Generate Planet Now")]
    public void GeneratePlanet()
    {
        if (shapeSettings == null || colorSettings == null)
        {
            Debug.LogError("Missing ShapeSettings or ColorSettings");
            return;
        }

        mesh.Clear();
        SphereCreator.CreateSphereMesh(resolution, radius, out Vector3[] vertices, out int[] triangles, out Vector2[] uvs);

        minElevation = float.MaxValue;
        maxElevation = float.MinValue;

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

            vertices[i] = normal * radius * (1 + totalDisplacement + shapeSettings.globalHeightOffset);
            float height = vertices[i].magnitude;
            minElevation = Mathf.Min(minElevation, height);
            maxElevation = Mathf.Max(maxElevation, height);
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

        if (meshRenderer != null && colorSettings.planetMaterial != null)
        {
            meshRenderer.sharedMaterial = colorSettings.planetMaterial;

            // ✅ Remove green tint in fallback
            meshRenderer.sharedMaterial.color = Color.white;

            meshRenderer.sharedMaterial.SetFloat("_Radius", radius);
            meshRenderer.sharedMaterial.SetFloat("_MinHeight", minElevation);
            meshRenderer.sharedMaterial.SetFloat("_MaxHeight", maxElevation);
            meshRenderer.sharedMaterial.SetColor("_OceanColor", colorSettings.oceanColor);

            UpdateBiomeTexture();

            if (biomeTexture != null)
                meshRenderer.sharedMaterial.SetTexture("_BiomeTexture", biomeTexture);
        }

        meshCollider.sharedMesh = mesh;

        GenerateOceanPlane();
        GenerateAtmospherePlane();
    }

    void GenerateOceanPlane()
    {
        foreach (Transform child in transform)
        {
            if (child.name == "Ocean" && child.gameObject != oceanGameObject)
                DestroyImmediate(child.gameObject);
        }

        if (oceanGameObject == null)
        {
            oceanGameObject = new GameObject("Ocean");
            oceanGameObject.transform.parent = transform;
            oceanGameObject.transform.localPosition = Vector3.zero;
            oceanGameObject.transform.localRotation = Quaternion.identity;
            oceanGameObject.transform.localScale = Vector3.one;
            oceanMeshFilter = oceanGameObject.AddComponent<MeshFilter>();
            oceanMeshRenderer = oceanGameObject.AddComponent<MeshRenderer>();
            oceanMesh = new Mesh { name = "Generated Ocean Mesh" };
            oceanMeshFilter.sharedMesh = oceanMesh;
        }

        oceanMesh.Clear();

        float oceanRadius = Mathf.Lerp(minElevation, maxElevation * 0.999f, seaLevel);

        SphereCreator.CreateSphereMesh(resolution, oceanRadius, out Vector3[] v, out int[] t, out Vector2[] uv);

        oceanMesh.vertices = v;
        oceanMesh.triangles = t;
        oceanMesh.uv = uv;
        oceanMesh.RecalculateNormals();
        oceanMesh.RecalculateBounds();

        if (colorSettings.oceanMaterial != null)
        {
            oceanMeshRenderer.sharedMaterial = colorSettings.oceanMaterial;
            oceanMeshRenderer.sharedMaterial.SetFloat("_Radius", oceanRadius);
            oceanMeshRenderer.sharedMaterial.SetColor("_Color", colorSettings.oceanColor);
        }
    }

    void GenerateAtmospherePlane()
    {
        foreach (Transform child in transform)
        {
            if (child.name == "Atmosphere" && child.gameObject != atmosphereGameObject)
                DestroyImmediate(child.gameObject);
        }

        if (atmosphereGameObject == null)
        {
            atmosphereGameObject = new GameObject("Atmosphere");
            atmosphereGameObject.transform.parent = transform;
            atmosphereGameObject.transform.localPosition = Vector3.zero;
            atmosphereGameObject.transform.localRotation = Quaternion.identity;
            atmosphereGameObject.transform.localScale = Vector3.one;
            atmosphereMeshFilter = atmosphereGameObject.AddComponent<MeshFilter>();
            atmosphereMeshRenderer = atmosphereGameObject.AddComponent<MeshRenderer>();
            atmosphereController = atmosphereGameObject.AddComponent<AtmosphereController>();
            atmosphereMesh = new Mesh { name = "Generated Atmosphere Mesh" };
            atmosphereMeshFilter.sharedMesh = atmosphereMesh;
        }

        atmosphereMesh.Clear();
        float atmosphereRadius = maxElevation * atmosphereExpansionFactor;

        SphereCreator.CreateSphereMesh(resolution, atmosphereRadius, out Vector3[] v, out int[] t, out Vector2[] uv);

        atmosphereMesh.vertices = v;
        atmosphereMesh.triangles = t;
        atmosphereMesh.uv = uv;
        atmosphereMesh.RecalculateNormals();
        atmosphereMesh.RecalculateBounds();

        if (colorSettings.atmosphereMaterial != null)
        {
            atmosphereMeshRenderer.sharedMaterial = colorSettings.atmosphereMaterial;
            if (sceneSunLight == null)
                sceneSunLight = GameObject.FindFirstObjectByType<Light>();

            atmosphereController.sunLight = sceneSunLight;
            atmosphereController.atmosphereMaterial = colorSettings.atmosphereMaterial;
            atmosphereController.atmosphereRadius = atmosphereRadius;
            atmosphereController.atmosphereColor = colorSettings.atmosphereColor;
            atmosphereController.density = colorSettings.atmosphereDensity;
            atmosphereController.power = colorSettings.atmospherePower;
            atmosphereController.ambientLightInfluence = colorSettings.atmosphereAmbientLightInfluence;
            atmosphereController.rimPower = colorSettings.atmosphereRimPower;
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
        if (oceanGameObject != null) DestroyImmediate(oceanGameObject);
        if (atmosphereGameObject != null) DestroyImmediate(atmosphereGameObject);
    }
}