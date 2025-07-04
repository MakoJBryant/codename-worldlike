using SolarSystem.Planets.Settings;
using UnityEngine;

namespace SolarSystem.Planets.Generation.Coloring
{
    public class ColorGenerator
    {
        private readonly ColorSettings settings;

        public ColorGenerator(ColorSettings settings)
        {
            this.settings = settings;
        }

        public Color EvaluateColor(float elevation)
        {
            Color gradientColor = settings.gradient.Evaluate(elevation);
            return Color.Lerp(gradientColor, settings.tintColor, settings.tintPercent);
        }
    }
}
