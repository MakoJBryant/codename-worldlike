using UnityEngine;
using MakoJBryant.SolarSystem.Generation;

[ExecuteInEditMode]
public class PlanetGenerator : MonoBehaviour
{
    [Range(2, 256)]
    public int resolution = 64;
    public float radius = 1f;

    [Tooltip("Assign your scene's main Directional Light (Sun) here. If left unassigned, the script will try to find one tagged 'Sun' or the first Light in the scene.")]
    public Light sceneSunLight;

    [Header("Settings Assets")]
    public ShapeSettings shapeSettings;
    public ColorSettings colorSettings;

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

        mesh ??= new Mesh { name = "Generated Planet Mesh" };
        if (meshFilter.sharedMesh != mesh) meshFilter.sharedMesh = mesh;

        GeneratePlanet();
    }

    void OnValidate()
    {
        mesh ??= new Mesh { name = "Generated Planet Mesh" };
        if (colorSettings != null) UpdateBiomeTexture();
    }

    [ContextMenu("Generate Planet Now")]
    public void GeneratePlanet()
    {
        if (shapeSettings == null || colorSettings == null)
        {
            Debug.LogError("Missing ShapeSettings or ColorSettings asset.");
            return;
        }

        mesh ??= new Mesh { name = "Generated Planet Mesh" };
        mesh.Clear();

        SphereCreator.CreateSphereMesh(resolution, radius, out Vector3[] vertices, out int[] triangles, out Vector2[] uvs);

        minElevation = float.MaxValue;
        maxElevation = float.MinValue;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 normal = vertices[i].normalized;
            float displacement = 0f;
            float mask = 1f;

            foreach (NoiseLayer noise in shapeSettings.noiseLayers)
            {
                if (!noise.enabled) continue;

                float frequency = noise.roughness;
                float amplitude = 1f;
                float layerNoise = 0f;
                float totalAmplitude = 0f;

                for (int o = 0; o < noise.octaves; o++)
                {
                    Vector3 sample = (normal + noise.offset) * frequency;
                    float v = PerlinNoise3D.GenerateNoise(sample.x, sample.y, sample.z);

                    v = noise.noiseType == NoiseType.Ridge
                        ? 1f - Mathf.Abs(v * 2f - 1f)
                        : v * 2f - 1f;

                    layerNoise += v * amplitude;
                    totalAmplitude += amplitude;
                    amplitude *= noise.persistence;
                    frequency *= noise.lacunarity;
                }

                float noiseVal = totalAmplitude > 0 ? layerNoise / totalAmplitude : 0f;
                noiseVal += noise.minValue;

                if (noise.useFirstLayerAsMask && mask <= 0) continue;
                if (noise.useFirstLayerAsMask) mask = noiseVal;

                displacement += noiseVal * noise.strength;
            }

            float height = 1 + displacement + shapeSettings.globalHeightOffset;
            vertices[i] = normal * radius * height;
            float finalMagnitude = vertices[i].magnitude;
            minElevation = Mathf.Min(minElevation, finalMagnitude);
            maxElevation = Mathf.Max(maxElevation, finalMagnitude);
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        mesh.normals = CalculateNormals(vertices, triangles);
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;

        if (meshRenderer != null && colorSettings.planetMaterial != null)
        {
            meshRenderer.sharedMaterial = colorSettings.planetMaterial;
            meshRenderer.sharedMaterial.SetFloat("_Radius", radius);
            meshRenderer.sharedMaterial.SetFloat("_MinHeight", minElevation);
            meshRenderer.sharedMaterial.SetFloat("_MaxHeight", maxElevation);
            meshRenderer.sharedMaterial.SetColor("_OceanColor", colorSettings.oceanColor);

            UpdateBiomeTexture();
            if (biomeTexture != null)
                meshRenderer.sharedMaterial.SetTexture("_BiomeTexture", biomeTexture);
        }

        GenerateOceanPlane();
        GenerateAtmospherePlane();
    }

    Vector3[] CalculateNormals(Vector3[] vertices, int[] triangles)
    {
        Vector3[] normals = new Vector3[vertices.Length];

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int i1 = triangles[i], i2 = triangles[i + 1], i3 = triangles[i + 2];
            Vector3 normal = Vector3.Cross(vertices[i2] - vertices[i1], vertices[i3] - vertices[i1]).normalized;
            normals[i1] += normal;
            normals[i2] += normal;
            normals[i3] += normal;
        }

        for (int i = 0; i < normals.Length; i++) normals[i].Normalize();
        return normals;
    }

    void UpdateBiomeTexture()
    {
        if (colorSettings == null || colorSettings.biomes == null || colorSettings.biomes.Length == 0)
        {
            if (biomeTexture != null) DestroyImmediate(biomeTexture);
            return;
        }

        int res = 256;
        biomeTexture = new Texture2D(res, 1, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color[] pixels = new Color[res];
        System.Array.Sort(colorSettings.biomes, (a, b) => a.startHeight.CompareTo(b.startHeight));

        for (int i = 0; i < res; i++)
        {
            float h = (float)i / (res - 1);
            Color c = colorSettings.oceanColor;

            foreach (var biome in colorSettings.biomes)
            {
                float blend = Mathf.Clamp01((h - biome.startHeight) / biome.blendAmount);
                c = Color.Lerp(c, biome.color, blend);
            }
            pixels[i] = c;
        }

        biomeTexture.SetPixels(pixels);
        biomeTexture.Apply();
    }

    void GenerateOceanPlane()
    {
        if (oceanGameObject == null)
        {
            oceanGameObject = new GameObject("Ocean");
            oceanGameObject.transform.SetParent(transform);
            oceanGameObject.transform.localPosition = Vector3.zero;
            oceanGameObject.transform.localRotation = Quaternion.identity;

            oceanMeshFilter = oceanGameObject.AddComponent<MeshFilter>();
            oceanMeshRenderer = oceanGameObject.AddComponent<MeshRenderer>();
            oceanMesh = new Mesh { name = "Ocean Mesh" };
            oceanMeshFilter.sharedMesh = oceanMesh;
        }

        oceanMesh.Clear();
        SphereCreator.CreateSphereMesh(resolution, radius * 1.01f, out Vector3[] verts, out int[] tris, out Vector2[] uvs);
        oceanMesh.vertices = verts;
        oceanMesh.triangles = tris;
        oceanMesh.uv = uvs;
        oceanMesh.RecalculateNormals();
        oceanMesh.RecalculateBounds();

        if (colorSettings.oceanMaterial != null)
        {
            oceanMeshRenderer.sharedMaterial = colorSettings.oceanMaterial;
            oceanMeshRenderer.sharedMaterial.SetFloat("_Radius", radius);
            oceanMeshRenderer.sharedMaterial.SetColor("_Color", colorSettings.oceanColor);
        }
    }

    void GenerateAtmospherePlane()
    {
        if (atmosphereGameObject == null)
        {
            atmosphereGameObject = new GameObject("Atmosphere");
            atmosphereGameObject.transform.SetParent(transform);
            atmosphereGameObject.transform.localPosition = Vector3.zero;
            atmosphereGameObject.transform.localRotation = Quaternion.identity;

            atmosphereMeshFilter = atmosphereGameObject.AddComponent<MeshFilter>();
            atmosphereMeshRenderer = atmosphereGameObject.AddComponent<MeshRenderer>();
            atmosphereMesh = new Mesh { name = "Atmosphere Mesh" };
            atmosphereMeshFilter.sharedMesh = atmosphereMesh;
            atmosphereController = atmosphereGameObject.AddComponent<AtmosphereController>();
        }

        atmosphereMesh.Clear();

        float buffer = 0.02f;
        float atmosphereRadius = Mathf.Min(maxElevation * (1f + buffer), radius * 1.1f);

        SphereCreator.CreateSphereMesh(resolution, atmosphereRadius, out Vector3[] verts, out int[] tris, out Vector2[] uvs);
        atmosphereMesh.vertices = verts;
        atmosphereMesh.triangles = tris;
        atmosphereMesh.uv = uvs;
        atmosphereMesh.RecalculateNormals();
        atmosphereMesh.RecalculateBounds();

        if (colorSettings.atmosphereMaterial != null)
        {
            atmosphereMeshRenderer.sharedMaterial = colorSettings.atmosphereMaterial;

            // ✅ FIX: Safe and modern way to assign sunLight
            if (sceneSunLight == null)
            {
                GameObject sunGO = GameObject.FindWithTag("Sun");
                if (sunGO != null)
                    sceneSunLight = sunGO.GetComponent<Light>();
                else
                    sceneSunLight = Object.FindFirstObjectByType<Light>();
            }

            if (atmosphereController != null)
            {
                atmosphereController.atmosphereMaterial = colorSettings.atmosphereMaterial;
                atmosphereController.sunLight = sceneSunLight;
                atmosphereController.atmosphereRadius = atmosphereRadius;
                atmosphereController.atmosphereColor = colorSettings.atmosphereColor;
                atmosphereController.density = colorSettings.atmosphereDensity;
                atmosphereController.power = colorSettings.atmospherePower;
                atmosphereController.ambientLightInfluence = colorSettings.atmosphereAmbientLightInfluence;
                atmosphereController.rimPower = colorSettings.atmosphereRimPower;
            }
        }
    }

    void OnDestroy()
    {
        if (biomeTexture != null) DestroyImmediate(biomeTexture);
        if (oceanGameObject != null) DestroyImmediate(oceanGameObject);
        if (atmosphereGameObject != null) DestroyImmediate(atmosphereGameObject);
    }
}
