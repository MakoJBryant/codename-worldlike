using UnityEngine;

namespace MakoJBryant.SolarSystem.Generation
{
    [CreateAssetMenu(fileName = "New Color Settings", menuName = "Solar System/Color Settings")]
    public class ColorSettings : ScriptableObject
    {
        public Material planetMaterial; // Reference to your S_PlanetSurface material
        public Material oceanMaterial;  // NEW: Reference to your OceanMaterial
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
    }
}