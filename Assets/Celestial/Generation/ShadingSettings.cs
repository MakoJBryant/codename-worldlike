using UnityEngine;

[CreateAssetMenu(fileName = "ShadingSettings", menuName = "Planet/ShadingSettings")]
public class ShadingSettings : ScriptableObject
{
    public Material terrainMaterial;
    public ComputeShader shadingCompute;

    public bool hasOcean = false;
    public float oceanLevel = 0.5f;

    /// <summary>
    /// Optionally initialize shader parameters from ShapeSettings or other data.
    /// </summary>
    public void Initialize(ShapeSettings shapeSettings)
    {
        // TODO: Implement if needed
    }

    /// <summary>
    /// Runs the compute shader to generate shading data for vertices.
    /// </summary>
    /// <param name="vertexBuffer">Buffer of vertex positions.</param>
    /// <returns>Array of Vector4 shading data corresponding to vertices.</returns>
    public Vector4[] GenerateShadingData(ComputeBuffer vertexBuffer)
    {
        if (shadingCompute == null)
        {
            Debug.LogWarning("ShadingSettings: ComputeShader not assigned.");
            return null;
        }

        if (vertexBuffer == null)
        {
            Debug.LogWarning("ShadingSettings: Vertex buffer is null.");
            return null;
        }

        int vertexCount = vertexBuffer.count;
        Vector4[] shadingData = new Vector4[vertexCount];

        int kernel = shadingCompute.FindKernel("CSMain");
        if (kernel < 0)
        {
            Debug.LogError("ShadingSettings: Kernel 'CSMain' not found in compute shader.");
            return null;
        }

        shadingCompute.SetBuffer(kernel, "vertices", vertexBuffer);

        using (ComputeBuffer shadingBuffer = new ComputeBuffer(vertexCount, sizeof(float) * 4))
        {
            shadingCompute.SetBuffer(kernel, "shadingData", shadingBuffer);

            shadingCompute.SetInt("vertexCount", vertexCount);
            // Set additional compute shader parameters here as needed

            int threadGroups = Mathf.CeilToInt(vertexCount / 64f);
            shadingCompute.Dispatch(kernel, threadGroups, 1, 1);

            shadingBuffer.GetData(shadingData);
        }

        return shadingData;
    }

    /// <summary>
    /// Sets material shader properties related to terrain.
    /// </summary>
    public void SetTerrainProperties(Material material, Vector2 heightMinMax, float bodyScale)
    {
        if (material == null) return;

        material.SetFloat("_MinHeight", heightMinMax.x);
        material.SetFloat("_MaxHeight", heightMinMax.y);
        material.SetFloat("_BodyScale", bodyScale);
        material.SetFloat("_OceanLevel", oceanLevel);
        // Set other material properties as needed
    }

    public void ReleaseBuffers()
    {
        // Implement if you store and manage ComputeBuffers long-term
    }
}
