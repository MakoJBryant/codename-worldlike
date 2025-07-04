using UnityEngine;

namespace SolarSystem.Planets.Settings
{
    [CreateAssetMenu(fileName = "ColorSettings", menuName = "SolarSystem/Planets/ColorSettings")]
    public class ColorSettings : ScriptableObject
    {
        public Material planetMaterial;

        [Header("Ocean")]
        public Color oceanColor = Color.blue;
        public float oceanThreshold = 0.3f;

        [Header("Land Gradient")]
        public Gradient gradient;

        [Header("Tint")]
        public Color tintColor = Color.white;
        [Range(0, 1)] public float tintPercent = 0;
    }
}
