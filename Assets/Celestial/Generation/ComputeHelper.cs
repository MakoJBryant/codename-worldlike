using UnityEngine;

public static class ComputeHelper
{
    public static void CreateStructuredBuffer<T>(ref ComputeBuffer buffer, T[] data) where T : struct
    {
        if (buffer != null)
        {
            buffer.Release();
            buffer = null;
        }

        if (data == null || data.Length == 0)
        {
            Debug.LogWarning("ComputeHelper.CreateStructuredBuffer: Data array is null or empty.");
            return;
        }

        int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
        buffer = new ComputeBuffer(data.Length, stride);
        buffer.SetData(data);
    }

    public static void Release(ref ComputeBuffer buffer)
    {
        if (buffer != null)
        {
            buffer.Release();
            buffer = null;
        }
    }
}
