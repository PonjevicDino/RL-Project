using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class ChunkSpawner : MonoBehaviour
{
    private GameObject chunkPrefab;
    [SerializeField] private Vector3 spawnPosition = new Vector3(0.0f, 0.0f, 0.0f);
    [SerializeField] private GameObject terrainPrefab;

    private SpriteShapeController shape;
    private ContinuousFourierTransform terrain;

    public GameObject chunk;
    private int chunkNumber = 0;

    private GameObject lastChunk;

    private Vector3[] points;
    private Vector3 previousEndPoint;

    private void Start()
    {
        //SpawnTerrain();
    }

    public void InitializeChunkSpawning(GameObject chunkPrefab)
    {
        this.chunkPrefab = chunkPrefab;
        this.shape = chunkPrefab.gameObject.GetComponent<SpriteShapeController>();
        terrain = transform.GetComponent<ContinuousFourierTransform>();

        points = terrain.ComputeFourierTransform();
        //SpawnNewDoubleChunk();
        SpawnNewChunkBasedOnFourier();
    }

    public void SpawnNewChunk()
    {
        //If there is a previous chunk, collect it´s endpoint.y as the new startHeight otherwise use 0
        Vector3 previousEndPoint = new Vector3(0.0f, 0.0f);
        if (chunk)
        {
            previousEndPoint = chunk.GetComponent<SpriteTerrainGenerator>().endPoint;
        }
        
        //Use the spawnOffset of the prefab to determine where to spawn the next chunk and position it based on its chunkNumber
        Vector3 spawnOffset = new Vector3(chunkPrefab.GetComponent<SpriteTerrainGenerator>().scale, 0f, 0f);
        chunk = Instantiate(chunkPrefab, spawnPosition + (spawnOffset * chunkNumber), Quaternion.identity, this.transform);
        chunk.GetComponent<SpriteTerrainGenerator>().SpawnChunk(chunkNumber, previousEndPoint);
        chunkNumber++;
    }

    public void SpawnNewDoubleChunk()
    {
        SpawnNewFourierChunk(false);
        lastChunk = chunk;
        SpawnNewFourierChunk(true);
        chunkNumber++;
    }

    public void SpawnNewFourierChunk(bool dummy)
    {
        //If there is a previous chunk, collect it´s endpoint.y as the new startHeight otherwise use 0
        Vector3 previousEndPoint = new Vector3(0.0f, 0.0f);

        if (dummy)
        {
            previousEndPoint = chunk.GetComponent<SpriteTerrainGenerator>().endPoint;
        }
        else
        {
            if (lastChunk)
            {
                previousEndPoint = lastChunk.GetComponent<SpriteTerrainGenerator>().endPoint;
            }
        }

        //Use the spawnOffset of the prefab to determine where to spawn the next chunk and position it based on its chunkNumber
        Vector3 spawnOffset = previousEndPoint + spawnPosition + (new Vector3(1.0f, 0.0f) * chunkNumber);
        chunk = Instantiate(chunkPrefab, new Vector3(spawnOffset.x, spawnPosition.y), Quaternion.identity, this.transform);
        chunk.GetComponent<SpriteTerrainGenerator>().SpawnNewFourierChunk(points, chunkNumber, previousEndPoint, dummy ? null : chunk);
    }

    public void SpawnNewChunkBasedOnFourier()
    {
        //If there is a previous chunk, collect it´s endpoint.y as the new startHeight otherwise use 0
        Vector3 previousEndPoint = new Vector3(0.0f, 0.0f);

        if (chunk)
        {
            //previousEndPoint = chunk.GetComponent<SpriteTerrainGenerator>().endPoint;
            previousEndPoint = chunk.GetComponent<SpriteTerrainGenerator>().dummySpawnPoint; //For overlapping spawn
        }

        //Use the spawnOffset of the prefab to determine where to spawn the next chunk and position it based on its chunkNumber
        Vector3 spawnOffset = previousEndPoint + spawnPosition; //(new Vector3(1.0f, 0.0f) * chunkNumber);
        chunk = Instantiate(chunkPrefab, new Vector3(spawnOffset.x, spawnPosition.y), Quaternion.identity, this.transform);
        chunk.GetComponent<SpriteTerrainGenerator>().SpawnChunkBasedOnFourier(points, chunkNumber, previousEndPoint); 
        chunkNumber++;
    }

    public void EnableNextChunk()
    {
        SpriteTerrainGenerator currentChunk = chunk.GetComponent<SpriteTerrainGenerator>();
        currentChunk.GetComponent<SpriteShapeRenderer>().enabled = true;
        currentChunk.collider.isTrigger = false;
        currentChunk.GetComponent<SpriteShapeController>().BakeCollider();
        
    }

    private void SpawnTerrain()
    {
        chunk = Instantiate(terrainPrefab, spawnPosition, Quaternion.identity, this.transform);
        chunk.GetComponent<SpriteTerrainGenerator>().SpawnTerrain(terrain);
    }
}
