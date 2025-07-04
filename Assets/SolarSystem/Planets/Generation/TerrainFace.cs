using UnityEngine;

namespace SolarSystem.Planets.Generation
{
    public class TerrainFace
    {
        private ShapeGenerator shapeGenerator;
        private Vector3 localUp;
        private Vector3 axisA;
        private Vector3 axisB;
        private int resolution;

        public TerrainFace(ShapeGenerator shapeGenerator, Vector3 localUp, int resolution)
        {
            this.shapeGenerator = shapeGenerator;
            this.localUp = localUp;
            this.resolution = resolution;

            axisA = new Vector3(localUp.y, localUp.z, localUp.x);
            axisB = Vector3.Cross(localUp, axisA);
        }

        public Mesh ConstructMesh()
        {
            Mesh mesh = new Mesh();
            Vector3[] vertices = new Vector3[resolution * resolution];
            int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];
            Vector3[] normals = new Vector3[vertices.Length];
            Color[] colors = new Color[vertices.Length];

            int triIndex = 0;
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int i = x + y * resolution;
                    // Calculate percent across this face from 0 to 1
                    Vector2 percent = new Vector2(x, y) / (resolution - 1);
                    // Point on unit cube face
                    Vector3 pointOnUnitCube = localUp
                        + (percent.x - 0.5f) * 2 * axisA
                        + (percent.y - 0.5f) * 2 * axisB;

                    Vector3 pointOnUnitSphere = pointOnUnitCube.normalized;
                    vertices[i] = shapeGenerator.CalculatePointOnPlanet(pointOnUnitSphere);
                    normals[i] = pointOnUnitSphere;

                    // Triangles except last row/column
                    if (x != resolution - 1 && y != resolution - 1)
                    {
                        triangles[triIndex] = i;
                        triangles[triIndex + 1] = i + resolution + 1;
                        triangles[triIndex + 2] = i + resolution;

                        triangles[triIndex + 3] = i;
                        triangles[triIndex + 4] = i + 1;
                        triangles[triIndex + 5] = i + resolution + 1;
                        triIndex += 6;
                    }
                }
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.normals = normals;

            return mesh;
        }
    }
}
