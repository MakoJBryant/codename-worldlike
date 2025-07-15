using UnityEngine;
using MakoJBryant.SolarSystem.Generation;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
[DisallowMultipleComponent]
[ExecuteInEditMode]
public class PlanetGenerator : MonoBehaviour
{
    [Range(2, 256)] public int resolution = 64;
    public float radius = 1f;
    [Range(0f, 1f)] public float seaLevel = 0.5f;
    public Light sceneSunLight;

    [Header("Settings Assets")]
    public ShapeSettings shapeSettings;
    public ColorSettings colorSettings;

    [Range(0.5f, 1.5f)] public float atmosphereExpansionFactor = 1.02f;

    public Transform sunTransform;

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
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();

        mesh = new Mesh { name = "Generated Planet Mesh" };
        meshFilter.sharedMesh = mesh;

        GeneratePlanet();
    }

    void OnValidate()
    {
        if (shapeSettings != null && colorSettings != null)
            GeneratePlanet();
    }

    [ContextMenu("Generate Planet Now")]
    public void GeneratePlanet()
    {
        if (shapeSettings == null || colorSettings == null)
        {
            Debug.LogWarning("ShapeSettings or ColorSettings missing—aborting planet generation.");
            return;
        }

        mesh.Clear();
        SphereCreator.CreateSphereMesh(resolution, radius, out Vector3[] vertices, out int[] triangles, out Vector2[] uvs);

        // --- DIAGNOSTIC: Calculate mesh centroid before displacement ---
        Vector3 centroidBefore = Vector3.zero;
        for (int i = 0; i < vertices.Length; i++)
            centroidBefore += vertices[i];
        centroidBefore /= vertices.Length;
        Debug.Log($"[PlanetGenerator] Mesh centroid before displacement: {centroidBefore}");

        minElevation = float.MaxValue;
        maxElevation = float.MinValue;

        // Displace vertices based on noise, in local space
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 normal = vertices[i].normalized;
            float displacement = shapeSettings.globalHeightOffset;
            float firstValue = 0f;

            foreach (var layer in shapeSettings.noiseLayers)
            {
                if (!layer.enabled) continue;

                float noise = 0f, freq = layer.roughness, amp = 1f, ampSum = 0f;
                for (int o = 0; o < layer.octaves; o++)
                {
                    Vector3 p = (normal + layer.offset) * freq;
                    float v = PerlinNoise3D.GenerateNoise(p.x, p.y, p.z);
                    v = layer.noiseType == NoiseType.Ridge ? 1 - Mathf.Abs(v * 2 - 1) : v * 2 - 1;
                    noise += v * amp;
                    ampSum += amp;
                    amp *= layer.persistence;
                    freq *= layer.lacunarity;
                }

                float final = (ampSum == 0f ? 0f : noise / ampSum) + layer.minValue;
                if (layer.useFirstLayerAsMask) firstValue = final;
                if (layer.useFirstLayerAsMask && firstValue <= 0f) final = 0f;

                displacement += final * layer.strength;
            }

            // Displace vertex along its normal (local space)
            float originalLength = vertices[i].magnitude;
            vertices[i] = normal * (originalLength + displacement);

            float height = vertices[i].magnitude;
            minElevation = Mathf.Min(minElevation, height);
            maxElevation = Mathf.Max(maxElevation, height);

            // Debug displaced vertex positions of first few vertices (optional)
            if (i < 10)
            {
                Debug.Log($"[Vertex {i}] Normal: {normal}, Displacement: {displacement}, New Pos: {vertices[i]}");
            }
        }

        // --- DIAGNOSTIC: Calculate mesh centroid after displacement ---
        Vector3 centroidAfter = Vector3.zero;
        for (int i = 0; i < vertices.Length; i++)
            centroidAfter += vertices[i];
        centroidAfter /= vertices.Length;
        Debug.Log($"[PlanetGenerator] Mesh centroid after displacement: {centroidAfter}");

        // Calculate normals from triangles
        var normals = new Vector3[vertices.Length];
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
            Vector3 faceNormal = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]).normalized;
            normals[a] += faceNormal;
            normals[b] += faceNormal;
            normals[c] += faceNormal;
        }
        for (int i = 0; i < normals.Length; i++) normals[i].Normalize();

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.normals = normals;
        mesh.RecalculateBounds();

        // Update material properties
        meshRenderer.sharedMaterial = colorSettings.planetMaterial;
        meshRenderer.sharedMaterial.SetFloat("_Radius", radius);
        meshRenderer.sharedMaterial.SetFloat("_MinHeight", minElevation);
        meshRenderer.sharedMaterial.SetFloat("_MaxHeight", maxElevation);
        meshRenderer.sharedMaterial.SetVector("_PlanetCenter", transform.position);

        biomeTexture = UpdateBiomeTexture();
        if (biomeTexture != null)
            meshRenderer.sharedMaterial.SetTexture("_BiomeTexture", biomeTexture);

        meshCollider.sharedMesh = mesh;

        AlignChildObjects();
        GenerateOceanPlane();
        GenerateAtmospherePlane();
    }

    void LateUpdate()
    {
        AlignChildObjects();

        if (sunTransform != null)
        {
            float orbitSpeed = 10f;
            transform.RotateAround(sunTransform.position, Vector3.up, orbitSpeed * Time.deltaTime);

            meshRenderer.sharedMaterial.SetVector("_PlanetCenter", transform.position);

            if (oceanMeshRenderer != null)
                oceanMeshRenderer.sharedMaterial.SetVector("_PlanetCenter", transform.position);

            if (atmosphereMeshRenderer != null)
                atmosphereMeshRenderer.sharedMaterial.SetVector("_PlanetCenter", transform.position);
        }
    }

    private void AlignChildObjects()
    {
        if (oceanGameObject != null)
            oceanGameObject.transform.SetParent(transform, false);

        if (atmosphereGameObject != null)
            atmosphereGameObject.transform.SetParent(transform, false);
    }

    void GenerateOceanPlane()
    {
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
            oceanGameObject.transform.SetParent(transform, false);
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

        if (oceanMeshRenderer != null && colorSettings.oceanMaterial != null)
        {
            oceanMeshRenderer.sharedMaterial = colorSettings.oceanMaterial;
            oceanMeshRenderer.sharedMaterial.SetFloat("_Radius", oceanRadius);
            oceanMeshRenderer.sharedMaterial.SetColor("_Color", colorSettings.oceanColor);
            oceanMeshRenderer.sharedMaterial.SetVector("_PlanetCenter", transform.position);
        }

        Debug.Log($"Ocean Transform - Position: {oceanGameObject.transform.position}, Rotation: {oceanGameObject.transform.rotation.eulerAngles}, Scale: {oceanGameObject.transform.localScale}");
    }

    void GenerateAtmospherePlane()
    {
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
            atmosphereGameObject.transform.SetParent(transform, false);
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

        if (atmosphereMeshRenderer != null && colorSettings.atmosphereMaterial != null)
        {
            atmosphereMeshRenderer.sharedMaterial = colorSettings.atmosphereMaterial;

#if UNITY_2023_1_OR_NEWER
            if (sceneSunLight == null)
                sceneSunLight = Object.FindFirstObjectByType<Light>();
#else
            if (sceneSunLight == null)
                sceneSunLight = FindObjectOfType<Light>();
#endif
            atmosphereController.sunLight = sceneSunLight;
            atmosphereController.atmosphereMaterial = colorSettings.atmosphereMaterial;
            atmosphereController.atmosphereRadius = atmosphereRadius;
            atmosphereController.atmosphereColor = colorSettings.atmosphereColor;
            atmosphereController.density = colorSettings.atmosphereDensity;
            atmosphereController.power = colorSettings.atmospherePower;
            atmosphereController.ambientLightInfluence = colorSettings.atmosphereAmbientLightInfluence;
            atmosphereController.rimPower = colorSettings.atmosphereRimPower;
        }

        Debug.Log($"Atmosphere Transform - Position: {atmosphereGameObject.transform.position}, Rotation: {atmosphereGameObject.transform.rotation.eulerAngles}, Scale: {atmosphereGameObject.transform.localScale}");
    }

    Texture2D UpdateBiomeTexture()
    {
        if (colorSettings.biomes == null || colorSettings.biomes.Length == 0)
            return null;

        int texRes = 256;
        Texture2D texture = new Texture2D(texRes, 1, TextureFormat.RGBA32, false)
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

        texture.SetPixels(pixels);
        texture.Apply();

        return texture;
    }

    void OnDestroy()
    {
        if (biomeTexture != null) DestroyImmediate(biomeTexture);
        if (oceanGameObject != null) DestroyImmediate(oceanGameObject);
        if (atmosphereGameObject != null) DestroyImmediate(atmosphereGameObject);
    }
}
