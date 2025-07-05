using UnityEngine;

/// <summary>
/// Holds resolution values for different LOD levels and collider mesh.
/// </summary>
[System.Serializable]
public class ResolutionSettings
{
    [Tooltip("Resolution for LOD 0 (highest detail).")]
    public int lod0 = 300;

    [Tooltip("Resolution for LOD 1.")]
    public int lod1 = 100;

    [Tooltip("Resolution for LOD 2.")]
    public int lod2 = 50;

    [Tooltip("Resolution for collider mesh.")]
    public int collider = 100;

    /// <summary>
    /// Gets the resolution value for the specified LOD level.
    /// Returns collider resolution if LOD is out of range.
    /// </summary>
    public int GetLODResolution(int lod)
    {
        return lod switch
        {
            0 => Mathf.Max(1, lod0),
            1 => Mathf.Max(1, lod1),
            2 => Mathf.Max(1, lod2),
            _ => Mathf.Max(1, collider),
        };
    }
}
