using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.U2D;

public class CustomSuspension : MonoBehaviour
{
    [SerializeField] private GameObject frontWheel;
    [SerializeField] private GameObject backWheel;
    [SerializeField] private float frontWheelDistance;
    [SerializeField] private float backWheelDistance;
    [SerializeField] private float wheelYOffset;

    private Vector3[] wheels = new Vector3[2];
    private Vector2[] forces = new Vector2[2];
    private float[] oldDist = new float[2];

    private float maxSuspensionLength = 0.33f;
    private float suspensionMultiplier = 50.0f;
    private float dampSensitivity = 100.0f;
    private float maxDamp = 40.0f;

    private Rigidbody2D carRigidbody;

    void Awake()
    {
        for (int wheel = 0; wheel < wheels.Length; wheel++)
        {
            oldDist[wheel] = maxSuspensionLength;
        }
    }

    void Start()
    {
        carRigidbody = this.GetComponent<Rigidbody2D>();
        frontWheel.transform.parent = transform.parent;
        backWheel.transform.parent = transform.parent;
    }

    void FixedUpdate()
    {
        wheels[0] = transform.position - transform.right * frontWheelDistance - transform.up * wheelYOffset;
        wheels[1] = transform.position + transform.right * backWheelDistance - transform.up * wheelYOffset;

        for (int wheel = 0; wheel < wheels.Length; wheel++)
        {
            bool floorHit = false;
            RaycastHit2D[] hits = Physics2D.RaycastAll(wheels[wheel] + transform.up, -transform.up, maxSuspensionLength + 1.0f);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.gameObject.transform.GetComponent<SpriteShapeRenderer>() != null)
                {
                    forces[wheel] = Mathf.Clamp(maxSuspensionLength - (hit.distance - 1.0f), 0.0f, maxSuspensionLength) * suspensionMultiplier * transform.up +
                                    Mathf.Clamp((oldDist[wheel] - (hit.distance - 1.0f)) * dampSensitivity, 0.0f, maxDamp) * transform.up
                                    * Time.deltaTime;
                    //Debug.Log("Force: " + forces[wheel]);
                    carRigidbody.AddForceAtPosition(Mathf.Clamp(maxSuspensionLength - (hit.distance - 1.0f), 0.0f, maxSuspensionLength) * suspensionMultiplier * transform.up +
                                                    Mathf.Clamp((oldDist[wheel] - (hit.distance - 1.0f)) * dampSensitivity, 0.0f, maxDamp) * transform.up
                                                    / Mathf.Abs(Physics2D.gravity.y)
                                                    * Time.deltaTime,
                                                    wheels[wheel]);
                    if (wheel == 0) frontWheel.transform.position = hit.point + new Vector2(0.0f, 1.0f) * 0.2667422f;
                    if (wheel == 1) backWheel.transform.position = hit.point + new Vector2(0.0f, 1.0f) * 0.2667422f;
                    floorHit = true;
                    oldDist[wheel] = hit.distance - 1.0f;
                    break;
                }
            }
            if (!floorHit)
            {
                if (wheel == 0) frontWheel.transform.position = wheels[wheel] - transform.up * (maxSuspensionLength - 0.2667422f);
                if (wheel == 1) backWheel.transform.position = wheels[wheel] - transform.up * (maxSuspensionLength - 0.2667422f);
                oldDist[wheel] = maxSuspensionLength;
            }
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            frontWheel.transform.Rotate(new Vector3(0.0f, 0.0f, -100000.0f) * Time.deltaTime, Space.Self);
            backWheel.transform.Rotate(new Vector3(0.0f, 0.0f, -100000.0f) * Time.deltaTime, Space.Self);
            ApplyVelocityToCar();
        }
    }

    private void ApplyVelocityToCar()
    {
        float angularVelocity = frontWheel.transform.eulerAngles.z * Mathf.Deg2Rad;
        Vector3 tangentialVelocity = angularVelocity * 0.2667422f * transform.right;

        float frictionCoefficient = transform.parent.Find("Terrain").GetChild(0).GetComponent<SpriteTerrainGenerator>().material.friction;
        float normalForce = (carRigidbody.mass + forces[0].y / Mathf.Abs(Physics2D.gravity.y)) * Physics2D.gravity.magnitude;
        Vector3 frictionForce = frictionCoefficient * normalForce * tangentialVelocity.normalized;

        Debug.Log("Friciton: " + frictionForce);
        carRigidbody.AddForce(new Vector2(frictionForce.x, 0.0f), ForceMode2D.Force);
    }
}
