using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class ProceduralTerrainGenerator : MonoBehaviour
{
    [SerializeField] private SpriteShapeController shape;
    [SerializeField] private int scale = 300;
    [SerializeField] private int hillHeight = 10;

    [SerializeField] private int numOfPoints = 100;
    [SerializeField] private int distanceBetweenPoints = 3;

    void Start()
    {
        shape = transform.GetChild(0).GetComponent<SpriteShapeController>();

        shape.spline.SetPosition(2, shape.spline.GetPosition(2) + Vector3.right * scale);
        shape.spline.SetPosition(3, shape.spline.GetPosition(3) + Vector3.right * scale);

        for (int i = 0; i < numOfPoints; i++)
        {
            float xPos = shape.spline.GetPosition(i + 1).x;
            shape.spline.InsertPointAt(i + 2, new Vector3(xPos + distanceBetweenPoints,
                                                          hillHeight * Mathf.PerlinNoise(i * Random.Range(5.0f, 15.0f), 0.0f)));
        }

        for (int i = 2; i < numOfPoints + 2; i++)
        {
            shape.spline.SetTangentMode(i, ShapeTangentMode.Continuous);
            shape.spline.SetLeftTangent(i, new Vector3(-1, 0,0));
            shape.spline.SetRightTangent(i, new Vector3(1, 0, 0));
        }

        transform.GetChild(0).gameObject.AddComponent<BoxCollider2D>();
    }
}
