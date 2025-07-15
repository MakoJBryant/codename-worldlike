using UnityEngine;

[ExecuteAlways]
public class AtmosphereController : MonoBehaviour
{
    [Tooltip("The material used for rendering the atmosphere.")]
    public Material atmosphereMaterial;

    [Tooltip("The main directional light in the scene, representing the Sun.")]
    public Light sunLight;

    [Tooltip("The radius of the atmosphere mesh. This should be slightly larger than the planet's radius.")]
    public float atmosphereRadius;

    [Header("Atmosphere Visual Settings")]
    public Color atmosphereColor = new Color(0.2f, 0.4f, 1.0f, 1.0f); // Default light blue
    [Range(0.1f, 10.0f)] public float density = 1.0f; // Overall intensity/opacity
    [Range(1.0f, 50.0f)] public float power = 5.0f; // Sharpness of alpha falloff
    [Range(0.0f, 1.0f)] public float ambientLightInfluence = 0.1f; // Base ambient light
    [Range(1.0f, 20.0f)] public float rimPower = 3.0f; // Sharpness of rim glow

    private void Start()
    {
        if (atmosphereMaterial == null)
        {
            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (mr != null)
            {
                atmosphereMaterial = mr.sharedMaterial;
            }
            else
            {
                Debug.LogWarning("AtmosphereController: No MeshRenderer found to get material from. Ensure one is present.", this);
            }
        }

        UpdateShaderProperties();
    }

    private void Update()
    {
        UpdateShaderProperties();
    }

    private void UpdateShaderProperties()
    {
        if (atmosphereMaterial == null)
        {
            Debug.LogWarning("AtmosphereController: Atmosphere Material is null. Cannot set shader properties.", this);
            return;
        }

        // Send sun direction
        if (sunLight != null)
        {
            atmosphereMaterial.SetVector("_SunDirection", -sunLight.transform.forward);
        }
        else
        {
            atmosphereMaterial.SetVector("_SunDirection", Vector3.forward);
        }

        // 🌍 Send the planet's *world-space* center as _PlanetCenter
        Vector3 planetCenter = transform.parent != null ? transform.parent.position : transform.position;
        atmosphereMaterial.SetVector("_PlanetCenter", planetCenter);

        atmosphereMaterial.SetFloat("_AtmosphereRadius", atmosphereRadius);
        atmosphereMaterial.SetColor("_AtmosphereColor", atmosphereColor);
        atmosphereMaterial.SetFloat("_Density", density);
        atmosphereMaterial.SetFloat("_Power", power);
        atmosphereMaterial.SetFloat("_AmbientLightInfluence", ambientLightInfluence);
        atmosphereMaterial.SetFloat("_RimPower", rimPower);
    }
}
