using UnityEngine;

public class OrbitingBody : MonoBehaviour
{
    [Header("Orbit Settings")]
    public Transform orbitCenter;
    public float orbitRadius = 10f;
    public float orbitSpeed = 0f;          // Degrees per second for orbit around planet
    public Vector3 orbitAxis = Vector3.up;

    [Header("Rotation Settings")]
    public float selfRotationSpeed = 0f;   // Degrees per second for self-rotation (usually 0 here)

    private float angle;

    void Start()
    {
        if (!orbitCenter) return;
        Vector3 offset = transform.position - orbitCenter.position;
        angle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
    }

    void Update()
    {
        if (!orbitCenter) return;

        // Update orbit angle
        angle += orbitSpeed * Time.deltaTime;
        if (angle >= 360f) angle -= 360f;

        // Calculate new orbit position
        Quaternion rotation = Quaternion.AngleAxis(angle, orbitAxis.normalized);
        Vector3 offset = rotation * Vector3.right * orbitRadius;
        transform.position = orbitCenter.position + offset;

        // Always look at the planet center
        transform.LookAt(orbitCenter);

        // Optional: self-rotation for visual spin, usually 0 for sun/moon
        if (selfRotationSpeed != 0f)
            transform.Rotate(Vector3.up, selfRotationSpeed * Time.deltaTime, Space.Self);
    }
}
