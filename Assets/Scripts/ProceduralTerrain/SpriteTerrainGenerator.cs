using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class SpriteTerrainGenerator : MonoBehaviour
{
    [System.Serializable]
    public struct FourierTransformation
    {
        public float initialFrequency;     // Standard frequency
        public float initialAmplitude;     // Initial amplitude to start higher
        public float amplitudeIncrease;    // Increase in amplitude
        public int pointCount;             // Number of points to render
        public float initialPointSpacing;  // Initial spacing between points
        public float spacingIncreaseFactor; // Factor by which to increase spacing
        public float randomnessFactor;     // Initial randomness factor
    }

    [SerializeField] private int numOfPoints = 10;
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private GameObject fuelTankPrefab;
    [SerializeField] private PhysicsMaterial2D material;
    [SerializeField] private Vector3 gravity = new Vector3(0.0f, -9.81f);
    [SerializeField] public FourierTransformation fourierTransformation;

    public Vector3 endPoint;
    private SpriteShapeController shape;

    private void Start()
    {
        Physics.gravity = gravity;
    }

    public void SpawnChunkBasedOnFourier(Vector3[] points, int chunkNumber, Vector3 previousEndPoint)
    {
        //Rename the gameObject for better debugging
        this.gameObject.name = "Chunk " + (chunkNumber + 1);
        shape = transform.GetComponent<SpriteShapeController>();

        int offset = chunkNumber * numOfPoints; 

        endPoint = points[offset + numOfPoints];

        //Pull the two points underneath the ground further down so the ground covers the whole lower screen
        shape.spline.SetPosition(0, shape.spline.GetPosition(0) + Vector3.down * 20);
        shape.spline.SetPosition(3, shape.spline.GetPosition(3) + Vector3.down * 20);

        //Set the start of the ground to the endpoint.y of the previous chunk to keep the correct height
        shape.spline.SetPosition(1, new Vector3(shape.spline.GetPosition(1).x, previousEndPoint.y));

        //Set the lower right point to the end of the chunk, so a flat plane comes out
        shape.spline.SetPosition(3, new Vector3(endPoint.x - points[offset].x, shape.spline.GetPosition(3).y));

        shape.spline.SetPosition(2, new Vector3(shape.spline.GetPosition(3).x, endPoint.y));

        for (int i = 0; i < numOfPoints; i++)
        {
            //Afterwards insert the point into the spline
            if (i != 0)
            {
                shape.spline.InsertPointAt(i + 1, new Vector3(points[offset + i].x - points[offset].x, points[offset + i].y));
            }
        }

        shape.spline.SetTangentMode(1, ShapeTangentMode.Broken);
        shape.spline.SetRightTangent(1, new Vector3(5, 0, 0));

        //The condition must be <(numOfPoints-1) otherwise the right side won´t be flatten!
        for (int i = 2; i < numOfPoints + 1; i++)
        {
            shape.spline.SetTangentMode(i, ShapeTangentMode.Continuous);
            shape.spline.SetLeftTangent(i, new Vector3(-5, 0, 0));
            shape.spline.SetRightTangent(i, new Vector3(5, 0, 0));
        }

        shape.spline.SetTangentMode(numOfPoints + 1, ShapeTangentMode.Broken);
        shape.spline.SetLeftTangent(numOfPoints + 1, new Vector3(-5, 0, 0));

        float spawnOffset = previousEndPoint.x;

        PlaceCoins(spawnOffset);
        PlaceFuelTanks(spawnOffset);

        EdgeCollider2D collider = transform.gameObject.AddComponent<EdgeCollider2D>();
        collider.sharedMaterial = material;
    }
    private void PlaceCoins(float spawnOffset)
    {
        for (int splinePoint = 3; splinePoint < shape.spline.GetPointCount()-2; splinePoint += 3)
        {
            int spawnSplinePoint = splinePoint + Random.Range(-1, 1);
            Vector3 heightOffset = GetHighestPointBetweenSplinePoints(splinePoint, spawnSplinePoint) + new Vector3(0.0f, 2.0f);
            Vector3 spawnPosition = GetMeanBetweenSplinePoints(splinePoint, spawnSplinePoint);
            GameObject coinPrefabInstance = Instantiate(coinPrefab,
                                                        new Vector3(spawnPosition.x, heightOffset.y + 3.0f) + Vector3.right * spawnOffset + Vector3.right,
                                                        Quaternion.identity, transform);
            coinPrefabInstance.GetComponent<CoinGenerator>().SpawnCoins(spawnPosition);
        }
    }

    private void PlaceFuelTanks(float spawnOffset)
    {
        Vector3 fuelTankSpawnPoint = shape.spline.GetPosition(5);
        Vector3 heightOffset = GetHighestPointBetweenSplinePoints(4, 6) + new Vector3(0.0f, 2.0f);
        Instantiate(fuelTankPrefab, new Vector3(fuelTankSpawnPoint.x, heightOffset.y + 1) + Vector3.right * spawnOffset,
                                                Quaternion.identity, transform);
        fuelTankSpawnPoint = shape.spline.GetPosition(10);
        heightOffset = GetHighestPointBetweenSplinePoints(9, 11) + new Vector3(0.0f, 2.0f);
        Instantiate(fuelTankPrefab, new Vector3(fuelTankSpawnPoint.x - 1, heightOffset.y + 1) + Vector3.right * spawnOffset,
                                                Quaternion.identity, transform);
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
