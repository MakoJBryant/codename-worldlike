using UnityEngine;

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
        uvs = new Vector2[numVerticesPerFace * 6];     // UVs for each vertex

        // Calculate the total number of triangle indices needed for all 6 faces.
        // Each face has resolution * resolution quads.
        // Each quad has 2 triangles. Each triangle has 3 indices.
        int numTrianglesPerFace = resolution * resolution * 2;
        triangles = new int[numTrianglesPerFace * 3 * 6]; // 6 faces, 3 indices per triangle

        // Define the 6 faces of the cube.
        // localUp: The 'normal' direction of the face.
        // axisA, axisB: The two perpendicular axes that define the plane of the face.
        Face[] faces = new Face[]
        {
            new Face { localUp = Vector3.up, axisA = Vector3.right, axisB = Vector3.forward },
            new Face { localUp = Vector3.down, axisA = Vector3.right, axisB = Vector3.back },
            new Face { localUp = Vector3.left, axisA = Vector3.forward, axisB = Vector3.up },
            new Face { localUp = Vector3.right, axisA = Vector3.back, axisB = Vector3.up },
            new Face { localUp = Vector3.forward, axisA = Vector3.up, axisB = Vector3.right },
            new Face { localUp = Vector3.back, axisA = Vector3.up, axisB = Vector3.left } // Corrected axisB for back face
        };

        int vertexIndex = 0;   // Current index for adding vertices to the 'vertices' array
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
                // The winding order (clockwise/counter-clockwise) is crucial for rendering.
                // If your mesh doesn't show up, try swapping two indices in each triangle.
                // This order (0, 1, 2) and (0, 2, 3) for a quad like this:
                // 2 -- 3
                // |    |
                // 0 -- 1
                // Usually works for front-facing normals.

                // First triangle (bottom-left, top-left, top-right)
                triangles[triangleIndex] = i;                   // Bottom-left
                triangles[triangleIndex + 1] = i + resolution + 1; // Top-left (vertex on next row)
                triangles[triangleIndex + 2] = i + 1;           // Bottom-right

                // Second triangle (top-right, top-left, bottom-right of next row)
                triangles[triangleIndex + 3] = i + 1;           // Bottom-right
                triangles[triangleIndex + 4] = i + resolution + 1; // Top-left
                triangles[triangleIndex + 5] = i + resolution + 2; // Top-right (vertex on next row, next column)

                triangleIndex += 6; // Move to the next 6 slots for the next quad's triangles
            }
        }
    }
}