using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SpriteTerrainGenerator;

public class ContinuousFourierTransform : MonoBehaviour
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

    private Vector3[] points;
    private LineRenderer lineRenderer;
    [SerializeField] public FourierTransformation fourierTransformation;

    private void Start()
    {
        // Get the LineRenderer component attached to this GameObject
        //lineRenderer = GetComponent<LineRenderer>();

        // Precompute the Fourier transformation points
        //ComputeFourierTransform(fourierTransformation);
        //UpdateRenderer();

        //Random.InitState(0);
    }

    public Vector3[] ComputeFourierTransform(SpriteTerrainGenerator.FourierTransformation fourierTransformation, Vector3 offset)
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
            //float y = Mathf.Sin(x * fourierTransformation.initialFrequency) * (amplitude + randomAmplitude);
            float y = Mathf.PerlinNoise(x, 0.0f) * (amplitude + randomAmplitude) * 10.0f;

            // Make sure the first point leads into a valley
            if (i == 0)
            {
                y = amplitude; // Start higher for the first point
            }

            // Update amplitude for the next point
            amplitude += fourierTransformation.amplitudeIncrease; // Increase amplitude for the next point

            // Assign the calculated point position
            points[i] = new Vector3(x, y, 0) + offset;
        }

        //UpdateRenderer();
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

