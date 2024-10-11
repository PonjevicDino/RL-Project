using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class SpriteTerrainGenerator : MonoBehaviour
{
    private SpriteShapeController shape;
    [SerializeField] public int scale = 30;
    [SerializeField] private int hillHeight = 10;

    [SerializeField] private int numOfPoints = 10;
    [SerializeField] private int distanceBetweenPoints = 3;

    public Vector3 endPoint;

    private int terrainYOffset = 5;

    public void SpawnChunk(int chunkNumber, float startHeight)
    {
        //Rename the gameObject for better debugging
        this.gameObject.name = "Chunk " + (chunkNumber + 1);
        shape = transform.GetComponent<SpriteShapeController>();

        //Pull the two points underneath the ground further down so the ground covers the whole lower screen
        shape.spline.SetPosition(0, shape.spline.GetPosition(0) + Vector3.down * 20);
        shape.spline.SetPosition(3, shape.spline.GetPosition(3) + Vector3.down * 20);

        //Set the start of the ground to the endpoint.y of the previous chunk to keep the correct height
        Vector3 start = shape.spline.GetPosition(1);
        shape.spline.SetPosition(1, new Vector3(start.x, startHeight, 0.0f));

        //Set the lower right point to the end of the chunk, so a flat plane comes out
        shape.spline.SetPosition(3, shape.spline.GetPosition(3) + Vector3.right * (scale - 2));

        //The new endpoint.y needs to be precalculated, since otherwise it would always lay on the same height as the start
        endPoint = new Vector3(shape.spline.GetPosition(3).x, hillHeight * Mathf.PerlinNoise((numOfPoints-1) * Random.Range(5.0f, 15.0f), 0.0f) - terrainYOffset, 0.0f);
        shape.spline.SetPosition(2, endPoint);

        for (int i = 0; i < (numOfPoints-1); i++)
        {
            //The xPos is set based on the current i and the yPos by the perlin noise
            float xPos = shape.spline.GetPosition(i + 1).x + distanceBetweenPoints;
            float yPos = hillHeight * Mathf.PerlinNoise(i * Random.Range(5.0f, 15.0f), 0.0f) - terrainYOffset;
               
            //Afterwards insert the point into the spline
            shape.spline.InsertPointAt(i + 2, new Vector3(xPos, yPos));
        }

        //The condition must be <(numOfPoints-1) otherwise the right side won´t be flatten!
        for (int i = 2; i < (numOfPoints-1) + 2; i++)
        {
            shape.spline.SetTangentMode(i, ShapeTangentMode.Continuous);
            shape.spline.SetLeftTangent(i, new Vector3(-1, 0,0));
            shape.spline.SetRightTangent(i, new Vector3(1, 0, 0));
        }

        //Lastly set the collision, so the vehicle can drive on the surface
        transform.gameObject.AddComponent<EdgeCollider2D>();
    }
}
