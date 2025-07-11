using UnityEngine;

namespace MakoJBryant.SolarSystem.Generation // Namespace for generation-related classes
{
    // This class is a static helper, meaning you won't attach it to a GameObject.
    // Its methods can be called directly from other scripts.
    public static class SphereCreator
    {
        // A struct to hold the data for a single face of the cube-sphere.
        // This makes it easier to pass around the necessary information for each face.
        public struct Face
        {
            public Vector3 localUp; // The 'up' direction for this face (e.g., Vector3.up, Vector3.down)
            public Vector3 axisA;   // One of the horizontal axes for this face (e.g., Vector3.right)
            public Vector3 axisB;   // The other horizontal axis for this face (e.g., Vector3.forward)
        }

        /// <summary>
        /// Generates the mesh data (vertices and triangles) for a cube-sphere.
        /// A cube-sphere is created by subdividing the faces of a cube and then normalizing
        /// the vertices to push them onto the surface of a sphere. This results in a more
        /// uniform distribution of vertices compared to a standard UV sphere.
        /// </summary>
        /// <param name="resolution">The number of subdivisions per face. Higher values mean more detail.</param>
        /// <param name="radius">The radius of the generated sphere.</param>
        /// <param name="vertices">Output array for the generated vertex positions.</param>
        /// <param name="triangles">Output array for the generated triangle indices.</param>
        /// <param name="uvs">Output array for the generated UV coordinates (basic for now).</param>
        public static void CreateSphereMesh(int resolution, float radius, out Vector3[] vertices, out int[] triangles, out Vector2[] uvs)
        {
            // Calculate the total number of vertices needed for all 6 faces.
            // Each face is a (resolution + 1) x (resolution + 1) grid of vertices.
            int numVerticesPerFace = (resolution + 1) * (resolution + 1);
            vertices = new Vector3[numVerticesPerFace * 6]; // 6 faces
            uvs = new Vector2[numVerticesPerFace * 6];      // UVs for each vertex

            // Calculate the total number of triangle indices needed for all 6 faces.
            // Each face has resolution * resolution quads.
            // Each quad has 2 triangles. Each triangle has 3 indices.
            int numTrianglesPerFace = resolution * resolution * 2;
            triangles = new int[numTrianglesPerFace * 3 * 6]; // 6 faces, 3 indices per triangle

            // --- CORRECTED FACE DEFINITIONS ---
            // These definitions ensure that for each face, Vector3.Cross(axisA, axisB) points
            // in the same direction as localUp. This creates a consistent right-handed
            // coordinate system for the grid on each face, leading to consistent outward-facing normals
            // when combined with the standard triangle winding order.
            Face[] faces = new Face[]
            {
                // localUp         axisA (X-direction on face)  axisB (Y-direction on face)
                new Face { localUp = Vector3.up,      axisA = Vector3.forward,  axisB = Vector3.right }, // For Top face (+Y): X moves Forward, Y moves Right
                new Face { localUp = Vector3.down,    axisA = Vector3.back,     axisB = Vector3.right }, // For Bottom face (-Y): X moves Back, Y moves Right
                new Face { localUp = Vector3.left,    axisA = Vector3.forward,  axisB = Vector3.up },    // For Left face (-X): X moves Forward, Y moves Up
                new Face { localUp = Vector3.right,   axisA = Vector3.back,     axisB = Vector3.up },    // For Right face (+X): X moves Back, Y moves Up
                new Face { localUp = Vector3.forward, axisA = Vector3.right,    axisB = Vector3.up },    // For Front face (+Z): X moves Right, Y moves Up
                new Face { localUp = Vector3.back,    axisA = Vector3.left,     axisB = Vector3.up }     // For Back face (-Z): X moves Left, Y moves Up
            };
            // --- END CORRECTED FACE DEFINITIONS ---

            int vertexIndex = 0;    // Current index for adding vertices to the 'vertices' array
            int triangleIndex = 0; // Current index for adding triangle indices to the 'triangles' array

            // Loop through each face and generate its mesh data.
            foreach (Face face in faces)
            {
                // Create the vertices and triangles for the current face.
                CreateFace(face, resolution, radius, vertices, triangles, uvs, ref vertexIndex, ref triangleIndex);
            }

            Debug.Log($"SphereCreator: Generated {vertices.Length} vertices and {triangles.Length / 3} triangles.");
        }

        /// <summary>
        /// Generates the vertices, triangles, and UVs for a single face of the cube-sphere.
        /// </summary>
        private static void CreateFace(Face face, int resolution, float radius, Vector3[] vertices, int[] triangles, Vector2[] uvs, ref int vertexIndex, ref int triangleIndex)
        {
            // Store the starting vertex index for this face.
            // This is used to calculate local indices for triangles within this face.
            int currentFaceVertexStart = vertexIndex;

            // Loop to generate vertices for this face.
            for (int y = 0; y <= resolution; y++)
            {
                for (int x = 0; x <= resolution; x++)
                {
                    // Calculate percentage across the face (0 to 1).
                    Vector2 percent = new Vector2(x, y) / resolution;

                    // Calculate a point on the unit cube face.
                    // (percent.x - 0.5f) * 2: Maps 0-1 to -1 to 1.
                    Vector3 pointOnUnitCube = face.localUp + (percent.x - 0.5f) * 2 * face.axisA + (percent.y - 0.5f) * 2 * face.axisB;

                    // Normalize the point to push it onto the surface of a unit sphere, then scale by radius.
                    vertices[vertexIndex] = pointOnUnitCube.normalized * radius;

                    // Assign basic UV coordinates (0-1 range across the face).
                    uvs[vertexIndex] = percent;

                    vertexIndex++; // Move to the next vertex slot
                }
            }

            // Loop to generate triangles for this face.
            // We iterate up to resolution - 1 because we're forming quads from vertices.
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    // Calculate the index of the bottom-left vertex of the current quad.
                    int i = currentFaceVertexStart + y * (resolution + 1) + x;

                    // Define the two triangles that form the quad.
                    // Standard Clockwise Winding for a quad (BL, BR, TL) and (BR, TR, TL)
                    // This winding order, combined with the new `axisA`/`axisB` definitions,
                    // should result in consistently outward-pointing normals for all faces.

                    // First triangle: Bottom-Left, Bottom-Right, Top-Left
                    triangles[triangleIndex] = i;                   // Vertex at (x,y)
                    triangles[triangleIndex + 1] = i + 1;           // Vertex at (x+1,y)
                    triangles[triangleIndex + 2] = i + resolution + 1; // Vertex at (x,y+1)

                    // Second triangle: Bottom-Right, Top-Right, Top-Left
                    triangles[triangleIndex + 3] = i + 1;               // Vertex at (x+1,y)
                    triangles[triangleIndex + 4] = i + resolution + 2; // Vertex at (x+1,y+1)
                    triangles[triangleIndex + 5] = i + resolution + 1; // Vertex at (x,y+1)

                    triangleIndex += 6; // Move to the next 6 slots for the next quad's triangles
                }
            }
        }
    }
}
