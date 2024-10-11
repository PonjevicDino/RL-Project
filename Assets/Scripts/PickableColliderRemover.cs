using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickableColliderRemover : MonoBehaviour
{
    void OnCollisionEnter2D(Collision2D collision)
    {
            Destroy(transform.gameObject.GetComponent<Rigidbody2D>());
            transform.gameObject.GetComponent<CircleCollider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        Destroy(transform.gameObject.GetComponent<Rigidbody2D>());
        transform.gameObject.GetComponent<CircleCollider2D>().isTrigger = true;
    }
}
