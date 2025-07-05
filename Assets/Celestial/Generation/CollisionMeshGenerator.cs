using UnityEngine;

[RequireComponent(typeof(MeshCollider))]
[ExecuteAlways]
public class CollisionMeshGenerator : MonoBehaviour
{
    public ShapeSettings shapeSettings;
    public int colliderResolution = 50;

    private ComputeBuffer vertexBuffer;
    private Mesh collisionMesh;

    void OnEnable()
    {
        GenerateCollisionMesh();
    }

    void OnDisable()
    {
        if (vertexBuffer != null)
        {
            vertexBuffer.Release();
            vertexBuffer = null;
        }

        if (collisionMesh != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(collisionMesh);
#else
            Destroy(collisionMesh);
#endif
            collisionMesh = null;
        }
    }

    void GenerateCollisionMesh()
    {
        if (shapeSettings == null || shapeSettings.heightMapCompute == null)
        {
            Debug.LogWarning("Missing shape settings or compute shader for collider.");
            return;
        }

        // Explicit typing for deconstruction from CubeSphereBuilder
        (Vector3[] verts, int[] tris) = CubeSphereBuilder.Generate(colliderResolution);

        if (verts.Length == 0 || tris.Length == 0)
        {
            Debug.LogWarning("Generated zero vertices or triangles for collider.");
            return;
        }

        vertexBuffer?.Release();
        vertexBuffer = new ComputeBuffer(verts.Length, sizeof(float) * 3);
        vertexBuffer.SetData(verts);

        float[] heights = shapeSettings.CalculateHeights(vertexBuffer);
        for (int i = 0; i < verts.Length; i++)
        {
            verts[i] *= heights[i];
        }

        if (collisionMesh == null)
            collisionMesh = new Mesh();
        else
            collisionMesh.Clear();

        collisionMesh.indexFormat = verts.Length < 65535
            ? UnityEngine.Rendering.IndexFormat.UInt16
            : UnityEngine.Rendering.IndexFormat.UInt32;

        collisionMesh.vertices = verts;
        collisionMesh.triangles = tris;
        collisionMesh.RecalculateNormals();

        var meshCollider = GetComponent<MeshCollider>();
        if (meshCollider != null)
        {
            meshCollider.sharedMesh = collisionMesh;
        }
    }
}
