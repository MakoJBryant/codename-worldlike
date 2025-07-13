using UnityEngine;

public class AtmosphereController : MonoBehaviour
{
    // These are the correct and intended declarations:

    [Tooltip("The material used for rendering the atmosphere.")]
    public Material atmosphereMaterial; // Correct declaration

    [Tooltip("The main directional light in the scene, representing the Sun.")]
    public Light sunLight; // Correct declaration

    [Tooltip("The radius of the atmosphere mesh. This should be slightly larger than the planet's radius.")]
    public float atmosphereRadius;

    // New public properties to match shader graph inputs
    [Header("Atmosphere Visual Settings")]
    public Color atmosphereColor = new Color(0.2f, 0.4f, 1.0f, 1.0f); // Default light blue
    [Range(0.1f, 10.0f)]
    public float density = 1.0f; // Overall intensity/opacity
    [Range(1.0f, 50.0f)]
    public float power = 5.0f; // Sharpness of alpha falloff
    [Range(0.0f, 1.0f)]
    public float ambientLightInfluence = 0.1f; // Base ambient light
    [Range(1.0f, 20.0f)]
    public float rimPower = 3.0f; // Sharpness of rim glow

    void Start()
    {
        // Ensure the material is assigned, try to get it from MeshRenderer if not set.
        if (atmosphereMaterial == null)
        {
            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (mr != null)
            {
                atmosphereMaterial = mr.sharedMaterial;
            }
            else
            {
                Debug.LogWarning("AtmosphereController: No MeshRenderer found to get material from.");
            }
        }
        // Call UpdateShaderProperties once at start to ensure initial values are set
        UpdateShaderProperties();
    }

    void Update()
    {
        // Update shader properties every frame (e.g., sun direction changes)
        UpdateShaderProperties();
    }

    // New private method to encapsulate setting shader properties
    private void UpdateShaderProperties()
    {
        if (atmosphereMaterial == null)
        {
            Debug.LogWarning("AtmosphereController: Atmosphere Material is null. Cannot set shader properties.");
            return;
        }

        // Pass the sun's direction to the atmosphere shader.
        if (sunLight != null)
        {
            atmosphereMaterial.SetVector("_SunDirection", -sunLight.transform.forward);
        }
        else
        {
            // If sunLight is null, use a default direction or log a warning
            atmosphereMaterial.SetVector("_SunDirection", Vector3.forward); // Default to forward if no sun
            // Debug.LogWarning("AtmosphereController: Sun Light is not assigned. Using default sun direction.");
        }

        // Pass the atmosphere radius to the shader.
        atmosphereMaterial.SetFloat("_AtmosphereRadius", atmosphereRadius);

        // Pass the new visual properties to the shader
        atmosphereMaterial.SetColor("_AtmosphereColor", atmosphereColor);
        atmosphereMaterial.SetFloat("_Density", density);
        atmosphereMaterial.SetFloat("_Power", power);
        atmosphereMaterial.SetFloat("_AmbientLightInfluence", ambientLightInfluence);
        atmosphereMaterial.SetFloat("_RimPower", rimPower);
    }
}