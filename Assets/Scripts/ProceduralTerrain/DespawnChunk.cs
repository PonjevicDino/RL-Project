using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DespawnChunk : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D collider)
    {
        ReorderSpline();
        Destroy(transform.parent.gameObject);
    }

    private void ReorderSpline()
    {
        //transform.parent.parent.GetComponent<ChunkSpawner>().shape;
    }
}
