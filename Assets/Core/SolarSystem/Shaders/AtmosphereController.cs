using UnityEngine;

public class AtmosphereController : MonoBehaviour
{
    [Tooltip("Assign your Atmosphere Material (M_Atmosphere) here.")]
    public Material atmosphereMaterial; // Reference to the material using the S_Atmosphere shader

    [Tooltip("Assign your Directional Light (Sun) GameObject here.")]
    public Light sunLight; // Reference to your scene's main directional light

    private static readonly int SunDirectionID = Shader.PropertyToID("_SunDirection");
    // You might also want to add a property for the atmosphere radius if your shader graph uses it directly.
    // private static readonly int AtmosphereRadiusID = Shader.PropertyToID("_AtmosphereRadius");


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
            // Try to find a directional light if not assigned
            if (GameObject.Find("Directional Light") != null) // Common default name
            {
                sunLight = GameObject.Find("Directional Light").GetComponent<Light>();
            }
            if (sunLight == null)
            {
                Debug.LogWarning("Sun Light not assigned to AtmosphereController on " + gameObject.name + " and could not be found automatically.");
                return;
            }
        }

        // Directional lights' forward vector (-transform.forward) points in the direction *of* the light.
        Vector3 lightDirection = -sunLight.transform.forward;

        // Pass the light direction to the shader.
        atmosphereMaterial.SetVector(SunDirectionID, lightDirection);

        // If your S_Atmosphere shader graph has an "_AtmosphereRadius" property, you would set it here.
        // The PlanetGenerator sets the scale of the GameObject, but the shader might need the value too.
        // atmosphereMaterial.SetFloat(AtmosphereRadiusID, transform.localScale.x * 0.5f); // Example, adjust as needed
    }
}