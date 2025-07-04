using UnityEngine;

namespace SolarSystem.Planets.Generation.Noise
{
    public static class Noise
    {
        public static float Evaluate(Vector3 point)
        {
            // Combine six 2D Perlin calls to simulate 3D noise
            float xy = Mathf.PerlinNoise(point.x, point.y);
            float yz = Mathf.PerlinNoise(point.y, point.z);
            float xz = Mathf.PerlinNoise(point.x, point.z);

            float yx = Mathf.PerlinNoise(point.y, point.x);
            float zy = Mathf.PerlinNoise(point.z, point.y);
            float zx = Mathf.PerlinNoise(point.z, point.x);

            // Average them and remap from [0,1] to [-1,1]
            float avg = (xy + yz + xz + yx + zy + zx) / 6f;
            return avg * 2f - 1f;
        }
    }
}
