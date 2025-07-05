using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

[CreateAssetMenu(fileName = "ShapeSettings", menuName = "Planet/ShapeSettings")]
public class ShapeSettings : ScriptableObject
{
    public ComputeShader heightMapCompute;
    public float planetRadius = 1f;
    public List<NoiseSettings> noiseLayers = new List<NoiseSettings>();

    // Match exactly to HLSL struct layout (48 bytes)
    [StructLayout(LayoutKind.Sequential)]
    struct NoiseLayerData
    {
        public int enabled;      // 4 bytes
        public float frequency;  // 4 bytes
        public float amplitude;  // 4 bytes
        public float persistence;// 4 bytes
        public int octaves;      // 4 bytes
        public Vector3 offset;   // 12 bytes
        public float padding;    // 4 bytes to align to 16 bytes
    }

    /// <summary>
    /// Uses the compute shader to calculate heights for the given vertices.
    /// </summary>
    /// <param name="vertexBuffer">ComputeBuffer of Vector3 vertex positions.</param>
    /// <returns>Array of heights corresponding to each vertex.</returns>
    public float[] CalculateHeights(ComputeBuffer vertexBuffer)
    {
        int vertexCount = vertexBuffer.count;
        if (vertexCount == 0)
        {
            Debug.LogWarning("ShapeSettings: Vertex buffer is empty.");
            return Array.Empty<float>();
        }

        if (heightMapCompute == null)
        {
            Debug.LogWarning("ShapeSettings: HeightMap compute shader is not assigned.");
            return Array.Empty<float>();
        }

        float[] heights = new float[vertexCount];
        int kernel = heightMapCompute.FindKernel("CSMain");
        if (kernel < 0)
        {
            Debug.LogError("ShapeSettings: 'CSMain' kernel not found in compute shader.");
            return Array.Empty<float>();
        }

        using (ComputeBuffer heightBuffer = new ComputeBuffer(vertexCount, sizeof(float)))
        {
            heightMapCompute.SetBuffer(kernel, "vertices", vertexBuffer);
            heightMapCompute.SetBuffer(kernel, "heights", heightBuffer);
            heightMapCompute.SetFloat("planetRadius", planetRadius);
            heightMapCompute.SetInt("vertexCount", vertexCount);

            int noiseCount = noiseLayers.Count;
            ComputeBuffer noiseBuffer = null;

            if (noiseCount > 0)
            {
                NoiseLayerData[] data = new NoiseLayerData[noiseCount];
                for (int i = 0; i < noiseCount; i++)
                {
                    var layer = noiseLayers[i];
                    data[i].enabled = layer.enabled ? 1 : 0;
                    data[i].frequency = layer.frequency;
                    data[i].amplitude = layer.amplitude;
                    data[i].persistence = layer.persistence;
                    data[i].octaves = layer.octaves;
                    data[i].offset = layer.offset;
                    data[i].padding = 0f; // alignment padding
                }

                noiseBuffer = new ComputeBuffer(noiseCount, Marshal.SizeOf(typeof(NoiseLayerData)));
                noiseBuffer.SetData(data);
                heightMapCompute.SetBuffer(kernel, "noiseLayers", noiseBuffer);
                heightMapCompute.SetInt("numNoiseLayers", noiseCount);
            }
            else
            {
                heightMapCompute.SetInt("numNoiseLayers", 0);
            }

            int threadGroups = Mathf.Max(1, Mathf.CeilToInt(vertexCount / 64f));
            heightMapCompute.Dispatch(kernel, threadGroups, 1, 1);

            heightBuffer.GetData(heights);
            noiseBuffer?.Release();
        }

        return heights;
    }
}
