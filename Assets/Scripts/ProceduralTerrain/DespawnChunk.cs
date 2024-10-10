using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DespawnChunk : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D collider)
    {
        Destroy(transform.parent.gameObject);
    }
}
