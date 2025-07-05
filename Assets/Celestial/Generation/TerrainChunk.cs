using UnityEngine;
using static PlanetGenerator;

public class TerrainChunk
{
    public Vector2Int coord;
    public GameObject chunkObject;
    public int currentLOD = -1;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private PlanetSettings planetSettings;
    private ResolutionSettings resolutionSettings;
    private ComputeBuffer vertexBuffer;
    private Mesh mesh;

    // LOD distance thresholds (tweakable)
    private float lod0Distance = 300f;
    private float lod1Distance = 600f;

    public TerrainChunk(Vector2Int coord, Vector3 position, Transform parent, PlanetSettings settings, ResolutionSettings res)
    {
        this.coord = coord;
        planetSettings = settings;
        resolutionSettings = res;

        chunkObject = new GameObject("Chunk_" + coord.x + "_" + coord.y);
        chunkObject.transform.parent = parent;
        chunkObject.transform.position = position;  // Set position here for distance calculations!

        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        mesh = new Mesh();
        meshFilter.mesh = mesh;  // Use instance mesh, safer for runtime modifications
    }

    public void UpdateLOD(Camera cam, float planetRadius)
    {
        if (cam == null || planetSettings == null || planetSettings.shape == null)
        {
            Debug.LogWarning("TerrainChunk.UpdateLOD: Missing Camera or PlanetSettings or ShapeSettings");
            return;
        }

        float distance = Vector3.Distance(chunkObject.transform.position, cam.transform.position);

        int lod = 0;
        if (distance > lod0Distance) lod = 1;
        if (distance > lod1Distance) lod = 2;

        if (lod != currentLOD)
        {
            GenerateMesh(lod, planetRadius);
            currentLOD = lod;
        }
    }

    private void GenerateMesh(int lod, float planetRadius)
    {
        int resolution = resolutionSettings.GetLODResolution(lod);
        var (verts, tris) = CubeSphereBuilder.Generate(resolution);

        // Release previous buffer before creating new
        if (vertexBuffer != null)
        {
            vertexBuffer.Release();
            vertexBuffer = null;
        }

        vertexBuffer = new ComputeBuffer(verts.Length, sizeof(float) * 3);
        vertexBuffer.SetData(verts);

        float[] heights = planetSettings.shape.CalculateHeights(vertexBuffer);
        for (int i = 0; i < verts.Length; i++)
        {
            verts[i] *= heights[i];
        }

        mesh.Clear();
        mesh.indexFormat = verts.Length > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        meshRenderer.sharedMaterial = planetSettings.shading.terrainMaterial;
    }

    public void Cleanup()
    {
        if (vertexBuffer != null)
        {
            vertexBuffer.Release();
            vertexBuffer = null;
        }

#if UNITY_EDITOR
        Object.DestroyImmediate(chunkObject);
#else
        Object.Destroy(chunkObject);
#endif
    }
}
