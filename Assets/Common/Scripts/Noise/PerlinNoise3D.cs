using UnityEngine;

namespace MakoJBryant.SolarSystem.Generation // Namespace for noise-related classes
{
    public static class PerlinNoise3D
    {
        // This is a very basic 3D Perlin noise implementation for demonstration.
        // It's often better to use a dedicated noise library (e.g., FastNoiseLite)
        // for more advanced features like octaves, persistence, lacunarity, and better quality.
        // For a true 3D Perlin noise, you'd typically sample a 3D grid.
        // For now, we'll simulate it by combining multiple 2D Perlin samples.
        // This is simplified but good enough to see initial results.

        /// <summary>
        /// Generates a pseudo 3D Perlin noise value based on a 3D position.
        /// This is a simple approximation by combining 2D noise planes.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="z">Z coordinate.</param>
        /// <returns>A float value between 0 and 1.</returns>
        public static float GenerateNoise(float x, float y, float z)
        {
            // Combine multiple 2D Perlin noise samples to get a rough 3D effect.
            // This is not a true 3D Perlin noise but works for basic spherical displacement.
            float xy = Mathf.PerlinNoise(x, y);
            float yz = Mathf.PerlinNoise(y, z);
            float xz = Mathf.PerlinNoise(x, z);

            float yx = Mathf.PerlinNoise(y, x);
            float zy = Mathf.PerlinNoise(z, y);
            float zx = Mathf.PerlinNoise(z, x);

            return (xy + yz + xz + yx + zy + zx) / 6f; // Average them to get a value between 0 and 1
        }
    }
}
