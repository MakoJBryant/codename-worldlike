using UnityEngine;

public enum NoiseType
{
    FBM,
    Ridged,
    Billow,
    Flat
}

[System.Serializable]
public class NoiseSettings
{
    public bool enabled = true;
    public NoiseType type = NoiseType.FBM;

    public float frequency = 1f;
    public float amplitude = 1f;
    public float persistence = 0.5f;
    public int octaves = 1;

    public Vector3 offset = Vector3.zero;
    public bool useFirstLayerAsMask = false;
    public bool invert = false;
    public float power = 1f;
}
