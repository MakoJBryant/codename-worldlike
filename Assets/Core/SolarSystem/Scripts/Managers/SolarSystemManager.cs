using System.Collections.Generic; // Required for List<T>
using UnityEngine; // Required for MonoBehaviour, Rigidbody, Debug, etc.

// Define a namespace for your solar system project.
// This helps prevent naming conflicts with other assets, especially important for the Asset Store.
namespace MakoJBryant.SolarSystem
{
    // The SolarSystemManager class orchestrates the entire solar system simulation.
    // It's a singleton to ensure there's only one instance managing the simulation.
    public class SolarSystemManager : MonoBehaviour
    {
        // Singleton instance pattern: Allows easy access to the manager from other scripts.
        public static SolarSystemManager Instance { get; private set; }

        [Header("Simulation Parameters")]
        [Tooltip("The gravitational constant (G) used in the simulation. Adjust to control gravitational strength.")]
        [SerializeField] private float gravitationalConstant = 0.001f; // A small value for game scale

        [Tooltip("The time scale of the simulation. 1.0 is real-time, 2.0 is twice as fast, etc.")]
        [SerializeField] private float timeScale = 1.0f; // Default real-time

        [Header("Celestial Bodies")]
        [Tooltip("List of all celestial bodies currently in the simulation.")]
        private List<CelestialBody> celestialBodies = new List<CelestialBody>();

        // Public property to access the list of celestial bodies (read-only from outside).
        public IReadOnlyList<CelestialBody> CelestialBodies => celestialBodies;

        // Awake is called when the script instance is being loaded.
        // Used for initial setup and singleton pattern enforcement.
        void Awake()
        {
            // Enforce singleton pattern:
            // If an instance already exists and it's not this one, destroy this new instance.
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("SolarSystemManager: Another instance of SolarSystemManager detected. Destroying this one.", this);
                Destroy(gameObject);
                return;
            }

            // Otherwise, set this instance as the singleton.
            Instance = this;
            Debug.Log("Solar System Manager Initialized!");
        }

        // FixedUpdate is called at a fixed framerate, independent of frame rate.
        // This is ideal for physics calculations to ensure consistency.
        void FixedUpdate()
        {
            // Iterate through each celestial body and calculate gravitational forces.
            for (int i = 0; i < celestialBodies.Count; i++)
            {
                CelestialBody bodyA = celestialBodies[i];

                // Ensure bodyA and its Rigidbody are valid before proceeding.
                if (bodyA == null || bodyA.Rigidbody == null) continue;

                for (int j = 0; j < celestialBodies.Count; j++)
                {
                    // Don't calculate gravity of a body on itself.
                    if (i == j) continue;

                    CelestialBody bodyB = celestialBodies[j];

                    // Ensure bodyB and its Rigidbody are valid.
                    if (bodyB == null || bodyB.Rigidbody == null) continue;

                    // Calculate the gravitational force between bodyA and bodyB.
                    Vector3 force = CalculateGravitationalForce(bodyA, bodyB);

                    // Apply the force to bodyA's Rigidbody.
                    // We multiply by Time.fixedDeltaTime to make the force application frame-rate independent.
                    // We also multiply by timeScale to control the simulation speed.
                    bodyA.Rigidbody.AddForce(force * timeScale, ForceMode.Force);
                }
            }
        }

        /// <summary>
        /// Calculates the gravitational force between two celestial bodies.
        /// Formula: F = G * (m1 * m2) / r^2 * direction
        /// </summary>
        /// <param name="bodyA">The first celestial body.</param>
        /// <param name="bodyB">The second celestial body.</param>
        /// <returns>The gravitational force vector.</returns>
        private Vector3 CalculateGravitationalForce(CelestialBody bodyA, CelestialBody bodyB)
        {
            // Get the direction vector from bodyA to bodyB.
            Vector3 direction = bodyB.Rigidbody.position - bodyA.Rigidbody.position;

            // Calculate the squared distance between the bodies.
            // Using sqrMagnitude avoids a costly square root operation if only magnitude is needed.
            float distanceSquared = direction.sqrMagnitude;

            // Avoid division by zero if bodies are at the exact same position or too close.
            // A small epsilon can prevent extreme forces when objects are very close.
            if (distanceSquared < 0.0001f) // Use a small threshold instead of 0f
            {
                return Vector3.zero;
            }

            // Calculate the magnitude of the force using Newton's Law of Universal Gravitation.
            float forceMagnitude = gravitationalConstant * (bodyA.mass * bodyB.mass) / distanceSquared;

            // Return the force vector (magnitude multiplied by normalized direction).
            return direction.normalized * forceMagnitude;
        }

        /// <summary>
        /// Registers a celestial body with the SolarSystemManager.
        /// This method is typically called by each CelestialBody's Awake method.
        /// </summary>
        /// <param name="body">The CelestialBody to register.</param>
        public void RegisterCelestialBody(CelestialBody body)
        {
            if (!celestialBodies.Contains(body))
            {
                celestialBodies.Add(body);
                Debug.Log($"SolarSystemManager: Registered celestial body: {body.bodyName}");
            }
        }

        /// <summary>
        /// Unregisters a celestial body from the SolarSystemManager.
        /// Call this if a body is destroyed or removed from the simulation.
        /// </summary>
        /// <param name="body">The CelestialBody to unregister.</param>
        public void UnregisterCelestialBody(CelestialBody body)
        {
            if (celestialBodies.Contains(body))
            {
                celestialBodies.Remove(body);
                Debug.Log($"SolarSystemManager: Unregistered celestial body: {body.bodyName}");
            }
        }

        // OnDestroy is called when the GameObject is destroyed.
        // Clean up the singleton instance to prevent issues if the scene reloads.
        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
