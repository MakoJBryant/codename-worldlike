using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class PlanetChunk : MonoBehaviour
{
    public ShapeSettings shapeSettings;
    public Material baseMaterial;

    public int baseResolution = 64;          // Highest LOD resolution
    public int maxLOD = 4;
    public float chunkSize = 10f;
    public float lodDistanceMultiplier = 2f;

    private Mesh[] lodMeshes;
    private GameObject[] lodObjects;

    private float[] lodThresholds;
    private int currentLOD = -1;
    private int targetLOD = -1;
    private float lodTransitionProgress = 1f;
    private const float fadeDuration = 0.5f;

    public void Initialize(ShapeSettings shapeSettings, Material baseMaterial, int baseResolution, int maxLOD, float chunkSize, float lodDistanceMultiplier)
    {
        this.shapeSettings = shapeSettings;
        this.baseMaterial = baseMaterial;
        this.baseResolution = baseResolution;
        this.maxLOD = maxLOD;
        this.chunkSize = chunkSize;
        this.lodDistanceMultiplier = lodDistanceMultiplier;

        GenerateLODs();

        currentLOD = maxLOD - 1; // Start at lowest detail
        targetLOD = currentLOD;
    }

    void Update()
    {
        if (Camera.main == null) return;
        UpdateLOD(Camera.main.transform.position);
    }

    void GenerateLODs()
    {
        lodMeshes = new Mesh[maxLOD];
        lodObjects = new GameObject[maxLOD];
        lodThresholds = new float[maxLOD];

        for (int i = 0; i < maxLOD; i++)
        {
            int resolution = baseResolution / (1 << i);
            if (resolution < 4) resolution = 4;

            // Assuming CubeSphereBuilder.Generate returns (Vector3[] verts, int[] tris)
            var (verts, tris) = CubeSphereBuilder.Generate(resolution);

            // Create ComputeBuffer from verts for CalculateHeights
            ComputeBuffer vertexBuffer = new ComputeBuffer(verts.Length, sizeof(float) * 3);
            vertexBuffer.SetData(verts);

            // Calculate heights using shapeSettings which expects ComputeBuffer
            float[] heights = shapeSettings.CalculateHeights(vertexBuffer);

            // Release buffer after use
            vertexBuffer.Release();

            // Apply height scaling to vertices
            for (int j = 0; j < verts.Length; j++)
                verts[j] *= heights[j];

            Mesh mesh = new Mesh();
            mesh.indexFormat = verts.Length < 65535 ? UnityEngine.Rendering.IndexFormat.UInt16 : UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            lodMeshes[i] = mesh;

            GameObject lodObj = new GameObject("LOD" + i);
            lodObj.transform.SetParent(transform, false);
            var mf = lodObj.AddComponent<MeshFilter>();
            var mr = lodObj.AddComponent<MeshRenderer>();
            mf.sharedMesh = mesh;

            Material fadeMat = new Material(baseMaterial);
            fadeMat.SetFloat("_FadeAmount", 0f);
            mr.sharedMaterial = fadeMat;

            lodObjects[i] = lodObj;
            lodObj.SetActive(i == currentLOD);

            lodThresholds[i] = chunkSize * Mathf.Pow(lodDistanceMultiplier, i);
        }
    }

    public void UpdateLOD(Vector3 viewerPos)
    {
        float dist = Vector3.Distance(transform.position, viewerPos);
        int desiredLOD = maxLOD - 1;

        for (int i = 0; i < lodThresholds.Length; i++)
        {
            if (dist < lodThresholds[i])
            {
                desiredLOD = i;
                break;
            }
        }

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
            float alpha = 0f;
            if (i == currentLOD) alpha = 1f - lodTransitionProgress;
            else if (i == targetLOD) alpha = lodTransitionProgress;

            lodObjects[i].SetActive(alpha > 0f);
            var mat = lodObjects[i].GetComponent<MeshRenderer>().sharedMaterial;
            mat.SetFloat("_FadeAmount", alpha);
        }

        if (lodTransitionProgress >= 1f)
            currentLOD = targetLOD;
    }
}
