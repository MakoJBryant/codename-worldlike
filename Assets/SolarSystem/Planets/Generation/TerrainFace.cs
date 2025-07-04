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

        private Vector3[] vertices;
        private int[] triangles;
        private Color[] colors;

        private float planetRadius;

        public TerrainFace(ShapeGenerator shapeGenerator, Vector3 localUp, int resolution)
        {
            this.shapeGenerator = shapeGenerator;
            this.localUp = localUp;
            this.resolution = resolution;

            axisA = new Vector3(localUp.y, localUp.z, localUp.x);
            axisB = Vector3.Cross(localUp, axisA);

            planetRadius = shapeGenerator.Radius;
        }

        public Mesh ConstructMesh()
        {
            int vertsPerFace = (resolution + 1) * (resolution + 1);
            vertices = new Vector3[vertsPerFace];
            triangles = new int[resolution * resolution * 6];
            colors = new Color[vertsPerFace];

            int vertIndex = 0;
            int triIndex = 0;

            for (int y = 0; y <= resolution; y++)
            {
                for (int x = 0; x <= resolution; x++)
                {
                    Vector2 percent = new Vector2(x, y) / resolution;
                    Vector3 pointOnUnitCube = localUp + (percent.x - 0.5f) * 2f * axisA + (percent.y - 0.5f) * 2f * axisB;
                    Vector3 pointOnUnitSphere = pointOnUnitCube.normalized;

                    Vector3 pointOnPlanet = shapeGenerator.CalculatePointOnPlanet(pointOnUnitSphere);
                    vertices[vertIndex] = pointOnPlanet;

                    // Elevation-based vertex coloring
                    float elevation = pointOnPlanet.magnitude - planetRadius;
                    float t = Mathf.InverseLerp(-0.1f, 0.5f, elevation);
                    colors[vertIndex] = Color.Lerp(Color.blue, Color.green, t);

                    vertIndex++;
                }
            }

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int i = y * (resolution + 1) + x;

                    triangles[triIndex++] = i;
                    triangles[triIndex++] = i + resolution + 1;
                    triangles[triIndex++] = i + 1;

                    triangles[triIndex++] = i + 1;
                    triangles[triIndex++] = i + resolution + 1;
                    triangles[triIndex++] = i + resolution + 2;
                }
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.colors = colors;
            mesh.RecalculateNormals();

            return mesh;
        }
    }
}
