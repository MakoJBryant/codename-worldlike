using System.Collections.Generic;
using UnityEngine;

public class PlanetLODController : MonoBehaviour
{
    public ShapeSettings shapeSettings;
    public Material lodMaterial; // Use consistent naming for the LOD fade material
    public int baseResolution = 64;
    public int maxLOD = 4;
    public float chunkSize = 10f;
    public float lodDistanceMultiplier = 2f;

    private List<PlanetChunk> chunks = new List<PlanetChunk>();

    void Start()
    {
        GenerateChunks();
    }

    void Update()
    {
        if (Camera.main == null) return;

        Vector3 viewerPos = Camera.main.transform.position;
        foreach (var chunk in chunks)
        {
            chunk.UpdateLOD(viewerPos);
        }
    }

    void GenerateChunks()
    {
        // Positions correspond to cube face centers at distance chunkSize from origin
        Vector3[] chunkPositions = new Vector3[]
        {
            Vector3.forward * chunkSize,
            Vector3.back * chunkSize,
            Vector3.left * chunkSize,
            Vector3.right * chunkSize,
            Vector3.up * chunkSize,
            Vector3.down * chunkSize,
        };

        foreach (var pos in chunkPositions)
        {
            GameObject chunkGO = new GameObject("PlanetChunk");
            chunkGO.transform.parent = transform;
            chunkGO.transform.localPosition = pos;
            chunkGO.transform.localScale = Vector3.one * chunkSize; // optional, depending on your chunk design

            PlanetChunk chunk = chunkGO.AddComponent<PlanetChunk>();

            chunk.Initialize(
                shapeSettings,
                lodMaterial,
                baseResolution,
                maxLOD,
                chunkSize,
                lodDistanceMultiplier
            );

            chunks.Add(chunk);
        }
    }
}
