using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkSpawner : MonoBehaviour
{
    private GameObject chunkPrefab;
    [SerializeField] private Vector3 spawnPosition = new Vector3(0.0f, 0.0f, 0.0f);

    private GameObject chunk;
    private int chunkNumber = 0;

    // Start is called before the first frame update
    private void Start()
    {
        //SpawnNewChunk();
    }

    public void InitializeChunkSpawning(GameObject chunkPrefab)
    {
        this.chunkPrefab = chunkPrefab;
        SpawnNewChunk();
    }

    public void SpawnNewChunk()
    {
        //If there is a previous chunk, collect it´s endpoint.y as the new startHeight otherwise use 0
        float startHeight = 0;
        if (chunk)
        {
            startHeight = chunk.GetComponent<SpriteTerrainGenerator>().endPoint.y;
        }
        
        //Use the spawnOffset of the prefab to determine where to spawn the next chunk and position it based on its chunkNumber
        Vector3 spawnOffset = new Vector3(chunkPrefab.GetComponent<SpriteTerrainGenerator>().scale, 0f, 0f);
        chunk = Instantiate(chunkPrefab, spawnPosition + (spawnOffset * chunkNumber), Quaternion.identity, this.transform);
        chunk.GetComponent<SpriteTerrainGenerator>().SpawnChunk(chunkNumber, startHeight);
        chunkNumber++;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
