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

    public TerrainChunk(Vector2Int coord, Transform parent, PlanetSettings settings, ResolutionSettings res)
    {
        this.coord = coord;
        planetSettings = settings;
        resolutionSettings = res;

        chunkObject = new GameObject("Chunk_" + coord.x + "_" + coord.y);
        chunkObject.transform.parent = parent;

        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        mesh = new Mesh();
        meshFilter.sharedMesh = mesh;
    }

    public void UpdateLOD(Camera cam, float planetRadius)
    {
        float distance = Vector3.Distance(chunkObject.transform.position, cam.transform.position);

        int lod = 0;
        if (distance > 300) lod = 1;
        if (distance > 600) lod = 2;

        if (lod != currentLOD)
        {
            GenerateMesh(lod);
            currentLOD = lod;
        }
    }

    private void GenerateMesh(int lod)
    {
        int resolution = resolutionSettings.GetLODResolution(lod);
        var (verts, tris) = CubeSphereBuilder.Generate(resolution);

        // Apply compute shader deformation
        vertexBuffer?.Release();
        vertexBuffer = new ComputeBuffer(verts.Length, sizeof(float) * 3);
        vertexBuffer.SetData(verts);

        float[] heights = planetSettings.shape.CalculateHeights(vertexBuffer);
        for (int i = 0; i < verts.Length; i++)
            verts[i] *= heights[i];

        mesh.Clear();
        mesh.indexFormat = verts.Length > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();

        meshFilter.sharedMesh = mesh;
        meshRenderer.sharedMaterial = planetSettings.shading.terrainMaterial;
    }

    public void Cleanup()
    {
        vertexBuffer?.Release();
        vertexBuffer = null;

#if UNITY_EDITOR
        Object.DestroyImmediate(chunkObject);
#else
        Object.Destroy(chunkObject);
#endif
    }
}
