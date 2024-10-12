using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickableColliderRemover : MonoBehaviour
{
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (transform.gameObject.activeSelf)
        {
            if (!collision.gameObject.name.Contains("Chunk") && !collision.gameObject.name.Contains("Vehicle") && !collision.gameObject.name.Contains("Bumper") && !collision.gameObject.name.Contains("Tire")) { 
                Debug.LogWarning(collision.gameObject.name); 
            }

            StartCoroutine(SetTriggerAfterDelay(2.0f));
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (transform.gameObject.activeSelf && !collision.gameObject.name.Contains("Collider"))
        {
            if (!collision.gameObject.name.Contains("Chunk") && !collision.gameObject.name.Contains("Vehicle") && !collision.gameObject.name.Contains("Bumper") && !collision.gameObject.name.Contains("Tire"))
            {
                Debug.LogWarning(collision.gameObject.name);
            }

            StartCoroutine(SetTriggerAfterDelay(2.0f));
        }
    }

    private IEnumerator SetTriggerAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (transform.gameObject.GetComponent<Rigidbody2D>() != null)
        {
            Destroy(transform.gameObject.GetComponent<Rigidbody2D>());
            transform.gameObject.GetComponent<CircleCollider2D>().isTrigger = true;
        }
    }
}
