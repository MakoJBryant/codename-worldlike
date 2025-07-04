using SolarSystem.Planets.Noise;
using SolarSystem.Planets.Settings;
using UnityEngine;

namespace SolarSystem.Planets.Generation.Noise
{
    public class SimpleNoiseFilter : INoiseFilter
    {
        private readonly NoiseSettings settings;

        public SimpleNoiseFilter(NoiseSettings settings)
        {
            this.settings = settings;
        }

        public float Evaluate(Vector3 point)
        {
            float noiseValue = 0;
            float frequency = settings.baseRoughness;
            float amplitude = 1;

            for (int i = 0; i < settings.numLayers; i++)
            {
                float v = Noise.Evaluate(point * frequency + settings.center);
                noiseValue += (v + 1f) * 0.5f * amplitude;

                frequency *= settings.roughness;
                amplitude *= settings.persistence;
            }

            noiseValue = Mathf.Max(0, noiseValue - settings.minValue);
            return noiseValue * settings.strength;
        }
    }
}
