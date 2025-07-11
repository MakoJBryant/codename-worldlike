using UnityEngine;

namespace MakoJBryant.SolarSystem // Use the same namespace as SolarSystemManager
{
    // Ensures a Rigidbody component is present on the GameObject this script is attached to.
    // Rigidbody is essential for physics simulation (mass, velocity, forces).
    [RequireComponent(typeof(Rigidbody))]
    public class CelestialBody : MonoBehaviour
    {
        [Tooltip("The name of this celestial body (e.g., 'Sun', 'Earth', 'Mars').")]
        public string bodyName = "New Celestial Body";

        [Tooltip("The mass of this celestial body. Higher mass means stronger gravitational pull and more inertia.")]
        public float mass = 1.0f; // Default mass

        // Private reference to the Rigidbody component.
        // [SerializeField] allows it to be visible in the Inspector for debugging, but not directly assigned.
        [SerializeField] private Rigidbody rb;

        // Public property to easily access the Rigidbody component from other scripts.
        public Rigidbody Rigidbody => rb;

        // Awake is called when the script instance is being loaded.
        // This is where we get component references and perform initial setup.
        void Awake()
        {
            // Get the Rigidbody component. If it's not found (which shouldn't happen due to RequireComponent),
            // log an error.
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogError($"CelestialBody: Rigidbody not found on {gameObject.name}. " +
                               "Please ensure a Rigidbody component is attached.", this);
            }

            // For orbital mechanics, we will manually apply gravitational forces in FixedUpdate
            // via the SolarSystemManager. Therefore, Unity's default physics gravity should be disabled.
            rb.useGravity = false;

            // Add this celestial body to the SolarSystemManager's list of bodies.
            // This allows the manager to track and apply forces to all bodies in the simulation.
            // We check for null in case SolarSystemManager hasn't been initialized yet (though Awake order helps).
            if (SolarSystemManager.Instance != null)
            {
                SolarSystemManager.Instance.RegisterCelestialBody(this);
            }
            else
            {
                Debug.LogWarning($"CelestialBody: SolarSystemManager.Instance is null when trying to register {bodyName}. " +
                                 "Ensure SolarSystemManager initializes before CelestialBody components.", this);
            }
        }

        // OnDestroy is called when the GameObject is destroyed or the scene is unloaded.
        // It's important to unregister the body to prevent null reference errors
        // in the SolarSystemManager's list if this body is removed during runtime.
        void OnDestroy()
        {
            if (SolarSystemManager.Instance != null)
            {
                SolarSystemManager.Instance.UnregisterCelestialBody(this);
            }
        }

        // You can add other methods here specific to a celestial body,
        // such as methods for applying initial velocity, handling collisions,
        // or updating visual properties based on simulation state.
    }
}
