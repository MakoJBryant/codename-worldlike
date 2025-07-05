using UnityEngine;

[CreateAssetMenu(fileName = "ShapeSettings", menuName = "Planet/ShapeSettings")]
public class ShapeSettings : ScriptableObject
{
    public ComputeShader heightMapCompute;
    public float planetRadius = 1f;

    // Calculates heights for vertices using compute shader
    public float[] CalculateHeights(ComputeBuffer vertexBuffer)
    {
        int vertexCount = vertexBuffer.count;
        float[] heights = new float[vertexCount];

        int kernel = heightMapCompute.FindKernel("CSMain");

        heightMapCompute.SetBuffer(kernel, "vertices", vertexBuffer);
        ComputeBuffer heightBuffer = new ComputeBuffer(vertexCount, sizeof(float));
        heightMapCompute.SetBuffer(kernel, "heights", heightBuffer);

        heightMapCompute.SetFloat("planetRadius", planetRadius);

        // IMPORTANT: set vertexCount for compute shader bounds check
        heightMapCompute.SetInt("vertexCount", vertexCount);

        heightMapCompute.Dispatch(kernel, Mathf.CeilToInt(vertexCount / 64f), 1, 1);

        heightBuffer.GetData(heights);
        heightBuffer.Release();

        return heights;
    }

    public void ReleaseBuffers()
    {
        // Placeholder for cleanup if needed
    }
}
