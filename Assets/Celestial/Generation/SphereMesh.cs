using System.Collections.Generic;
using UnityEngine;

public class SphereMesh
{
    public Vector3[] Vertices { get; private set; }
    public int[] Triangles { get; private set; }

    /// <summary>
    /// Constructs a UV sphere mesh with the given resolution.
    /// </summary>
    /// <param name="resolution">Number of subdivisions along latitude and longitude. Minimum 2 recommended.</param>
    public SphereMesh(int resolution)
    {
        if (resolution < 2)
            resolution = 2;  // Avoid degenerate sphere

        CreateSphere(resolution);
    }

    /// <summary>
    /// Generates a UV sphere mesh (unit radius, centered at origin).
    /// </summary>
    /// <param name="resolution">Number of latitude and longitude subdivisions.</param>
    void CreateSphere(int resolution)
    {
        var verts = new List<Vector3>();
        var tris = new List<int>();

        // Generate vertices
        for (int lat = 0; lat <= resolution; lat++)
        {
            float a1 = Mathf.PI * lat / resolution;
            float sin1 = Mathf.Sin(a1);
            float cos1 = Mathf.Cos(a1);

            for (int lon = 0; lon <= resolution; lon++)
            {
                float a2 = 2f * Mathf.PI * lon / resolution;
                float sin2 = Mathf.Sin(a2);
                float cos2 = Mathf.Cos(a2);

                Vector3 v = new Vector3(sin1 * cos2, cos1, sin1 * sin2);
                verts.Add(v);
            }
        }

        // Generate triangles
        for (int lat = 0; lat < resolution; lat++)
        {
            for (int lon = 0; lon < resolution; lon++)
            {
                int current = lat * (resolution + 1) + lon;
                int next = current + resolution + 1;

                // First triangle of quad
                tris.Add(current);
                tris.Add(next + 1);
                tris.Add(next);

                // Second triangle of quad
                tris.Add(current);
                tris.Add(current + 1);
                tris.Add(next + 1);
            }
        }

        Vertices = verts.ToArray();
        Triangles = tris.ToArray();
    }
}
