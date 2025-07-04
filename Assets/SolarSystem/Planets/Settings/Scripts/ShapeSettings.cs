using UnityEngine;

namespace SolarSystem.Planets.Settings
{
    [System.Serializable]
    public class NoiseLayer
    {
        public bool enabled = true;
        public bool useFirstLayerAsMask = false;
        public NoiseSettings noiseSettings;

        [Range(0f, 5f)]
        public float strength = 1f;       // How much this layer affects elevation

        [Range(-1f, 1f)]
        public float baseHeight = 0f;     // Base offset added before noise scaling
    }

    [CreateAssetMenu(fileName = "ShapeSettings", menuName = "SolarSystem/Planets/ShapeSettings")]
    public class ShapeSettings : ScriptableObject
    {
        public float radius = 1f;
        public NoiseLayer[] noiseLayers;
    }
}
