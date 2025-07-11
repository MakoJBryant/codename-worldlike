using UnityEngine;

// Mark as System.Serializable so it can be embedded directly in the Inspector
// of other MonoBehaviour scripts (like PlanetGenerator).
namespace MakoJBryant.SolarSystem.Generation // Namespace for noise-related classes
{
    // Enum to define different types of noise effects.
    public enum NoiseType
    {
        Standard, // Regular FBM (Perlin/Simplex)
        Ridge     // Modified FBM to create sharp, creased features (like mountains)
        // You could add Billow, etc., later
    }

    [System.Serializable]
    public class NoiseLayer
    {
        [Tooltip("Enable or disable this noise layer.")]
        public bool enabled = true; // Allows you to easily toggle a noise layer on/off

        [Tooltip("The strength or amplitude of this noise layer's effect on displacement.")]
        public float strength = 1f;    // Overall multiplier for this layer's noise

        [Tooltip("The initial frequency (roughness) of the noise. Higher = smaller, more frequent features.")]
        public float roughness = 1f;    // Base frequency for this layer. Higher = smaller, more frequent features.

        [Tooltip("The number of noise octaves. More octaves add more detail but increase computation time.")]
        [Range(1, 8)]
        public int octaves = 4;        // Number of FBM iterations for this layer

        [Tooltip("Persistence controls how quickly the amplitude decreases for each successive octave.")]
        [Range(0.01f, 1.0f)]
        public float persistence = 0.5f; // How much amplitude decreases with each octave

        [Tooltip("Lacunarity controls how quickly the frequency increases for each successive octave.")]
        [Range(1.0f, 4.0f)]
        public float lacunarity = 2.0f;  // How much frequency increases with each octave

        [Tooltip("A 3D offset applied to the noise sampling coordinates. Use to shift the noise pattern.")]
        public Vector3 offset;         // Unique offset for THIS noise layer to break symmetry

        [Tooltip("The type of noise to generate (Standard or Ridge).")]
        public NoiseType noiseType = NoiseType.Standard; // Defaults to standard FBM

        [Tooltip("A baseline value added to the noise. Useful for pushing land above 'sea level' or creating plateaus.")]
        public float minValue = 0; // A baseline value added to the noise. Useful for pushing land above "sea level" or creating plateaus.

        [Tooltip("If true, this layer's effect will be masked by the first enabled layer's value. " +
                 "Only applies if the first layer's value is positive.")]
        public bool useFirstLayerAsMask = false; // If true, this layer's noise is only applied where the first layer's noise is positive.
    }
}
