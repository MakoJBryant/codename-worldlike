// File: Assets/Scripts/Generation/ColorSettings.cs

using UnityEngine;

// [System.Serializable] makes this class's public fields visible and editable in the Unity Inspector
[System.Serializable]
public class ColorSettings
{
    // The material that will be applied to the planet and receive the color data
    public Material planetMaterial;

    // Array of biomes to define different color zones
    public Biome[] biomes;

    // Settings for ocean color and depth (if you want separate ocean rendering later, this helps)
    public Color oceanColor = Color.blue; // Basic ocean color for now

    // Inner serializable class for defining each biome's properties
    [System.Serializable]
    public class Biome
    {
        public string name; // For easier identification in the Inspector
        public Color color; // The color of this biome
        [Range(0, 1)]
        public float startHeight; // Normalized height (0-1) at which this biome begins
        [Range(0, 1)]
        public float blendAmount; // How much to blend with the previous biome's color
    }
}