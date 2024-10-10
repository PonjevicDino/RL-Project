using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class SpriteTerrainGenerator : MonoBehaviour
{
    private SpriteShapeController shape;
    [SerializeField] private int scale = 30;
    [SerializeField] private int hillHeight = 10;

    [SerializeField] private int numOfPoints = 10;
    [SerializeField] private int distanceBetweenPoints = 3;

    public Vector3 endPoint;

    private int terrainYOffset = 5;

    void Awake()
    {
        shape = transform.GetComponent<SpriteShapeController>();

        shape.spriteShape = Instantiate(shape.spriteShape);

        shape.spline.SetPosition(0, shape.spline.GetPosition(0) + Vector3.down * 10);
        shape.spline.SetPosition(3, shape.spline.GetPosition(3) + Vector3.down * 10);

        shape.spline.SetPosition(2, shape.spline.GetPosition(2) + Vector3.right * scale);
        shape.spline.SetPosition(3, shape.spline.GetPosition(3) + Vector3.right * scale);

        for (int i = 0; i < numOfPoints; i++)
        {
            float xPos = shape.spline.GetPosition(i + 1).x;

            Vector3 pointPos = new Vector3(xPos + distanceBetweenPoints,
                                           hillHeight * Mathf.PerlinNoise(i * Random.Range(5.0f, 15.0f) - terrainYOffset,
                                           0.0f));

            shape.spline.InsertPointAt(i + 2, pointPos);
        
            if (i == numOfPoints - 1)
            {
                endPoint = pointPos;
            }
        }

        for (int i = 2; i < numOfPoints + 2; i++)
        {
            shape.spline.SetTangentMode(i, ShapeTangentMode.Continuous);
            shape.spline.SetLeftTangent(i, new Vector3(-1, 0,0));
            shape.spline.SetRightTangent(i, new Vector3(1, 0, 0));
        }

        transform.gameObject.AddComponent<EdgeCollider2D>();
    }

    public int GetScale() { return scale; }
}
