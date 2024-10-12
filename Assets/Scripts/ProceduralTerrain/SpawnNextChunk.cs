using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnNextChunk : MonoBehaviour
{
    private bool isNextChunkSpawned = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        //If the next chunk wasn´t spawned yet, sapwn it using the ChunkSpawner
        if (!isNextChunkSpawned && !other.gameObject.name.Contains("Coin"))
        {
            Object.FindAnyObjectByType<ChunkSpawner>().SpawnNewChunkBasedOnFourier();
            isNextChunkSpawned = true;
        }
    }
}
