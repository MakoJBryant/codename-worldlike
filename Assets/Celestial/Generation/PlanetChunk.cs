using UnityEngine;

public class PlanetChunk : MonoBehaviour
{
    public ShapeSettings shapeSettings;
    public Material lodMaterial; // Assign your LOD fade material here in inspector

    public int baseResolution = 64;
    public int maxLOD = 4;
    public float chunkSize = 10f;
    public float lodDistanceMultiplier = 2f;

    private Mesh[] lodMeshes;
    private GameObject[] lodObjects;
    private Material[] lodMaterials;

    private float[] lodThresholds;
    private int currentLOD = -1;
    private int targetLOD = -1;
    private float lodTransitionProgress = 1f;
    private const float fadeDuration = 0.5f;

    // Call this once after creating chunk, assign parameters
    public void Initialize(
        ShapeSettings shapeSettings,
        Material lodMaterial,
        int baseResolution,
        int maxLOD,
        float chunkSize,
        float lodDistanceMultiplier)
    {
        this.shapeSettings = shapeSettings;
        this.lodMaterial = lodMaterial;
        this.baseResolution = baseResolution;
        this.maxLOD = maxLOD;
        this.chunkSize = chunkSize;
        this.lodDistanceMultiplier = lodDistanceMultiplier;

        if (shapeSettings == null || lodMaterial == null)
        {
            Debug.LogError("PlanetChunk.Initialize: shapeSettings or lodMaterial is null.");
            return;
        }

        GenerateLODs();

        // Start with lowest detail visible
        currentLOD = maxLOD - 1;
        targetLOD = currentLOD;
    }

    void GenerateLODs()
    {
        lodMeshes = new Mesh[maxLOD];
        lodObjects = new GameObject[maxLOD];
        lodMaterials = new Material[maxLOD];
        lodThresholds = new float[maxLOD];

        for (int i = 0; i < maxLOD; i++)
        {
            int resolution = Mathf.Max(baseResolution / (1 << i), 4);

            var (verts, tris) = CubeSphereBuilder.Generate(resolution);

            ApplyHeightDeformation(ref verts);

            Mesh mesh = new Mesh();
            mesh.indexFormat = verts.Length < 65535 ? UnityEngine.Rendering.IndexFormat.UInt16 : UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.MarkDynamic();

            lodMeshes[i] = mesh;

            GameObject lodObj = new GameObject($"LOD{i}");
            lodObj.transform.SetParent(transform, false);

            var mf = lodObj.AddComponent<MeshFilter>();
            mf.mesh = mesh;

            var mr = lodObj.AddComponent<MeshRenderer>();

            lodMaterials[i] = new Material(lodMaterial);
            mr.material = lodMaterials[i];

            lodMaterials[i].SetFloat("_FadeAmount", (i == currentLOD) ? 1f : 0f);

            lodObj.SetActive(i == currentLOD);

            lodObjects[i] = lodObj;

            // Set threshold distances exponentially
            lodThresholds[i] = chunkSize * Mathf.Pow(lodDistanceMultiplier, i);
        }
    }

    void ApplyHeightDeformation(ref Vector3[] verts)
    {
        if (shapeSettings == null || shapeSettings.noiseLayers == null || shapeSettings.noiseLayers.Count == 0)
        {
            for (int i = 0; i < verts.Length; i++)
                verts[i] *= shapeSettings.planetRadius;
            return;
        }

        for (int i = 0; i < verts.Length; i++)
        {
            float height = 1f;

            for (int n = 0; n < shapeSettings.noiseLayers.Count; n++)
            {
                var layer = shapeSettings.noiseLayers[n];
                if (!layer.enabled)
                    continue;

                float noiseValue = FBM(verts[i] * layer.frequency + layer.offset, layer.octaves, layer.persistence);

                if (layer.invert)
                    noiseValue = 1f - noiseValue;

                noiseValue = Mathf.Pow(noiseValue, layer.power);
                noiseValue *= layer.amplitude;

                if (n == 0 || !layer.useFirstLayerAsMask)
                {
                    height += noiseValue;
                }
                else
                {
                    height += noiseValue * Mathf.Clamp01(height - 1f);
                }
            }

            verts[i] *= height * shapeSettings.planetRadius;
        }
    }

    float FBM(Vector3 point, int octaves, float persistence)
    {
        float total = 0f;
        float amplitude = 1f;
        float frequency = 1f;

        for (int i = 0; i < octaves; i++)
        {
            total += Mathf.PerlinNoise(point.x * frequency, point.y * frequency) * amplitude;
            amplitude *= persistence;
            frequency *= 2f;
        }

        return total;
    }

    public void UpdateLOD(Vector3 viewerPos)
    {
        float dist = Vector3.Distance(transform.position, viewerPos);

        Debug.Log($"PlanetChunk: Distance to viewer = {dist}");

        int desiredLOD = maxLOD - 1;

        for (int i = 0; i < lodThresholds.Length; i++)
        {
            if (dist < lodThresholds[i])
            {
                desiredLOD = i;
                break;
            }
        }

        Debug.Log($"PlanetChunk: Desired LOD = {desiredLOD}");

        if (targetLOD != desiredLOD)
        {
            targetLOD = desiredLOD;
            lodTransitionProgress = 0f;
        }

        if (lodTransitionProgress < 1f)
        {
            lodTransitionProgress += Time.deltaTime / fadeDuration;
            if (lodTransitionProgress > 1f) lodTransitionProgress = 1f;
        }

        for (int i = 0; i < lodObjects.Length; i++)
        {
            if (lodObjects[i] == null)
                continue;

            float alpha = 0f;
            if (i == currentLOD) alpha = 1f - lodTransitionProgress;
            else if (i == targetLOD) alpha = lodTransitionProgress;

            lodObjects[i].SetActive(alpha > 0f);

            if (lodMaterials[i] != null)
                lodMaterials[i].SetFloat("_FadeAmount", alpha);
        }

        if (lodTransitionProgress >= 1f)
            currentLOD = targetLOD;
    }
}
