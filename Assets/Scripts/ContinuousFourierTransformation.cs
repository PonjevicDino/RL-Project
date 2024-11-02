using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static SpriteTerrainGenerator;

public class ContinuousFourierTransform : MonoBehaviour
{
    private Vector3[] points;
    private LineRenderer lineRenderer;

    private void Start()
    {
        // Get the LineRenderer component attached to this GameObject
        //lineRenderer = GetComponent<LineRenderer>();

        // Precompute the Fourier transformation points
        //ComputeFourierTransform(fourierTransformation);
        //UpdateRenderer();

        //Random.InitState(0);
    }

    public Vector3[] ComputeFourierTransform(SpriteTerrainGenerator.FourierTransformation fourierTransformation, Vector3 offset, float threshold)
    {
        int fourierThreshold = (int)(fourierTransformation.pointCount * (threshold % 1.0f));
        Vector3 fourierThresholdOffset = Vector3.zero;

        float amplitude = fourierTransformation.initialAmplitude;
        points = new Vector3[fourierTransformation.pointCount - fourierThreshold];

        float randomOffsetForPerlin = Random.Range(-100.0f, 100.0f);

        for (int i = 0; i < fourierTransformation.pointCount; i++)
        {
            float randomnessFactorOffset = (1 + (i / (float)fourierTransformation.pointCount));

            // Calculate the x position, increasing spacing further into the function
            float x = i * (fourierTransformation.initialPointSpacing +
                          (i * (fourierTransformation.spacingIncreaseFactor / randomnessFactorOffset)));

            // Increase the randomness factor over time
            float currentRandomnessFactor = fourierTransformation.randomnessFactor * randomnessFactorOffset;

            // Add randomness to the amplitude based on the increasing randomness factor
            float randomAmplitude = Random.Range(-currentRandomnessFactor, currentRandomnessFactor);

            // Reduce frequency to make oscillations less frequent
            float adjustedFrequency = fourierTransformation.initialFrequency * 0.5f; // Reduce by 50%

            // Use Perlin noise with a scaled x value to make the function smoother and less frequent
            float y = Mathf.PerlinNoise((x + randomOffsetForPerlin) * 0.1f, 0.0f) * (amplitude + randomAmplitude); // Scale x by 0.1 to reduce frequency

            // Update amplitude for the next point
            amplitude += fourierTransformation.amplitudeIncrease; // Increase amplitude for the next point

            if (i == fourierThreshold)
            {
                fourierThresholdOffset = new Vector3(x, y, 0) + offset;
            }

            if (i >= fourierThreshold)
            {
                // Assign the calculated point position
                points[i-fourierThreshold] = new Vector3(x - fourierThresholdOffset.x, y, 0) + offset;
            }
        }

        points[0].y = points[1].y + fourierTransformation.initialAmplitude; // Make sure the first point leads into a valley
        return points;
    }

    private void UpdateRenderer()
    {
        // Set the number of positions in the LineRenderer
        lineRenderer.positionCount = points.Length;

        // Update the positions in the LineRenderer
        for (int i = 0; i < points.Length; i++)
        {
            lineRenderer.SetPosition(i, points[i]);
        }
    }
}

