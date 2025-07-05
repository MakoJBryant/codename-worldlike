using UnityEngine;

public class ComputeShaderHeightTest : MonoBehaviour
{
    public ComputeShader heightMapCompute;    // Assign your HeightMap.compute asset here
    public float planetRadius = 1f;
    public int vertexCount = 256;              // Number of vertices to test

    private ComputeBuffer vertexBuffer;
    private ComputeBuffer heightBuffer;

    private Vector3[] vertices;
    private float[] heights;

    void Start()
    {
        if (heightMapCompute == null)
        {
            Debug.LogError("Compute shader not assigned!");
            enabled = false;  // Disable this script to avoid further errors
            return;
        }

        // Create simple test vertices (points on unit sphere)
        vertices = new Vector3[vertexCount];
        for (int i = 0; i < vertexCount; i++)
        {
            float phi = Mathf.Acos(1 - 2 * (i + 0.5f) / vertexCount);
            float theta = Mathf.PI * (1 + Mathf.Sqrt(5)) * (i + 0.5f);

            float x = Mathf.Cos(theta) * Mathf.Sin(phi);
            float y = Mathf.Sin(theta) * Mathf.Sin(phi);
            float z = Mathf.Cos(phi);

            vertices[i] = new Vector3(x, y, z);
        }

        vertexBuffer = new ComputeBuffer(vertexCount, sizeof(float) * 3);
        vertexBuffer.SetData(vertices);

        heightBuffer = new ComputeBuffer(vertexCount, sizeof(float));

        RunComputeShader();
    }

    void RunComputeShader()
    {
        int kernel = heightMapCompute.FindKernel("CSMain");
        if (kernel < 0)
        {
            Debug.LogError("Compute shader kernel 'CSMain' not found.");
            return;
        }

        heightMapCompute.SetBuffer(kernel, "vertices", vertexBuffer);
        heightMapCompute.SetBuffer(kernel, "heights", heightBuffer);
        heightMapCompute.SetFloat("planetRadius", planetRadius);
        heightMapCompute.SetInt("vertexCount", vertexCount);
        heightMapCompute.SetInt("numNoiseLayers", 0);  // Disable noise layers for this test

        int threadGroups = Mathf.CeilToInt(vertexCount / 64f);
        heightMapCompute.Dispatch(kernel, threadGroups, 1, 1);

        heights = new float[vertexCount];
        heightBuffer.GetData(heights);

        for (int i = 0; i < Mathf.Min(10, vertexCount); i++)
        {
            Debug.Log($"Vertex {i} height: {heights[i]} (expected ~{planetRadius})");
        }
    }

    private void OnDrawGizmos()
    {
        if (vertices == null || heights == null || vertices.Length != heights.Length)
            return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < Mathf.Min(50, vertices.Length); i++)
        {
            Vector3 pos = transform.position + vertices[i] * heights[i];
            Gizmos.DrawSphere(pos, 0.05f);
        }
    }

    private void OnDestroy()
    {
        if (vertexBuffer != null)
        {
            vertexBuffer.Release();
            vertexBuffer = null;
        }
        if (heightBuffer != null)
        {
            heightBuffer.Release();
            heightBuffer = null;
        }
    }
}
