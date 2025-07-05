using UnityEngine;

[CreateAssetMenu(fileName = "PlanetSettings", menuName = "Planet/PlanetSettings")]
public class PlanetSettings : ScriptableObject
{
    public ShapeSettings shape;
    public ShadingSettings shading;
}
