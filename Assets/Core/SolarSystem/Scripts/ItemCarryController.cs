using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ItemCarryController : MonoBehaviour
{
    [Header("References")]
    public Transform carryPoint;
    public Camera playerCamera;
    public float pickupRange = 3f;
    public LayerMask pickupLayer;

    [Header("Physics")]
    public float dropPushForce = 2f;

    private Rigidbody carriedRb;
    private Transform carriedTransform;

    private Vector3 originalScale;

    void Start()
    {
        // Ensure pickupLayer includes Default layer so dropped objects can be picked up again
        int defaultLayer = LayerMask.NameToLayer("Default");
        if ((pickupLayer.value & (1 << defaultLayer)) == 0)
        {
            pickupLayer |= (1 << defaultLayer);
        }
    }

    void Update()
    {
        if (carriedTransform != null)
        {
            // Snap directly every frame to avoid lag/jitter
            carriedTransform.position = carryPoint.position;
            carriedTransform.rotation = carryPoint.rotation;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (carriedTransform == null)
                TryPickup();
            else
                Drop();
        }
    }

    void TryPickup()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, pickupLayer))
        {
            Rigidbody rb = hit.collider.attachedRigidbody;
            if (rb != null && carriedTransform == null)
            {
                carriedRb = rb;
                carriedTransform = rb.transform;

                // Store original scale before parenting
                originalScale = carriedTransform.localScale;

                // Set physics to kinematic and disable gravity while held
                carriedRb.isKinematic = true;
                carriedRb.useGravity = false;
                carriedRb.collisionDetectionMode = CollisionDetectionMode.Continuous;

                // Change layer to avoid player collisions and immediate re-pickup
                carriedTransform.gameObject.layer = LayerMask.NameToLayer("HeldObject");

                // Parent to carry point, keeping world position and rotation
                carriedTransform.SetParent(carryPoint, true);

                // Restore original scale (in case parenting affects scale)
                carriedTransform.localScale = originalScale;

                // Snap position and rotation immediately
                carriedTransform.position = carryPoint.position;
                carriedTransform.rotation = carryPoint.rotation;

                // Ensure collider is enabled
                Collider col = carriedTransform.GetComponent<Collider>();
                if (col != null)
                    col.enabled = true;
            }
        }
    }

    void Drop()
    {
        if (carriedTransform == null) return;

        // Unparent
        carriedTransform.SetParent(null);

        // Restore original scale
        carriedTransform.localScale = originalScale;

        // Restore physics
        carriedRb.isKinematic = false;
        carriedRb.useGravity = true;

        // Reset layer so it can be picked up again
        carriedTransform.gameObject.layer = LayerMask.NameToLayer("Default");

        // Ensure collider is enabled
        Collider col = carriedTransform.GetComponent<Collider>();
        if (col != null)
            col.enabled = true;

        // Stop all motion
        carriedRb.linearVelocity = Vector3.zero;
        carriedRb.angularVelocity = Vector3.zero;

        // Add a small downward nudge to help settle it
        carriedRb.AddForce(Vector3.down * 0.5f, ForceMode.VelocityChange);

        // Optional: Uncomment below to add a slight forward push if desired
        // carriedRb.AddForce(playerCamera.transform.forward * dropPushForce, ForceMode.VelocityChange);

        // Clear references
        carriedTransform = null;
        carriedRb = null;
    }
}
