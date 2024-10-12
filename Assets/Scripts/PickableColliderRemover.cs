using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickableColliderRemover : MonoBehaviour
{
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (transform.gameObject.activeSelf)
        {
            StartCoroutine(SetTriggerAfterDelay(1.0f));
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (transform.gameObject.activeSelf)
        {
            StartCoroutine(SetTriggerAfterDelay(1.0f));
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
