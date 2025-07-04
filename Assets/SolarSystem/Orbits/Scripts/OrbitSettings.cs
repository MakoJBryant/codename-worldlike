using UnityEngine;

namespace SolarSystem.Orbits.Settings
{
    [CreateAssetMenu(fileName = "NewOrbitSettings", menuName = "SolarSystem/Orbits/OrbitSettings")]
    public class OrbitSettings : ScriptableObject
    {
        public float semiMajorAxis = 10f;
        public float eccentricity = 0f;
        public float orbitalSpeed = 10f;
    }
}
