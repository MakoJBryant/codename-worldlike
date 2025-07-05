using System.Collections.Generic;
using UnityEngine;

public class PlanetGenerator : MonoBehaviour
{
    public ShapeSettings shapeSettings;
    public Material baseMaterial;

    public int chunkCountPerAxis = 4;
    public float planetRadius = 10f;
    public int baseResolution = 64;
    public int maxLOD = 4;
    public float lodDistanceMultiplier = 2f;

    private List<PlanetChunk> chunks = new List<PlanetChunk>();

    void Start()
    {
        GenerateChunks();
    }

    void Update()
    {
        Vector3 viewerPos = Camera.main ? Camera.main.transform.position : Vector3.zero;
        foreach (var chunk in chunks)
        {
            chunk.UpdateLOD(viewerPos);
        }
    }

    void GenerateChunks()
    {
        // Clear old chunks
        foreach (var c in chunks)
        {
            if (Application.isPlaying)
                Destroy(c.gameObject);
            else
                DestroyImmediate(c.gameObject);
        }
        chunks.Clear();

        float spacing = (planetRadius * 2) / chunkCountPerAxis;
        Vector3 start = transform.position - Vector3.one * planetRadius;

        for (int x = 0; x < chunkCountPerAxis; x++)
        {
            for (int y = 0; y < chunkCountPerAxis; y++)
            {
                for (int z = 0; z < chunkCountPerAxis; z++)
                {
                    Vector3 chunkPos = start + new Vector3(x + 0.5f, y + 0.5f, z + 0.5f) * spacing;
                    GameObject chunkObj = new GameObject($"Chunk_{x}_{y}_{z}");
                    chunkObj.transform.parent = transform;
                    chunkObj.transform.position = chunkPos;
                    chunkObj.transform.localScale = Vector3.one * spacing;

                    PlanetChunk chunk = chunkObj.AddComponent<PlanetChunk>();
                    chunk.shapeSettings = shapeSettings;
                    chunk.baseMaterial = baseMaterial;
                    chunk.baseResolution = baseResolution;
                    chunk.maxLOD = maxLOD;
                    chunk.chunkSize = spacing;
                    chunk.lodDistanceMultiplier = lodDistanceMultiplier;

                    chunks.Add(chunk);
                }
            }
        }
    }
}
