using UnityEngine;

[RequireComponent(typeof(Rigidbody))] // Ensures a Rigidbody is always present on this GameObject
public class GravityBody : MonoBehaviour
{
    private Rigidbody rb; // Reference to the Rigidbody component
    private GravityAttractor currentAttractor; // The planet currently attracting this body

    // --- Awake is called when the script instance is being loaded ---
    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Ensure Unity's default gravity is OFF, as we're implementing our own.
        // This is CRITICAL.
        rb.useGravity = false;

        // Find the first GravityAttractor in the scene.
        // For a single planet game, this is fine. For multiple planets,
        // you'd have more sophisticated logic to find the closest one.
        currentAttractor = FindFirstObjectByType<GravityAttractor>();

        if (currentAttractor == null)
        {
            Debug.LogError("GravityBody: No GravityAttractor found in the scene! This body won't be attracted.", this);
            enabled = false; // Disable the script if no attractor is found
            return;
        }

        // Add this Rigidbody to the attractor's list.
        // This makes the attractor "aware" of this body.
        // This could be made more robust (e.g., check if already in list).
        currentAttractor.attractedBodies.Add(rb);
    }

    // FixedUpdate is called at a fixed interval and is best for physics calculations.
    void FixedUpdate()
    {
        if (currentAttractor == null) return; // Don't do anything if no attractor

        // Calculate the 'up' direction relative to the planet's surface.
        // This is the normalized vector from the object's position to the planet's center.
        Vector3 gravityUp = (rb.position - currentAttractor.transform.position).normalized;

        // Calculate the object's current 'up' direction (local y-axis).
        Vector3 bodyUp = transform.up;

        // Rotate the object to align its 'up' with the planet's gravity 'up'.
        // Quaternion.FromToRotation calculates the rotation needed to go from 'fromDirection' to 'toDirection'.
        // Then, we apply that rotation to the current rotation.
        rb.rotation = Quaternion.FromToRotation(bodyUp, gravityUp) * rb.rotation;
    }

    // --- OnDestroy is called when the GameObject is destroyed ---
    void OnDestroy()
    {
        // Clean up: Remove this Rigidbody from the attractor's list when destroyed.
        if (currentAttractor != null && currentAttractor.attractedBodies.Contains(rb))
        {
            currentAttractor.attractedBodies.Remove(rb);
        }
    }
}