using SolarSystem.Planets.Settings;
using UnityEngine;

namespace SolarSystem.Planets.Generation.Coloring
{
    public class ColorGenerator
    {
        private readonly ColorSettings settings;

        private float minHeight;
        private float maxHeight;

        public ColorGenerator(ColorSettings settings)
        {
            this.settings = settings;

            // Initialize from settings, fallback to defaults if not set
            minHeight = settings.minHeight;
            maxHeight = settings.maxHeight;
        }

        /// <summary>
        /// Call this to update min/max height when mesh changes.
        /// </summary>
        public void UpdateMinMaxHeights(float minHeight, float maxHeight)
        {
            this.minHeight = minHeight;
            this.maxHeight = maxHeight;
        }

        /// <summary>
        /// Calculate normalized elevation [0..1] from height (absolute)
        /// </summary>
        private float NormalizeElevation(float height)
        {
            return Mathf.InverseLerp(minHeight, maxHeight, height);
        }

        /// <summary>
        /// Main method to get color for a vertex based on elevation
        /// </summary>
        /// <param name="point">Vertex position in local space</param>
        /// <param name="planetRadius">Base radius of planet</param>
        /// <returns>Color to apply</returns>
        public Color CalculateColor(Vector3 point, float planetRadius)
        {
            float height = point.magnitude - planetRadius;
            float elevation = NormalizeElevation(height);
            Color gradientColor = settings.gradient.Evaluate(elevation);
            return Color.Lerp(gradientColor, settings.tintColor, settings.tintPercent);
        }
    }
}
