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

        buffer = new ComputeBuffer(data.Length, System.Runtime.InteropServices.Marshal.SizeOf(typeof(T)));
        buffer.SetData(data);
    }

    public static void Release(ComputeBuffer buffer)
    {
        if (buffer != null)
        {
            buffer.Release();
            buffer = null;
        }
    }
}
