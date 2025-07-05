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

    void OnValidate()
    {
        // Auto-update collider mesh when values change in inspector
        GenerateCollisionMesh();
    }

    void GenerateCollisionMesh()
    {
        if (shapeSettings == null)
        {
            Debug.LogWarning("CollisionMeshGenerator: shapeSettings is null. Cannot generate collision mesh.");
            return;
        }
        if (shapeSettings.heightMapCompute == null)
        {
            Debug.LogWarning("CollisionMeshGenerator: shapeSettings.heightMapCompute is null. Cannot generate collision mesh.");
            return;
        }
        if (colliderResolution < 1)
        {
            Debug.LogWarning("CollisionMeshGenerator: colliderResolution must be >= 1.");
            return;
        }

        Debug.Log($"CollisionMeshGenerator: Generating collision mesh with resolution {colliderResolution}...");

        (Vector3[] verts, int[] tris) = CubeSphereBuilder.Generate(colliderResolution);

        if (verts.Length == 0 || tris.Length == 0)
        {
            Debug.LogWarning("CollisionMeshGenerator: Generated zero vertices or triangles for collider.");
            return;
        }

        Debug.Log($"CollisionMeshGenerator: Generated {verts.Length} vertices and {tris.Length} triangles.");

        vertexBuffer?.Release();
        vertexBuffer = new ComputeBuffer(verts.Length, sizeof(float) * 3);
        vertexBuffer.SetData(verts);

        float[] heights = shapeSettings.CalculateHeights(vertexBuffer);

        if (heights == null || heights.Length != verts.Length)
        {
            Debug.LogWarning($"CollisionMeshGenerator: Heights array length {heights?.Length ?? 0} doesn't match vertices length {verts.Length}.");
            vertexBuffer.Release();
            vertexBuffer = null;
            return;
        }

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
            Debug.Log("CollisionMeshGenerator: Assigned generated mesh to MeshCollider.");
        }
        else
        {
            Debug.LogWarning("CollisionMeshGenerator: No MeshCollider component found on this GameObject.");
        }
    }
}
