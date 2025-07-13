using UnityEngine;

namespace MakoJBryant.SolarSystem
{
    public class OrbitCamera : MonoBehaviour
    {
        [Tooltip("The GameObject the camera will orbit around (e.g., your Planet or Sun).")]
        public Transform target; // The object to orbit around (e.g., the Planet)

        [Header("Orbit Settings")]
        [Tooltip("How fast the camera orbits horizontally.")]
        public float orbitSpeedX = 100f;
        [Tooltip("How fast the camera orbits vertically.")]
        public float orbitSpeedY = 100f;
        [Tooltip("The minimum vertical angle (degrees) the camera can reach.")]
        [Range(-90, 90)]
        public float minY = -80f; // Limit vertical orbit to prevent flipping
        [Tooltip("The maximum vertical angle (degrees) the camera can reach.")]
        [Range(-90, 90)]
        public float maxY = 80f;

        [Header("Zoom Settings")]
        [Tooltip("How fast the camera zooms in/out.")]
        public float zoomSpeed = 500f;
        [Tooltip("The minimum distance the camera can be from the target.")]
        public float minZoom = 2f;
        [Tooltip("The maximum distance the camera can be from the target.")]
        public float maxZoom = 50f;

        [Header("Initial Position")]
        [Tooltip("The initial distance from the target.")]
        public float initialDistance = 15f;

        private float currentX = 0f; // Current horizontal rotation angle
        private float currentY = 0f; // Current vertical rotation angle
        private float currentDistance; // Current distance from the target

        // Awake is called when the script instance is being loaded
        void Awake()
        {
            // Set initial distance
            currentDistance = initialDistance;

            // If no target is assigned, try to find the "Sun" GameObject first, then "Planet"
            if (target == null)
            {
                GameObject sunGO = GameObject.Find("Sun");
                if (sunGO != null)
                {
                    target = sunGO.transform;
                    Debug.Log("OrbitCamera: Automatically assigned Sun as target.");
                }
                else
                {
                    GameObject planetGO = GameObject.Find("Planet");
                    if (planetGO != null)
                    {
                        target = planetGO.transform;
                        Debug.Log("OrbitCamera: Automatically assigned Planet as target.");
                    }
                    else
                    {
                        Debug.LogWarning("OrbitCamera: No target assigned and neither 'Sun' nor 'Planet' GameObject found. Camera will not orbit.");
                        enabled = false; // Disable script if no target
                    }
                }
            }

            // Set initial camera position and rotation
            if (target != null)
            {
                // Ensure the camera starts at a reasonable position relative to the target
                transform.position = target.position + new Vector3(0, 0, -currentDistance);
                transform.LookAt(target.position);

                // Calculate initial angles from current camera position relative to target
                Vector3 localPos = target.InverseTransformPoint(transform.position);
                currentY = Mathf.Asin(localPos.y / currentDistance) * Mathf.Rad2Deg;
                currentX = Mathf.Atan2(localPos.x, localPos.z) * Mathf.Rad2Deg;

                // Clamp initial Y to limits
                currentY = Mathf.Clamp(currentY, minY, maxY);

                UpdateCameraPosition();
            }
        }

        // LateUpdate is called once per frame, after all Update functions have been called.
        // This is good for camera logic to ensure all object movements are complete.
        void LateUpdate()
        {
            if (target == null) return;

            // --- Orbiting (Mouse Input) ---
            if (Input.GetMouseButton(0)) // Left mouse button held down
            {
                currentX += Input.GetAxis("Mouse X") * orbitSpeedX * Time.deltaTime;
                currentY -= Input.GetAxis("Mouse Y") * orbitSpeedY * Time.deltaTime; // Invert Y for intuitive control

                // Clamp the vertical angle to prevent camera flipping
                currentY = Mathf.Clamp(currentY, minY, maxY);
            }

            // --- Zooming (Scroll Wheel) ---
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                currentDistance -= scroll * zoomSpeed * Time.deltaTime;
                currentDistance = Mathf.Clamp(currentDistance, minZoom, maxZoom); // Clamp distance
            }

            UpdateCameraPosition();
        }

        /// <summary>
        /// Updates the camera's position and rotation based on current angles and distance.
        /// </summary>
        void UpdateCameraPosition()
        {
            // Calculate rotation based on current angles
            Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);

            // Calculate target position based on rotation and distance
            // We start from target.position and move 'backwards' along the camera's forward vector
            // to place it at the correct distance.
            Vector3 negDistance = new Vector3(0.0f, 0.0f, -currentDistance);
            Vector3 position = rotation * negDistance + target.position;

            // Apply calculated position and rotation to the camera
            transform.rotation = rotation;
            transform.position = position;
        }
    }
}
