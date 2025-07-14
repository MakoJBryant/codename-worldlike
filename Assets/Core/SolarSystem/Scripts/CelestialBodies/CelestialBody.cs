using UnityEngine;

namespace MakoJBryant.SolarSystem
{
    // Ensures a Rigidbody component is present on the GameObject this script is attached to.
    [RequireComponent(typeof(Rigidbody))]
    public class CelestialBody : MonoBehaviour
    {
        [Tooltip("The name of this celestial body (e.g., 'Sun', 'Earth', 'Mars').")]
        public string bodyName = "New Celestial Body";

        [Tooltip("The mass of this celestial body. Higher mass means stronger gravitational pull and more inertia.")]
        public float mass = 1000f; // Default mass, adjust in Inspector

        [Tooltip("The initial velocity of this body in meters per second. Set this in the Inspector for orbits.")]
        public Vector3 initialVelocity; // Set this in the Inspector for orbits

        [SerializeField] private Rigidbody rb;

        public Rigidbody Rigidbody => rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogError($"CelestialBody: Rigidbody not found on {gameObject.name}. " +
                               "Please ensure a Rigidbody component is attached.", this);
            }

            // Disable Unity's default gravity, as we'll be handling gravity manually via SolarSystemManager.
            rb.useGravity = false;
            rb.isKinematic = false; // Ensure it's not kinematic so forces can move it.
            rb.mass = mass; // Set the Rigidbody's mass.
            rb.linearDamping = 0f; // No air resistance in space.
            rb.angularDamping = 0f; // No angular resistance.

            // Register this celestial body with the SolarSystemManager.
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

        void Start()
        {
            // Initialize the Rigidbody's linear velocity with the initial velocity.
            rb.linearVelocity = initialVelocity;
        }

        // The ApplyVerletIntegration method is no longer needed here because
        // SolarSystemManager now directly applies forces using Rigidbody.AddForce,
        // letting Unity's physics engine handle the integration.

        void OnDestroy()
        {
            // Unregister this celestial body from the SolarSystemManager.
            if (SolarSystemManager.Instance != null)
            {
                SolarSystemManager.Instance.UnregisterCelestialBody(this);
            }
        }

        void OnDrawGizmos()
        {
            if (Application.isPlaying && rb != null)
            {
                Gizmos.color = Color.cyan;
                // Draw a line representing the current linear velocity.
                Gizmos.DrawLine(transform.position, transform.position + rb.linearVelocity);
                Gizmos.DrawSphere(transform.position + rb.linearVelocity, 0.1f);
            }
        }
    }
}
