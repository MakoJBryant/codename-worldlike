using UnityEngine;
using System.Collections.Generic; // Required for List<Rigidbody>

public class GravityAttractor : MonoBehaviour
{
    [SerializeField] private float gravityStrength = 9.81f; // How strong the gravity is. Default to Earth's gravity.

    // A list to hold all objects that this planet should attract.
    // We'll manually add them for now.
    public List<Rigidbody> attractedBodies = new List<Rigidbody>();

    // FixedUpdate is called at a fixed interval and is best for physics calculations.
    void FixedUpdate()
    {
        // Loop through every Rigidbody in our list of attracted bodies.
        foreach (Rigidbody rb in attractedBodies)
        {
            // Ensure the Rigidbody exists and is not null
            if (rb == null)
            {
                Debug.LogWarning("GravityAttractor: Null Rigidbody found in 'Attracted Bodies' list. Please remove it.", this);
                continue; // Skip this null entry
            }

            // --- Calculation 1: Direction from the object to the planet's center ---
            // 'transform.position' is the position of THIS Planet (the attractor).
            // 'rb.position' is the position of the Rigidbody being attracted.
            Vector3 directionToPlanet = (transform.position - rb.position).normalized; // .normalized makes it a unit vector (length 1)

            // --- Calculation 2: The actual gravitational force ---
            // Force = Direction * Strength * Mass (F=ma, where 'a' is gravityStrength)
            // We multiply by rb.mass so that objects with different masses fall with the same acceleration (F/m = a, so a is constant).
            Vector3 gravitationalForce = directionToPlanet * gravityStrength * rb.mass;

            // --- Apply the force to the Rigidbody ---
            rb.AddForce(gravitationalForce);
        }
    }

    // Optional: Draw a debug gizmo in the editor to visualize the attractor's radius
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f); // Draw a small sphere at the attractor's center
    }
}