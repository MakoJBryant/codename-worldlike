using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class PlanetGenerator : MonoBehaviour
{
    public enum PreviewMode { LOD0, LOD1, LOD2, CollisionRes }
    public PreviewMode previewMode;
    public ResolutionSettings resolutionSettings;

    public PlanetSettings planetSettings;
    public bool logTimers;

    private Mesh previewMesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private ComputeBuffer vertexBuffer;
    private Vector2 heightMinMax;

    private static Dictionary<int, SphereMesh> sphereGenerators;

    void OnEnable()
    {
        RegeneratePlanet();
    }

    void OnValidate()
    {
        RegeneratePlanet();
    }

    void RegeneratePlanet()
    {
        if (planetSettings == null || planetSettings.shape == null || planetSettings.shading == null)
        {
            Debug.LogWarning("Missing planet settings, shape, or shading.");
            return;
        }

        if (planetSettings.shape.heightMapCompute == null)
        {
            Debug.LogWarning("Missing compute shader in ShapeSettings.");
            return;
        }

        int resolution = resolutionSettings.GetLODResolution((int)previewMode);
        var (vertices, triangles) = CreateSphereVertsAndTris(resolution);

        // Create or update structured buffer
        ComputeHelper.CreateStructuredBuffer(ref vertexBuffer, vertices);

        // Calculate heights using compute shader
        float[] heights = planetSettings.shape.CalculateHeights(vertexBuffer);

        float minH = float.PositiveInfinity;
        float maxH = float.NegativeInfinity;

        for (int i = 0; i < vertices.Length; i++)
        {
            float h = heights[i];
            vertices[i] *= h;
            minH = Mathf.Min(minH, h);
            maxH = Mathf.Max(maxH, h);
        }

        heightMinMax = new Vector2(minH, maxH);
        CreateMesh(ref previewMesh, vertices, triangles);

        if (meshFilter == null || meshRenderer == null)
        {
            var meshObj = GetOrCreateMeshObject("Planet Mesh");
            meshFilter = meshObj.GetComponent<MeshFilter>();
            meshRenderer = meshObj.GetComponent<MeshRenderer>();
        }

        meshFilter.sharedMesh = previewMesh;
        meshRenderer.sharedMaterial = planetSettings.shading.terrainMaterial;

        ComputeHelper.Release(vertexBuffer);
        vertexBuffer = null; // Avoid double release
    }

    GameObject GetOrCreateMeshObject(string name)
    {
        var child = transform.Find(name);
        if (child == null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            return go;
        }
        return child.gameObject;
    }

    (Vector3[] vertices, int[] triangles) CreateSphereVertsAndTris(int res)
    {
        if (sphereGenerators == null)
            sphereGenerators = new Dictionary<int, SphereMesh>();

        if (!sphereGenerators.ContainsKey(res))
            sphereGenerators.Add(res, new SphereMesh(res));

        var generator = sphereGenerators[res];
        Vector3[] verts = new Vector3[generator.Vertices.Length];
        int[] tris = new int[generator.Triangles.Length];
        System.Array.Copy(generator.Vertices, verts, verts.Length);
        System.Array.Copy(generator.Triangles, tris, tris.Length);
        return (verts, tris);
    }

    void CreateMesh(ref Mesh mesh, Vector3[] verts, int[] tris)
    {
        if (mesh == null)
            mesh = new Mesh();
        else
            mesh.Clear();

        mesh.indexFormat = verts.Length < 65535
            ? UnityEngine.Rendering.IndexFormat.UInt16
            : UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
    }

    [System.Serializable]
    public class ResolutionSettings
    {
        public int lod0 = 300;
        public int lod1 = 100;
        public int lod2 = 50;
        public int collider = 100;

        public int GetLODResolution(int lod)
        {
            return lod switch
            {
                0 => lod0,
                1 => lod1,
                2 => lod2,
                _ => collider,
            };
        }
    }
}
