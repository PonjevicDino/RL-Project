using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using static SpriteTerrainGenerator;

public class SpriteTerrainGenerator : MonoBehaviour
{
    [System.Serializable]
    public struct FourierTransformation
    {
        public float initialFrequency;     // Standard frequency
        public float initialAmplitude;     // Initial amplitude to start higher
        public float amplitudeIncrease;    // Increase in amplitude
        public float maxAmplitudePercentage;     // Maximum Percentage between height and width
        public int pointCount;             // Number of points to render
        public float initialPointSpacing;  // Initial spacing between points
        public float spacingIncreaseFactor; // Factor by which to increase spacing
        public float randomnessFactor;     // Initial randomness factor
    }

    [SerializeField] public int numOfPoints = 10;
    [SerializeField] private int distanceBetweenCoins = 4;
    [SerializeField] private int distanceBetweenFuelTanks = 7;
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private GameObject fuelTankPrefab;
    [SerializeField] public PhysicsMaterial2D material;
    [SerializeField] private Vector3 gravity = new Vector3(0.0f, -9.81f);
    [SerializeField] public FourierTransformation fourierTransformation;
    [SerializeField] private int pointsBetween = 1;

    public Vector3 endPoint;
    private SpriteShapeController shape;
    private float envOffsetY;



    void Start()
    {
        Physics2D.gravity = gravity;
        //Random.InitState(0);
    }

    private void UpdateEnvOffset()
    {
        envOffsetY = transform.parent.position.y;
    }

    public void SpawnChunkBasedOnFourier(Vector3[] points, int chunkNumber, Vector3 previousEndPoint)
    {
        UpdateEnvOffset();

        //Rename the gameObject for better debugging
        this.gameObject.name = "Chunk " + (chunkNumber + 1);
        shape = transform.GetComponent<SpriteShapeController>();

        int offset = chunkNumber * numOfPoints; 

        endPoint = points[offset + numOfPoints];

        //Pull the two points underneath the ground further down so the ground covers the whole lower screen
        shape.spline.SetPosition(0, shape.spline.GetPosition(0) + Vector3.down * 50.0f);
        shape.spline.SetPosition(3, shape.spline.GetPosition(3) + Vector3.down * 50.0f);

        //Set the start of the ground to the endpoint.y of the previous chunk to keep the correct height
        shape.spline.SetPosition(1, new Vector3(shape.spline.GetPosition(1).x, previousEndPoint.y));

        //Set the lower right point to the end of the chunk, so a flat plane comes out
        shape.spline.SetPosition(3, new Vector3(endPoint.x - points[offset].x, shape.spline.GetPosition(3).y));

        shape.spline.SetPosition(2, new Vector3(shape.spline.GetPosition(3).x, endPoint.y));

        for (int i = 0; i < numOfPoints; i++)
        {
            for (int j = 0; j < pointsBetween; j++)
            {
                float tempPoint = 1.0f / pointsBetween * j;
                float xPosFromOffset = points[offset + i].x - points[offset].x;
                float tempXPos = points[offset + i + 1].x - points[offset + i].x;

                float yPosFromOffset = points[offset + i].y;
                float tempYPos = (points[offset + i + 1].y - points[offset + i].y) * tempPoint;

                if (i + j != 0)
                {
                    shape.spline.InsertPointAt(i * pointsBetween + j + 1, new Vector3(xPosFromOffset + tempXPos * (1.0f + tempPoint) - tempXPos, yPosFromOffset + tempYPos));
                }
            }
        }

        float tangentOffset = offset * (fourierTransformation.spacingIncreaseFactor/5.0f);
        shape.spline.SetTangentMode(1, ShapeTangentMode.Broken);
        shape.spline.SetRightTangent(1, new Vector3((5.0f + tangentOffset) / pointsBetween, 0, 0));

        for (int i = 2; i <= numOfPoints * pointsBetween; i++)
        {
            float leftTangentY = 0.0f;
            float rightTangentY = 0.0f;

            if (pointsBetween != 1)
            {
                float lastPointDiff = offset + i + 1 < points.Length ? points[offset + i].y - points[offset + i - 1].y : 0.0f;
                float nextPointDiff = offset + i + 1 < points.Length ? points[offset + i].y - points[offset + i + 1].y : 0.0f;

                lastPointDiff = shape.spline.GetPosition(i - 1).y - shape.spline.GetPosition(i).y;
                nextPointDiff = shape.spline.GetPosition(i + 1).y - shape.spline.GetPosition(i).y;

                // .. / 4.0f for smoothing
                leftTangentY = lastPointDiff >= 0.0f ? Random.Range(0.0f, lastPointDiff / 2.0f) : Random.Range(lastPointDiff / 2.0f, 0.0f);
                rightTangentY = nextPointDiff >= 0.0f ? Random.Range(0.0f, nextPointDiff / 2.0f) : Random.Range(nextPointDiff / 2.0f, 0.0f);
            }

            shape.spline.SetTangentMode(i, ShapeTangentMode.Broken);
            shape.spline.SetLeftTangent(i, new Vector3((-5.0f - tangentOffset) / pointsBetween, leftTangentY / pointsBetween, 0));
            shape.spline.SetRightTangent(i, new Vector3((5.0f + tangentOffset) / pointsBetween, rightTangentY / pointsBetween, 0));
        }

        shape.spline.SetTangentMode(numOfPoints * pointsBetween + 1, ShapeTangentMode.Broken);
        shape.spline.SetLeftTangent(numOfPoints * pointsBetween + 1, new Vector3((-5.0f - tangentOffset) / pointsBetween, 0, 0));

        if (pointsBetween != 1)
        {
            shape.spline.SetPosition(0, shape.spline.GetPosition(0) + Vector3.right);
            shape.spline.SetPosition(1, shape.spline.GetPosition(1) + Vector3.right);
        }

        float spawnOffset = previousEndPoint.x;

        PlacePickables(spawnOffset, pointsBetween);
        PlaceCollider();

        EdgeCollider2D collider = transform.gameObject.AddComponent<EdgeCollider2D>();
        collider.sharedMaterial = material;
        shape.BakeMesh();
    }
    private void PlacePickables(float spawnOffset, int pointsBetween)
    {
        for (int splinePoint = 1; splinePoint < shape.spline.GetPointCount()-2; splinePoint++)
        {
            int spawnSplineOffset = splinePoint;
            if (splinePoint % (distanceBetweenCoins * pointsBetween) == 0)
            {
                spawnSplineOffset = PlaceCoins(spawnOffset, splinePoint);
            }
            if (splinePoint % (distanceBetweenFuelTanks * pointsBetween) == 0)
            {
                if (spawnSplineOffset == 0)
                {
                    spawnSplineOffset += 1;
                }

                PlaceFuelTanks(spawnOffset, spawnSplineOffset);
            }
        }
    }

    private int PlaceCoins(float spawnOffset, int splinePoint)
    {
        int spawnSplinePoint = splinePoint + Random.Range(-1, 1);
        Vector3 heightOffset = GetHighestPointBetweenSplinePoints(splinePoint, spawnSplinePoint) + Vector3.up * 5.0f;
        Vector3 spawnPosition = GetMeanBetweenSplinePoints(splinePoint, spawnSplinePoint);
        GameObject coinPrefabInstance = Instantiate(coinPrefab, new Vector3(spawnPosition.x, heightOffset.y + envOffsetY) 
            + Vector3.right * spawnOffset, Quaternion.identity, transform);
        coinPrefabInstance.GetComponent<CoinGenerator>().SpawnCoins(spawnPosition);
        return spawnSplinePoint;
    }

    private void PlaceFuelTanks(float spawnOffset, int splinePoint)
    {
        Vector3 fuelTankSpawnPoint = shape.spline.GetPosition(splinePoint);
        Vector3 heightOffset = GetHighestPointBetweenSplinePoints(splinePoint - 1, splinePoint + 1) + Vector3.up * 5.0f;
        Instantiate(fuelTankPrefab, new Vector3(fuelTankSpawnPoint.x, heightOffset.y + envOffsetY) 
            + Vector3.right * spawnOffset, Quaternion.identity, transform);
    }

    private void PlaceCollider()
    {
        float spawnDifference = (shape.spline.GetPosition(numOfPoints + 1).x - shape.spline.GetPosition(1).x) * (1.0f/3.0f);

        transform.Find("DeleteCollider").transform.SetPositionAndRotation(new Vector3(endPoint.x + spawnDifference, endPoint.y + envOffsetY, 0.0f), Quaternion.identity);
        transform.Find("SpawnCollider").transform.SetPositionAndRotation(new Vector3(endPoint.x - spawnDifference, endPoint.y + envOffsetY, 0.0f), Quaternion.identity);
    }

    public Vector3 GetMeanBetweenSplinePoints(int pointIndex1, int pointIndex2)
    {
        // Ensure the indices are within the valid range
        if (pointIndex1 < 0 || pointIndex1 >= shape.spline.GetPointCount() ||
            pointIndex2 < 0 || pointIndex2 >= shape.spline.GetPointCount())
        {
            Debug.LogError("Invalid spline point indices.");
            return Vector3.zero; // or handle the error as needed
        }

        // Get the positions of the two points
        Vector3 point1 = shape.spline.GetPosition(pointIndex1);
        Vector3 point2 = shape.spline.GetPosition(pointIndex2);

        // Calculate the mean position
        Vector3 meanPosition = new Vector3(
            (point1.x + point2.x) / 2,
            (point1.y + point2.y) / 2,
            (point1.z + point2.z) / 2
        );

        return meanPosition;
    }

    public Vector3 GetHighestPointBetweenSplinePoints(int pointIndex1, int pointIndex2)
    {
        // Ensure the indices are within the valid range
        if (pointIndex1 < 0 || pointIndex1 >= shape.spline.GetPointCount() ||
            pointIndex2 < 0 || pointIndex2 >= shape.spline.GetPointCount())
        {
            Debug.LogError("Invalid spline point indices.");
            return Vector3.zero; // or handle the error as needed
        }

        // Determine the range for the loop
        int startIndex = Mathf.Min(pointIndex1, pointIndex2);
        int endIndex = Mathf.Max(pointIndex1, pointIndex2);

        // Initialize variables to keep track of the highest point
        Vector3 highestPoint = shape.spline.GetPosition(startIndex);

        // Loop through all points between the two indices
        for (int i = startIndex; i <= endIndex; i++)
        {
            Vector3 currentPoint = shape.spline.GetPosition(i);

            // Check if the current point is higher than the highest point found so far
            if (currentPoint.y > highestPoint.y)
            {
                highestPoint = currentPoint;
            }
        }

        return highestPoint;
    }
}
