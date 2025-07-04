using UnityEngine;
using SolarSystem.Orbits.Settings;

namespace SolarSystem.Orbits
{
    public class OrbitSimulator : MonoBehaviour
    {
        public Transform orbitCenter;
        public OrbitSettings orbitSettings;

        private float orbitAngle = 0f;

        void Update()
        {
            if (orbitSettings == null || orbitCenter == null) return;

            orbitAngle += orbitSettings.orbitalSpeed * Time.deltaTime;
            float rad = orbitAngle * Mathf.Deg2Rad;

            float a = orbitSettings.semiMajorAxis;
            float e = Mathf.Clamp01(orbitSettings.eccentricity);
            float b = a * Mathf.Sqrt(1 - e * e);

            Vector3 offset = new Vector3(
                a * Mathf.Cos(rad),
                0f,
                b * Mathf.Sin(rad)
            );

            transform.position = orbitCenter.position + offset;
        }
    }
}
