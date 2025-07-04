using UnityEngine;

namespace SolarSystem.Core
{
    [ExecuteAlways]
    public class CelestialBody : MonoBehaviour
    {
        public float bodyRadius = 1f;

        private void OnValidate()
        {
            transform.localScale = Vector3.one * bodyRadius * 2f; // diameter
        }
    }
}
