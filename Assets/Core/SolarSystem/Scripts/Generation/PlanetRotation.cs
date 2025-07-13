using UnityEngine;

namespace MakoJBryant.SolarSystem
{
    public class PlanetRotation : MonoBehaviour
    {
        [Tooltip("Degrees per second for rotation around the Y-axis (up).")]
        public float rotationSpeed = 10f; // Default rotation speed in degrees per second

        // Update is called once per frame
        void Update()
        {
            // Rotate the GameObject around its local Y-axis (up direction)
            // Time.deltaTime ensures the rotation speed is frame-rate independent.
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
        }
    }
}