using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class DespawnChunk : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D collider)
    {
        if (!collider.gameObject.name.Contains("Coin") && !collider.gameObject.name.Contains("Fuel"))
        {
            //transform.parent.parent.GetComponent<ChunkSpawner>().EnableNextChunk();
            Destroy(transform.parent.gameObject);
        }
    }
}
