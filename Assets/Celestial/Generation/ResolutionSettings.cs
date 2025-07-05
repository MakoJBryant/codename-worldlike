[System.Serializable]
public class ResolutionSettings
{
    public int lod0 = 300;
    public int lod1 = 100;
    public int lod2 = 50;
    public int collider = 100;

    public int GetLODResolution(int lod)
    {
        return lod switch
        {
            0 => lod0,
            1 => lod1,
            2 => lod2,
            _ => collider,
        };
    }
}
