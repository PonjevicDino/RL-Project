using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.U2D;

public class ChunkSpawner : MonoBehaviour
{
    [SerializeField] private Vector3 spawnPosition = new Vector3(-9.0f, 0.0f, 0.0f);
    [SerializeField] private float fourierThreshold = 0.5f;
    [SerializeField] private bool debug = false;
    [SerializeField] private Transform totalChunks;

    private ContinuousFourierTransform terrain;
    private Vector3[] points;

    private GameObject chunk;
    private GameObject chunkPrefab;
    private int chunkNumber = 0;
    private int chunksToSpawn = 0;

    public Vector3 InitializeChunkSpawning(GameObject chunkPrefab)
    {
        this.chunkPrefab = chunkPrefab;
        SpriteTerrainGenerator.FourierTransformation fourierTransformation = chunkPrefab.GetComponent<SpriteTerrainGenerator>().fourierTransformation;
        terrain = transform.GetComponent<ContinuousFourierTransform>();

        points = terrain.ComputeFourierTransform(fourierTransformation, spawnPosition, fourierThreshold);
        chunksToSpawn = points.Length / chunkPrefab.GetComponent<SpriteTerrainGenerator>().numOfPoints;

        if (debug)
        {
            Debug.Log("MaxSpacing: " + (points[chunksToSpawn * chunkPrefab.GetComponent<SpriteTerrainGenerator>().numOfPoints] - points[chunksToSpawn * chunkPrefab.GetComponent<SpriteTerrainGenerator>().numOfPoints-1]));
            chunkPrefab.transform.Find("DeleteCollider").gameObject.SetActive(false);
            chunkPrefab.transform.Find("SpawnCollider").gameObject.SetActive(false);
            for (int i = 0; i < chunksToSpawn - 1; i++)
            {
                SpawnNewChunkBasedOnFourier();
            }
        }
        else
        {
            chunkPrefab.transform.Find("DeleteCollider").gameObject.SetActive(true);
            chunkPrefab.transform.Find("SpawnCollider").gameObject.SetActive(true);
            SpawnNewChunkBasedOnFourier();
        }

        return points[0] + (Vector3.up * 2);
    }

    public void SpawnNewChunkBasedOnFourier()
    {
        //If there is a previous chunk, collect it´s endpoint.y as the new startHeight otherwise use 0
        Vector3 previousEndPoint = points[0] + spawnPosition;

        if (chunk)
        {
            previousEndPoint = chunk.GetComponent<SpriteTerrainGenerator>().endPoint;
        }
               
        if (chunkNumber <= chunksToSpawn)
        {
            //Use the spawnOffset of the prefab to determine where to spawn the next chunk and position it based on its chunkNumber
            Vector3 spawnOffset = previousEndPoint;
            chunk = Instantiate(chunkPrefab, new Vector3(spawnOffset.x, spawnPosition.y), Quaternion.identity, this.transform);
            chunk.GetComponent<SpriteTerrainGenerator>().SpawnChunkBasedOnFourier(points, chunkNumber, previousEndPoint);
            GameManager.Instance.mapEndPoint = chunk.GetComponent<SpriteTerrainGenerator>().endPoint;
            chunkNumber++;
        }

        if (chunkNumber == chunksToSpawn - 1)
        {
            GameManager.Instance.mapEndPoint = chunk.GetComponent<SpriteTerrainGenerator>().endPoint;
        }
    }

    public Vector3 ReloadAllChunks()
    {
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
        chunkNumber = 0;

        // TODO: Enable for procedural generation
        return InitializeChunkSpawning(chunkPrefab);

        // TODO: Enable for fixed generation 
        /*
        for (int childChunk = 0; childChunk < totalChunks.childCount; childChunk++)
        {
            GameObject copiedChunk = Instantiate(totalChunks.GetChild(childChunk).gameObject, this.transform);
            copiedChunk.SetActive(true);
        } 
        */
    }
}
