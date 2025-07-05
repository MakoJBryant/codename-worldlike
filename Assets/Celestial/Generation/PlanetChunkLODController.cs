using UnityEngine;

public class PlanetChunkLODController : MonoBehaviour
{
    private PlanetChunk[] planetChunks;

    void Start()
    {
        // Use FindObjectsByType with No sorting for better performance
        planetChunks = GameObject.FindObjectsByType<PlanetChunk>(FindObjectsSortMode.None);

        if (planetChunks == null || planetChunks.Length == 0)
            Debug.LogWarning("No PlanetChunk components found in scene.");
    }

    void Update()
    {
        if (planetChunks == null || planetChunks.Length == 0) return;

        Vector3 viewerPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;

        foreach (var chunk in planetChunks)
        {
            if (chunk != null)
                chunk.UpdateLOD(viewerPos);
        }
    }
}
