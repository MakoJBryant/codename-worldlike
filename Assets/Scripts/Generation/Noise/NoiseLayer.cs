// File: Assets/Scripts/Generation/NoiseLayer.cs

using UnityEngine;

// [System.Serializable] makes this class's public fields visible and editable in the Unity Inspector
[System.Serializable]
public class NoiseLayer
{
    public bool enabled = true; // Allows you to easily toggle a noise layer on/off

    // Core FBM parameters for this layer
    public float strength = 1f;    // Overall multiplier for this layer's noise
    public float roughness = 1f;   // Base frequency for this layer. Higher = smaller, more frequent features.
    [Range(1, 8)]
    public int octaves = 4;        // Number of FBM iterations for this layer
    [Range(0.01f, 1.0f)]
    public float persistence = 0.5f; // How much amplitude decreases with each octave
    [Range(1.0f, 4.0f)]
    public float lacunarity = 2.0f;  // How much frequency increases with each octave
    public Vector3 offset;         // Unique offset for THIS noise layer to break symmetry

    // Noise type specific settings
    public NoiseType noiseType = NoiseType.Standard; // Defaults to standard FBM
    public float minValue = 0; // A baseline value added to the noise. Useful for pushing land above "sea level" or creating plateaus.

    // Masking option (advanced, for creating continents, etc.)
    public bool useFirstLayerAsMask = false; // If true, this layer's noise is only applied where the first layer's noise is positive.
}

// Enum to define different types of noise effects
public enum NoiseType
{
    Standard, // Regular FBM (Perlin/Simplex)
    Ridge     // Modified FBM to create sharp, creased features (like mountains)
    // You could add Billow, etc., later
}