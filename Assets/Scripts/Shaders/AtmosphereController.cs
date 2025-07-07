using UnityEngine;

public class AtmosphereController : MonoBehaviour
{
    [Tooltip("Assign your Atmosphere Material (M_Atmosphere) here.")]
    public Material atmosphereMaterial; // Reference to the material using the S_Atmosphere shader

    [Tooltip("Assign your Directional Light (Sun) GameObject here.")]
    public Light sunLight; // Reference to your scene's main directional light

    private static readonly int SunDirectionID = Shader.PropertyToID("_SunDirection");

    void Update()
    {
        // Ensure both the material and the sun light are assigned.
        if (atmosphereMaterial == null)
        {
            Debug.LogWarning("Atmosphere Material not assigned to AtmosphereController on " + gameObject.name);
            return;
        }
        if (sunLight == null)
        {
            Debug.LogWarning("Sun Light not assigned to AtmosphereController on " + gameObject.name);
            return;
        }

        // Directional lights' forward vector (-transform.forward) points in the direction *of* the light.
        // So, from the perspective of the lit object, the light source is coming from the negative forward direction.
        Vector3 lightDirection = -sunLight.transform.forward;

        // Pass the light direction to the shader.
        // Shader.PropertyToID is used for performance to convert string to ID once.
        atmosphereMaterial.SetVector(SunDirectionID, lightDirection);
    }
}