using UnityEngine; // Required for Vector3 and Range

namespace MakoJBryant.SolarSystem.Generation
{
    // Enum for different noise types (Standard Perlin, Ridge)
    public enum NoiseType { Standard, Ridge }

    // Make this struct serializable so it appears in the Inspector
    [System.Serializable]
    public struct NoiseLayer
    {
        public bool enabled;        // Whether this noise layer is active
        public float strength;      // How much this layer displaces the surface
        public float roughness;     // Frequency of the noise (higher = more jagged)
        public int octaves;         // Number of noise layers combined (more = more detail)
        [Range(0, 1)]
        public float persistence;   // How much amplitude decreases with each octave
        public float lacunarity;    // How much frequency increases with each octave
        public Vector3 offset;      // Offset the noise sample point (for variety)
        public float minValue;      // Minimum possible output value of this noise layer
        public NoiseType noiseType; // Type of noise (Standard or Ridge)
        public bool useFirstLayerAsMask; // If true, this layer only applies where the first layer's value is positive
    }
}