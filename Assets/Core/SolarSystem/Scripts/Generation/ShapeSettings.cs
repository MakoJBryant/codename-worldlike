using UnityEngine; // Required for ScriptableObject and Header

namespace MakoJBryant.SolarSystem.Generation
{
    // This attribute allows you to create instances of this ScriptableObject
    // directly from the Unity Editor's Create menu (Assets > Create > Solar System > Shape Settings).
    [CreateAssetMenu(fileName = "New Shape Settings", menuName = "Solar System/Shape Settings")]
    public class ShapeSettings : ScriptableObject
    {
        // Global offset for terrain height
        [Tooltip("Adjusts the overall height of the terrain relative to the base radius. Negative values will create oceans.")]
        public float globalHeightOffset = 0f;

        // This is the array that will hold all your different noise configurations
        public NoiseLayer[] noiseLayers;
    }
}