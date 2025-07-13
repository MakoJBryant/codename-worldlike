using UnityEngine;

namespace MakoJBryant.SolarSystem.Generation // Namespace for noise-related classes
{
    [CreateAssetMenu(fileName = "New Planet Preset", menuName = "Solar System/Planet Preset")]
    public class PlanetPreset : ScriptableObject
    {
        [Header("Core Planet Settings")]
        [Range(2, 256)]
        public int resolution = 64;
        public float radius = 1f;

        [Header("Orbital Settings")]
        [Tooltip("The distance from the central star (Sun) this planet will be positioned at.")]
        public float orbitalRadius = 0f; // New field for orbital distance

        [Header("Assigned Settings Assets")]
        // References to the Shape and Color settings for this specific planet preset
        public ShapeSettings shapeSettings;
        public ColorSettings colorSettings;

        [Header("Prefab References (Optional)")]
        // Optional: If you want to use a specific base prefab for planets, though we're generating meshes
        // This might be useful for attaching other components or specific render pipelines
        public GameObject planetPrefab;
    }
}
