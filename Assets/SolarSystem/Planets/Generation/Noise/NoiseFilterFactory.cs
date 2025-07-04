using SolarSystem.Planets.Settings;
using SolarSystem.Planets.Generation.Noise;

namespace SolarSystem.Planets.Noise
{
    public static class NoiseFilterFactory
    {
        public static INoiseFilter CreateNoiseFilter(NoiseSettings settings)
        {
            // Add support for other types later
            return new SimpleNoiseFilter(settings);
        }
    }
}
