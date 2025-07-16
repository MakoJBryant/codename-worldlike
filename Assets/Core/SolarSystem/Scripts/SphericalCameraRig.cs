using UnityEngine;

public class SphericalCameraRig : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform gravityCenter;
    public Transform pitchPivot; // holds Camera

    [Header("Camera Control")]
    public float yawSpeed = 240f;
    public float pitchSpeed = 180f;
    public float minPitch = -60f;
    public float maxPitch = 60f;
    public float mouseSensitivity = 1f;

    [Header("Smoothing")]
    public float upSmoothing = 10f;

    private float yaw;
    private float pitch = 20f;
    private Vector3 smoothedUp = Vector3.up;

    void LateUpdate()
    {
        if (!player || !gravityCenter || !pitchPivot) return;

        // Step 1: Determine up direction based on gravity
        Vector3 targetUp = (player.position - gravityCenter.position).normalized;
        smoothedUp = Vector3.Slerp(smoothedUp, targetUp, upSmoothing * Time.deltaTime);

        // Step 2: Update yaw and pitch input
        yaw += Input.GetAxis("Mouse X") * yawSpeed * mouseSensitivity * Time.deltaTime;
        pitch -= Input.GetAxis("Mouse Y") * pitchSpeed * mouseSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Step 3: Position camera rig at player position
        transform.position = player.position;

        // Step 4: Rebuild rotation manually
        Quaternion yawRotation = Quaternion.AngleAxis(yaw, smoothedUp);
        Vector3 right = yawRotation * Vector3.right;
        Vector3 forward = Vector3.Cross(smoothedUp, -right);
        Quaternion pitchRotation = Quaternion.AngleAxis(pitch, right);
        Vector3 finalForward = pitchRotation * forward;

        transform.rotation = Quaternion.LookRotation(finalForward, smoothedUp);
        pitchPivot.localRotation = Quaternion.identity;

        // Optional debug rays
        Debug.DrawRay(transform.position, smoothedUp * 2f, Color.green);
        Debug.DrawRay(transform.position, finalForward * 2f, Color.blue);
    }
}
