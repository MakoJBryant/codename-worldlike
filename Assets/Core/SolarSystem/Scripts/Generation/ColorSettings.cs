using UnityEngine;

namespace MakoJBryant.SolarSystem.Generation
{
    [CreateAssetMenu(fileName = "New Color Settings", menuName = "Solar System/Color Settings")]
    public class ColorSettings : ScriptableObject
    {
        // --- Planet Surface & Ocean Materials/Colors ---
        public Material planetMaterial; // Reference to your S_PlanetSurface material
        public Material oceanMaterial;  // Reference to your OceanMaterial
        public Color oceanColor;        // Base ocean color (still used for biome texture)

        public Biome[] biomes;

        [System.Serializable]
        public struct Biome
        {
            public string name;
            public Color color;
            [Range(0, 1)]
            public float startHeight;
            [Range(0, 1)]
            public float blendAmount;
        }

        // --- Atmosphere Settings (NEW) ---
        [Header("Atmosphere Settings")]
        public Material atmosphereMaterial; // Reference to your AtmosphereMaterial
        public Color atmosphereColor = new Color(0.2f, 0.4f, 1.0f, 1.0f); // Default light blue
        [Range(0.1f, 10.0f)]
        public float atmosphereDensity = 1.0f; // Overall intensity/opacity
        [Range(1.0f, 50.0f)]
        public float atmospherePower = 5.0f; // Sharpness of alpha falloff (controls falloff steepness)
        [Range(0.0f, 1.0f)]
        public float atmosphereAmbientLightInfluence = 0.1f; // Base ambient light (for dark side)
        [Range(1.0f, 20.0f)]
        public float atmosphereRimPower = 3.0f; // Sharpness of rim glow
    }
}
