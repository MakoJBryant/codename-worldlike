using SolarSystem.Planets.Generation.Coloring;
using SolarSystem.Planets.Settings;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SolarSystem.Planets.Generation
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class PlanetGenerator : MonoBehaviour
    {
        public ShapeSettings shapeSettings;
        public ColorSettings colorSettings;

        [Range(2, 256)]
        public int resolution = 10;

        private ShapeGenerator shapeGenerator;
        private ColorGenerator colorGenerator;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh mesh;

        private TerrainFace[] terrainFaces;

        private static readonly Vector3[] directions = {
            Vector3.up, Vector3.down,
            Vector3.left, Vector3.right,
            Vector3.forward, Vector3.back
        };

        void Awake()
        {
            Initialize();
            GeneratePlanet();
        }

        private void Initialize()
        {
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();

            shapeGenerator = new ShapeGenerator(shapeSettings);
            colorGenerator = new ColorGenerator(colorSettings);

            if (terrainFaces == null || terrainFaces.Length != 6)
            {
                terrainFaces = new TerrainFace[6];
                for (int i = 0; i < 6; i++)
                {
                    terrainFaces[i] = new TerrainFace(shapeGenerator, directions[i], resolution);
                }
            }
        }

        public void GeneratePlanet()
        {
            CombineInstance[] combine = new CombineInstance[6];

            for (int i = 0; i < 6; i++)
            {
                Mesh faceMesh = terrainFaces[i].ConstructMesh();
                combine[i].mesh = faceMesh;
                combine[i].transform = Matrix4x4.identity;
            }

            mesh = new Mesh();
            mesh.name = "Planet Mesh";
            mesh.CombineMeshes(combine, true, false);

            // Recalculate normals for smooth shading
            mesh.RecalculateNormals();

            // Assign colors after normals are calculated (we'll update colors in next step)
            AssignColors();

            // Finally assign mesh to meshFilter
            meshFilter.sharedMesh = mesh;
        }


        private void AssignColors()
        {
            Color[] colors = new Color[mesh.vertexCount];
            float planetRadius = shapeSettings.radius;

            for (int i = 0; i < mesh.vertexCount; i++)
            {
                Vector3 vertex = mesh.vertices[i];
                colors[i] = colorGenerator.CalculateColor(vertex, planetRadius);
            }

            mesh.colors = colors;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (shapeSettings == null || colorSettings == null)
                return;

            EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    Initialize();
                    GeneratePlanet();
                }
            };
        }
#endif
    }
}
