using UnityEngine;
using SolarSystem.Planets.Settings;
using SolarSystem.Planets.Noise;

namespace SolarSystem.Planets.Generation
{
    public class ShapeGenerator
    {
        private ShapeSettings settings;
        private INoiseFilter[] noiseFilters;

        public float Radius => settings.radius;

        public ShapeGenerator(ShapeSettings settings)
        {
            this.settings = settings;
            noiseFilters = new INoiseFilter[settings.noiseLayers.Length];

            for (int i = 0; i < noiseFilters.Length; i++)
            {
                noiseFilters[i] = NoiseFilterFactory.CreateNoiseFilter(settings.noiseLayers[i].noiseSettings);
            }
        }

        public float CalculatePointElevation(Vector3 pointOnUnitSphere)
        {
            float firstLayerValue = 0f;
            float elevation = 0f;

            if (noiseFilters.Length > 0)
            {
                if (settings.noiseLayers[0].enabled)
                {
                    firstLayerValue = noiseFilters[0].Evaluate(pointOnUnitSphere);
                    elevation = settings.noiseLayers[0].baseHeight + firstLayerValue * settings.noiseLayers[0].strength;
                }

                for (int i = 1; i < noiseFilters.Length; i++)
                {
                    if (settings.noiseLayers[i].enabled)
                    {
                        float mask = settings.noiseLayers[i].useFirstLayerAsMask ? firstLayerValue : 1f;
                        float noiseValue = noiseFilters[i].Evaluate(pointOnUnitSphere);
                        elevation += (settings.noiseLayers[i].baseHeight + noiseValue * settings.noiseLayers[i].strength) * mask;
                    }
                }
            }

            return elevation;
        }

        public Vector3 CalculatePointOnPlanet(Vector3 pointOnUnitSphere)
        {
            float elevation = CalculatePointElevation(pointOnUnitSphere);
            return pointOnUnitSphere * settings.radius * (1 + elevation);
        }
    }
}
