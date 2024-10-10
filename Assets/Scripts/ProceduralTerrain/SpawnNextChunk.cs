using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnNextChunk : MonoBehaviour
{
    [SerializeField] private GameObject chunkPrefab;

    private bool isNextChunkSpawned = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isNextChunkSpawned)
        {       
            Instantiate(chunkPrefab, transform.parent.gameObject.GetComponent<SpriteTerrainGenerator>().endPoint,
                        Quaternion.identity, 
                        transform.parent.parent.transform);

            isNextChunkSpawned = true;
        }
    }
}
