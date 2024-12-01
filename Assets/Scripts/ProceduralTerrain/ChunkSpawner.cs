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
    private Vector3 spawnPositionWithTerrainOffset;
    [SerializeField] public float fourierThreshold = 0.5f;
    [SerializeField] private bool debug = false;
    [SerializeField] private bool fixedTerrain = false;

    private ContinuousFourierTransform terrain;
    private Vector3[] points;

    private GameObject chunk;
    private GameObject chunkPrefab;
    private int chunkNumber = 0;
    private int chunksToSpawn = 0;

    private GameManager gameManager;

    void Start()
    {
        gameManager = this.transform.parent.Find("GameManager").GetComponent<GameManager>();
        spawnPositionWithTerrainOffset = new Vector3(spawnPosition.x, spawnPosition.y + transform.parent.position.y, spawnPosition.z);
    }

    public Vector3 InitializeChunkSpawning(GameObject chunkPrefab)
    {
        this.chunkPrefab = chunkPrefab;
        SpriteTerrainGenerator.FourierTransformation fourierTransformation = chunkPrefab.GetComponent<SpriteTerrainGenerator>().fourierTransformation;
        terrain = transform.GetComponent<ContinuousFourierTransform>();

        if (fixedTerrain)
        {
            Random.InitState(0);
        }
        points = terrain.ComputeFourierTransform(fourierTransformation, spawnPosition, fourierThreshold);
        chunksToSpawn = points.Length / chunkPrefab.GetComponent<SpriteTerrainGenerator>().numOfPoints;

        if (debug)
        {
            Debug.Log("MaxSpacing: " + (points[chunksToSpawn * chunkPrefab.GetComponent<SpriteTerrainGenerator>().numOfPoints - 1] - points[chunksToSpawn * chunkPrefab.GetComponent<SpriteTerrainGenerator>().numOfPoints - 2]));

            for (int i = 0; i < chunksToSpawn - 1; i++)
            {
                SpawnNewChunkBasedOnFourier();
                chunk.transform.Find("DeleteCollider").gameObject.SetActive(false);
                chunk.transform.Find("SpawnCollider").gameObject.SetActive(false);
            }
        }
        else
        {
            SpawnNewChunkBasedOnFourier();
        }

        return points[0] + (Vector3.up * (2 + spawnPositionWithTerrainOffset.y));
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
            chunk = Instantiate(chunkPrefab, new Vector3(spawnOffset.x, spawnOffset.y), Quaternion.identity, this.transform);
            chunk.transform.localPosition = new Vector3(spawnOffset.x, 0.0f);
            chunk.GetComponent<SpriteTerrainGenerator>().SpawnChunkBasedOnFourier(points, chunkNumber, previousEndPoint);
            if (!gameManager)
            {
                gameManager = this.transform.parent.Find("GameManager").GetComponent<GameManager>();
            }
            gameManager.mapEndPoint = chunk.GetComponent<SpriteTerrainGenerator>().endPoint;
            chunkNumber++;
        }

        if (chunkNumber == chunksToSpawn - 1)
        {
            gameManager.mapEndPoint = chunk.GetComponent<SpriteTerrainGenerator>().endPoint;
        }
    }

    public Vector3 ReloadAllChunks()
    {
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
        chunkNumber = 0;

        if (fixedTerrain)
        {
            chunk = null;
            SpawnNewChunkBasedOnFourier();
            return points[0] + (Vector3.up * (2 + spawnPositionWithTerrainOffset.y));
        }
        else
        {
            return InitializeChunkSpawning(chunkPrefab);
        }
    }
}
