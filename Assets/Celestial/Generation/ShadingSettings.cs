using UnityEngine;

[CreateAssetMenu(fileName = "ShadingSettings", menuName = "Planet/ShadingSettings")]
public class ShadingSettings : ScriptableObject
{
    public Material terrainMaterial;
    public ComputeShader shadingCompute;

    public bool hasOcean = false;
    public float oceanLevel = 0.5f;

    // Initialize or set any shader properties based on ShapeSettings if needed
    public void Initialize(ShapeSettings shapeSettings)
    {
        // Example: pass parameters from ShapeSettings if needed
    }

    public Vector4[] GenerateShadingData(ComputeBuffer vertexBuffer)
    {
        if (shadingCompute == null || vertexBuffer == null)
            return null;

        int vertexCount = vertexBuffer.count;
        Vector4[] shadingData = new Vector4[vertexCount];

        int kernel = shadingCompute.FindKernel("CSMain");

        shadingCompute.SetBuffer(kernel, "vertices", vertexBuffer);

        ComputeBuffer shadingBuffer = new ComputeBuffer(vertexCount, sizeof(float) * 4);
        shadingCompute.SetBuffer(kernel, "shadingData", shadingBuffer);

        shadingCompute.SetInt("vertexCount", vertexCount);
        // Set more shader params here if needed

        shadingCompute.Dispatch(kernel, Mathf.CeilToInt(vertexCount / 64f), 1, 1);

        shadingBuffer.GetData(shadingData);
        shadingBuffer.Release();

        return shadingData;
    }

    public void SetTerrainProperties(Material material, Vector2 heightMinMax, float bodyScale)
    {
        if (material == null)
            return;

        material.SetFloat("_MinHeight", heightMinMax.x);
        material.SetFloat("_MaxHeight", heightMinMax.y);
        material.SetFloat("_BodyScale", bodyScale);
        material.SetFloat("_OceanLevel", oceanLevel);
        // Set other material properties as needed
    }

    public void ReleaseBuffers()
    {
        // Cleanup compute buffers if any
    }
}
