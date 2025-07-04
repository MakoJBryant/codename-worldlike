namespace SolarSystem.Planets.Noise
{
    public interface INoiseFilter
    {
        float Evaluate(UnityEngine.Vector3 point);
    }
}
