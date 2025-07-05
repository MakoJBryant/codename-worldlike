using UnityEngine;

public static class CubeSphereBuilder
{
    public static (Vector3[] vertices, int[] triangles) Generate(int resolution)
    {
        if (resolution < 1) resolution = 1;

        int verticesPerFace = (resolution + 1) * (resolution + 1);
        int totalVertices = verticesPerFace * 6;
        int totalTriangles = resolution * resolution * 6 * 6;

        Vector3[] vertices = new Vector3[totalVertices];
        int[] triangles = new int[totalTriangles];

        int vertIndex = 0;
        int triIndex = 0;

        Vector3[] directions = {
            Vector3.up, Vector3.down,
            Vector3.left, Vector3.right,
            Vector3.forward, Vector3.back
        };

        for (int face = 0; face < 6; face++)
        {
            Vector3 normal = directions[face];
            Vector3 axisA = new Vector3(normal.y, normal.z, normal.x);
            Vector3 axisB = Vector3.Cross(normal, axisA);

            for (int y = 0; y <= resolution; y++)
            {
                for (int x = 0; x <= resolution; x++)
                {
                    float percentX = x / (float)resolution;
                    float percentY = y / (float)resolution;

                    Vector3 pointOnUnitCube = normal
                        + (percentX - 0.5f) * 2f * axisA
                        + (percentY - 0.5f) * 2f * axisB;

                    Vector3 pointOnUnitSphere = pointOnUnitCube.normalized;

                    vertices[vertIndex++] = pointOnUnitSphere;
                }
            }
        }

        vertIndex = 0;
        for (int face = 0; face < 6; face++)
        {
            int faceVertexStart = face * verticesPerFace;
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int current = faceVertexStart + y * (resolution + 1) + x;
                    int next = current + resolution + 1;

                    // First triangle
                    triangles[triIndex++] = current;
                    triangles[triIndex++] = next + 1;
                    triangles[triIndex++] = next;

                    // Second triangle
                    triangles[triIndex++] = current;
                    triangles[triIndex++] = current + 1;
                    triangles[triIndex++] = next + 1;
                }
            }
        }

        return (vertices, triangles);
    }
}
