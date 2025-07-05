using System.Collections.Generic;
using UnityEngine;

public class SphereMesh
{
    public Vector3[] Vertices { get; private set; }
    public int[] Triangles { get; private set; }

    public SphereMesh(int resolution)
    {
        CreateSphere(resolution);
    }

    void CreateSphere(int resolution)
    {
        // Simple icosphere or UV sphere generator
        // For simplicity, UV sphere here:

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        for (int lat = 0; lat <= resolution; lat++)
        {
            float a1 = Mathf.PI * lat / resolution;
            float sin1 = Mathf.Sin(a1);
            float cos1 = Mathf.Cos(a1);

            for (int lon = 0; lon <= resolution; lon++)
            {
                float a2 = 2 * Mathf.PI * lon / resolution;
                float sin2 = Mathf.Sin(a2);
                float cos2 = Mathf.Cos(a2);

                Vector3 v = new Vector3(sin1 * cos2, cos1, sin1 * sin2);
                verts.Add(v);
            }
        }

        for (int lat = 0; lat < resolution; lat++)
        {
            for (int lon = 0; lon < resolution; lon++)
            {
                int current = lat * (resolution + 1) + lon;
                int next = current + resolution + 1;

                tris.Add(current);
                tris.Add(next + 1);
                tris.Add(next);

                tris.Add(current);
                tris.Add(current + 1);
                tris.Add(next + 1);
            }
        }

        Vertices = verts.ToArray();
        Triangles = tris.ToArray();
    }
}
