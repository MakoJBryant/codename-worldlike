using UnityEngine;

namespace SolarSystem.Planets.Settings
{
    [CreateAssetMenu(fileName = "NoiseSettings", menuName = "SolarSystem/Planets/NoiseSettings")]
    public class NoiseSettings : ScriptableObject
    {
        public enum FilterType { Simple, Ridgid }
        public FilterType filterType;

        public float strength = 1f;
        public float baseRoughness = 1f;
        public float roughness = 2f;
        public float persistence = 0.5f;
        public float minValue = 0f;
        public int numLayers = 1;
        public Vector3 center;
    }
}
