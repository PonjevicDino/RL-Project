using NUnit.Framework;
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
    [SerializeField] private int terrainYOffset = 10;

    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private GameObject fuelTankPrefab;
    [SerializeField] private PhysicsMaterial2D material;

    public Vector3 endPoint;
    public Vector3 dummySpawnPoint;

    public void SpawnTerrain(ContinuousFourierTransform terrain)
    {
        //Rename the gameObject for better debugging
        shape = transform.GetComponent<SpriteShapeController>();

        Vector3[] points = terrain.ComputeFourierTransform();

        //Pull the two points underneath the ground further down so the ground covers the whole lower screen
        shape.spline.SetPosition(0, shape.spline.GetPosition(0) + Vector3.down * 200);
        shape.spline.SetPosition(3, shape.spline.GetPosition(3) + Vector3.down * 200);

        //Set the lower right point to the end of the chunk, so a flat plane comes out
        Vector3 position = shape.spline.GetPosition(3);
        shape.spline.SetPosition(3, new Vector3(points[points.Length-1].x, position.y));

        //The new endpoint.y needs to be precalculated, since otherwise it would always lay on the same height as the start
        shape.spline.SetPosition(2, points[0]);

        for (int i = 0; i < points.Length; i++)
        {
            //Afterwards insert the point into the spline
            shape.spline.InsertPointAt(i + 2, points[i]);
        }

        //The condition must be <(numOfPoints-1) otherwise the right side won´t be flatten!
        for (int i = 2; i < points.Length + 2; i++)
        {
            shape.spline.SetTangentMode(i, ShapeTangentMode.Continuous);
            shape.spline.SetLeftTangent(i, new Vector3(-5, 0, 0));
            shape.spline.SetRightTangent(i, new Vector3(5, 0, 0));
        }

        //Lastly set the collision, so the vehicle can drive on the surface
        EdgeCollider2D collider = transform.gameObject.AddComponent<EdgeCollider2D>();
        collider.sharedMaterial = material;

        PlaceCoinsOnTerrain();
        PlaceFuelTanksOnTerrain();
    }

    public void SpawnNewFourierChunk(Vector3[] points, int chunkNumber, Vector3 previousEndPoint, GameObject parent = null)
    {
        bool dummy = (parent != null);
        //Rename the gameObject for better debugging
        string dummyString = "";
        if (dummy) { dummyString = " Dummy"; }
        this.gameObject.name = "Chunk " + (chunkNumber + 1) + dummyString;

        if (!dummy) {
            shape = transform.GetComponent<SpriteShapeController>(); 
        }
        else 
        { 
            shape = parent.GetComponent<SpriteShapeController>();
            Destroy(transform.GetComponent<SpriteShapeController>());
        }

        int offset = chunkNumber * numOfPoints;
        if (dummy)
        {
            endPoint = new Vector3(previousEndPoint.x + points[offset + numOfPoints].x, points[offset + numOfPoints].y);
        }
        else
        {
            endPoint = points[offset + numOfPoints];
        }

        if (dummy)
        {
            //Pull the two points underneath the ground further down so the ground covers the whole lower screen
            shape.spline.SetPosition(0, shape.spline.GetPosition(0) + Vector3.down * 20);
            shape.spline.SetPosition(3, shape.spline.GetPosition(3) + Vector3.down * 20);

            //Set the start of the ground to the endpoint.y of the previous chunk to keep the correct height
            shape.spline.SetPosition(1, new Vector3(shape.spline.GetPosition(1).x, previousEndPoint.y));

            //Set the lower right point to the end of the chunk, so a flat plane comes out
            shape.spline.SetPosition(3, new Vector3(endPoint.x, shape.spline.GetPosition(3).y));

            shape.spline.SetPosition(2, new Vector3(shape.spline.GetPosition(3).x, endPoint.y));

            for (int i = 0; i < numOfPoints*2; i++)
            {
                //Afterwards insert the point into the spline
                if (i != 0)
                {
                    shape.spline.InsertPointAt(i + 1, new Vector3(points[offset + i].x, points[offset + i].y));
                }
            }

            //The condition must be <(numOfPoints-1) otherwise the right side won´t be flatten!
            for (int i = 2; i < numOfPoints*2 + 1; i++)
            {
                shape.spline.SetTangentMode(i, ShapeTangentMode.Continuous);
                shape.spline.SetLeftTangent(i, new Vector3(-5, 0, 0));
                shape.spline.SetRightTangent(i, new Vector3(5, 0, 0));
            }

            //Lastly set the collision, so the vehicle can drive on the surface
            EdgeCollider2D collider = parent.gameObject.AddComponent<EdgeCollider2D>();
            collider.sharedMaterial = material;

            float spawnOffset = previousEndPoint.x - points[offset].x - 2;

            PlaceCoins(spawnOffset);
            PlaceFuelTanks(spawnOffset);
        }
    }

    public void SpawnChunkBasedOnFourier(Vector3[] points, int chunkNumber, Vector3 previousEndPoint)
    {
        Debug.Log("Spawned Chunk " + chunkNumber);
        //Rename the gameObject for better debugging
        this.gameObject.name = "Chunk " + (chunkNumber + 1);
        shape = transform.GetComponent<SpriteShapeController>();

        //int offset = chunkNumber * (numOfPoints / 2); //For overlapping spawn
        int offset = chunkNumber * numOfPoints; //For overlapping spawn

        endPoint = points[offset + numOfPoints];
        dummySpawnPoint = points[offset + (numOfPoints/2)];

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

        //The condition must be <(numOfPoints-1) otherwise the right side won´t be flatten!
        for (int i = 2; i < numOfPoints + 1; i++)
        {
            shape.spline.SetTangentMode(i, ShapeTangentMode.Continuous);
            shape.spline.SetLeftTangent(i, new Vector3(-5, 0, 0));
            shape.spline.SetRightTangent(i, new Vector3(5, 0, 0));
        }

        //Lastly set the collision, so the vehicle can drive on the surface
        EdgeCollider2D collider = transform.gameObject.AddComponent<EdgeCollider2D>();
        collider.sharedMaterial = material;
        
        float spawnOffset = chunkNumber * scale - 2;

        PlaceCoins(spawnOffset);
        PlaceFuelTanks(spawnOffset);
    }

    public void SpawnChunk(int chunkNumber, Vector3 previousEndpoint)
    {
        //Rename the gameObject for better debugging
        this.gameObject.name = "Chunk " + (chunkNumber + 1);
        shape = transform.GetComponent<SpriteShapeController>();

        //Pull the two points underneath the ground further down so the ground covers the whole lower screen
        shape.spline.SetPosition(0, shape.spline.GetPosition(0) + Vector3.down * 20);
        shape.spline.SetPosition(3, shape.spline.GetPosition(3) + Vector3.down * 20);

        //Set the start of the ground to the endpoint.y of the previous chunk to keep the correct height
        shape.spline.SetPosition(1, new Vector3(shape.spline.GetPosition(1).x, previousEndpoint.y));

        //Set the lower right point to the end of the chunk, so a flat plane comes out
        shape.spline.SetPosition(3, shape.spline.GetPosition(3) + Vector3.right * (scale - 2));

        //The new endpoint.y needs to be precalculated, since otherwise it would always lay on the same height as the start
        endPoint = new Vector3(shape.spline.GetPosition(3).x, (hillHeight + chunkNumber * 2) * Mathf.PerlinNoise((numOfPoints - 1) * Random.Range(5.0f, 15.0f), 0.0f) - terrainYOffset, 0.0f);
        shape.spline.SetPosition(2, endPoint);

        for (int i = 0; i < (numOfPoints - 1); i++)
        {
            //The xPos is set based on the current i and the yPos by the perlin noise
            float xPos = shape.spline.GetPosition(i + 1).x + distanceBetweenPoints;
            float yPos = (hillHeight + chunkNumber * 2) * Mathf.PerlinNoise(xPos * Random.Range(5.0f, 15.0f), 0.0f) - terrainYOffset;

            //Afterwards insert the point into the spline
            shape.spline.InsertPointAt(i + 2, new Vector3(xPos, yPos));
        }

        //The condition must be <(numOfPoints-1) otherwise the right side won´t be flatten!
        for (int i = 2; i < (numOfPoints - 1) + 2; i++)
        {
            shape.spline.SetTangentMode(i, ShapeTangentMode.Continuous);
            shape.spline.SetLeftTangent(i, new Vector3(-5, 0, 0));
            shape.spline.SetRightTangent(i, new Vector3(5, 0, 0));
        }

        //Lastly set the collision, so the vehicle can drive on the surface
        EdgeCollider2D collider = transform.gameObject.AddComponent<EdgeCollider2D>();
        collider.sharedMaterial = material;

        int spawnOffset = chunkNumber * scale - 2;

        PlaceCoins(spawnOffset);
        PlaceFuelTanks(spawnOffset);
    }

    private void PlaceCoinsOnTerrain()
    {
        for (int splinePoint = 3; splinePoint < shape.spline.GetPointCount() - 1; splinePoint += 3)
        {
            int spawnSplinePoint = splinePoint + Random.Range(-1, 1);
            Vector3 heightOffset = GetHighestPointBetweenSplinePoints(spawnSplinePoint - 1, spawnSplinePoint + 1);
            Vector3 spawnPosition = shape.spline.GetPosition(spawnSplinePoint);
            GameObject coinPrefabInstance = Instantiate(coinPrefab,
                                                        new Vector3(spawnPosition.x, heightOffset.y + 1),
                                                        Quaternion.identity, this.transform);
            coinPrefabInstance.GetComponent<CoinGenerator>().SpawnCoins(spawnPosition);
        }
    }

    private void PlaceFuelTanksOnTerrain()
    {
        for (int splinePoint = 3; splinePoint < shape.spline.GetPointCount() - 1; splinePoint++)
        {
            if (splinePoint % 7 == 0 || splinePoint % 10 == 0)
            {
                Vector3 middle = GetMeanBetweenSplinePoints(splinePoint, splinePoint + 1);
                Instantiate(fuelTankPrefab, new Vector3(middle.x, middle.y + 1),
                                                        Quaternion.identity, this.transform);
            }
        }
    }

    private void PlaceCoins(float spawnOffset)
    {
        for (int splinePoint = 3; splinePoint < shape.spline.GetPointCount()-2; splinePoint += 3)
        {
            int spawnSplinePoint = splinePoint + Random.Range(-1, 1);
            Vector3 heightOffset = new Vector3(0.0f, 0.0f);//GetHighestPointBetweenSplinePoints(spawnSplinePoint - 1, spawnSplinePoint + 1);
            Vector3 spawnPosition = shape.spline.GetPosition(spawnSplinePoint);
            GameObject coinPrefabInstance = Instantiate(coinPrefab,
                                                        new Vector3(spawnPosition.x, heightOffset.y + 1)  + Vector3.right * spawnOffset,
                                                        Quaternion.identity, this.transform);
            coinPrefabInstance.GetComponent<CoinGenerator>().SpawnCoins(spawnPosition);
        }
    }

    private void PlaceFuelTanks(float spawnOffset)
    {
        Vector3 middle = GetMeanBetweenSplinePoints(7, 8);
        Instantiate(fuelTankPrefab, new Vector3(middle.x, middle.y + 1) + Vector3.right * spawnOffset,
                                                Quaternion.identity, this.transform);
        middle = GetMeanBetweenSplinePoints(10,11);
        Instantiate(fuelTankPrefab, new Vector3(middle.x, middle.y + 1) + Vector3.right * spawnOffset,
                                                Quaternion.identity, this.transform);
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
