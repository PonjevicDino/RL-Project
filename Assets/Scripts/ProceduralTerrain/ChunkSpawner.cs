using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.U2D;

public class ChunkSpawner : MonoBehaviour
{
    [SerializeField] private Vector3 spawnPosition = new Vector3(0.0f, 0.0f, 0.0f);

    [SerializeField] private Transform totalChunks;

    private ContinuousFourierTransform terrain;
    private Vector3[] points;

    private GameObject chunk;
    private GameObject chunkPrefab;
    private int chunkNumber = 0;

    private Vector3 previousEndPoint;

    public void InitializeChunkSpawning(GameObject chunkPrefab)
    {
        this.chunkPrefab = chunkPrefab;
        SpriteTerrainGenerator.FourierTransformation fourierTransformation = chunkPrefab.GetComponent<SpriteTerrainGenerator>().fourierTransformation;
        terrain = transform.GetComponent<ContinuousFourierTransform>();

        points = terrain.ComputeFourierTransform(fourierTransformation);
        SpawnNewChunkBasedOnFourier();
    }

    public void SpawnNewChunkBasedOnFourier()
    {
        //If there is a previous chunk, collect it´s endpoint.y as the new startHeight otherwise use 0
        Vector3 previousEndPoint = new Vector3(0.0f, 0.0f);

        if (chunk)
        {
            previousEndPoint = chunk.GetComponent<SpriteTerrainGenerator>().endPoint;
        }

        //Use the spawnOffset of the prefab to determine where to spawn the next chunk and position it based on its chunkNumber
        Vector3 spawnOffset = previousEndPoint + spawnPosition + (new Vector3(1.0f, 0.0f) * chunkNumber);
        chunk = Instantiate(chunkPrefab, new Vector3(spawnOffset.x, spawnPosition.y), Quaternion.identity, this.transform);
        chunk.GetComponent<SpriteTerrainGenerator>().SpawnChunkBasedOnFourier(points, chunkNumber, previousEndPoint); 
        chunkNumber++;
    }

    public void ReloadAllChunks()
    {
        
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
        chunkNumber = 0;

        // TODO: Enable for procedural generation
        InitializeChunkSpawning(chunkPrefab);

        // TODO: ENable for fixed generation
        /*
        for (int childChunk = 0; childChunk < totalChunks.childCount; childChunk++)
        {
            GameObject copiedChunk = Instantiate(totalChunks.GetChild(childChunk).gameObject, this.transform);
            copiedChunk.SetActive(true);
        }
        */
    }
}
