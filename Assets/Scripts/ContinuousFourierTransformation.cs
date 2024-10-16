using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SpriteTerrainGenerator;

public class ContinuousFourierTransform : MonoBehaviour
{
    [SerializeField] private float initialFrequency = 1f; // Standard frequency
    [SerializeField] private float initialAmplitude = 0.125f; // Initial amplitude to start higher
    [SerializeField] private float amplitudeIncrease = 0.125f; // Increase in amplitude
    [SerializeField] private int pointCount = 100; // Number of points to render
    [SerializeField] private float initialPointSpacing = 0.1f; // Initial spacing between points
    [SerializeField] private float spacingIncreaseFactor = 0.01f; // Factor by which to increase spacing
    [SerializeField] private float randomnessFactor = 0.5f; // Initial randomness factor

    private Vector3[] points;
    private LineRenderer lineRenderer;

    private void Start()
    {
        // Get the LineRenderer component attached to this GameObject
        lineRenderer = GetComponent<LineRenderer>();

        // Precompute the Fourier transformation points
        //ComputeFourierTransform();
        //UpdateRenderer();
    }

    public Vector3[] ComputeFourierTransform(SpriteTerrainGenerator.FourierTransformation fourierTransformation)
    {
        float amplitude = fourierTransformation.initialAmplitude;
        points = new Vector3[fourierTransformation.pointCount];

        for (int i = 0; i < fourierTransformation.pointCount; i++)
        {
            float randomnessFactorOffset = (1 + (i / (float)fourierTransformation.pointCount));

            // Calculate the x position, increasing spacing further into the function
            float x = i * (fourierTransformation.initialPointSpacing + (i * (fourierTransformation.spacingIncreaseFactor / randomnessFactorOffset)));

            // Increase the randomness factor over time
            float currentRandomnessFactor = fourierTransformation.randomnessFactor * randomnessFactorOffset;

            // Add randomness to the amplitude based on the increasing randomness factor
            float randomAmplitude = Random.Range(-currentRandomnessFactor, currentRandomnessFactor);
            float y = Mathf.Sin(x * fourierTransformation.initialFrequency) * (amplitude + randomAmplitude);

            // Make sure the first point leads into a valley
            if (i == 0)
            {
                y = amplitude; // Start higher for the first point
            }

            // Update amplitude for the next point
            amplitude += fourierTransformation.amplitudeIncrease; // Increase amplitude for the next point

            // Assign the calculated point position
            points[i] = new Vector3(x, y, 0);
        }

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

