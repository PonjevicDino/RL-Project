using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickableColliderRemover : MonoBehaviour
{
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (transform.gameObject.activeSelf)
        {
            StartCoroutine(SetTriggerAfterDelay(0.5f));
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (transform.gameObject.activeSelf && !collision.gameObject.name.Contains("Collider"))
        {
            StartCoroutine(SetTriggerAfterDelay(0.5f));
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

    void FixedUpdate()
    {
        if (transform.localPosition.y < -50.0f)
        {
            transform.SetLocalPositionAndRotation(transform.localPosition, Quaternion.identity);
        }
    }
}
