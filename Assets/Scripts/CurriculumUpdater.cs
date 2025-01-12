using InspectorGadgets.Attributes;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CurriculumUpdater : MonoBehaviour
{
    [SerializeField] private bool enableCurrUpdate = false;

    [SerializeField, Range(0.0f, 0.89f)] private float mapStartPos = 0.5f;
    [SerializeField, Range(0.9f, 0.99f)] private float mapEndPos = 0.9f;

    [SerializeField] private int increaseThrProgress = 1000;
    [SerializeField] private int increaseThrCount = 10;
    [SerializeField, Range(0.001f, 0.1f)] private float increaseFactor = 0.01f;

    [HideInInspector] public float progressOffset = 0.0f; 

    private List<int> agentProgressHist = new List<int>();

    [Readonly, SerializeField] private int totalMapSteps = 0;

    void OnValidate()
    {
        mapStartPos = Mathf.Max(mapStartPos, GetComponent<ChunkSpawner>().fourierThreshold); 
        increaseThrProgress = (int)Mathf.Min(increaseThrProgress, (1.0f - mapEndPos) * 19689);
        totalMapSteps = (int)((mapEndPos - mapStartPos) / increaseFactor);
    }

    void Start()
    {
        for (int entry = 0; entry < increaseThrCount; entry++)
        {
            agentProgressHist.Add(0);
        }
    }

    public void UpdateOnEpisodeEnd(int levelProgress)
    {
        if (!enableCurrUpdate)
        {
            return;
        }

        agentProgressHist.RemoveAt(0);
        agentProgressHist.Add(levelProgress);
        if (agentProgressHist.Average() > increaseThrProgress)
        {
            SetNewMapStart();
            agentProgressHist = new List<int>();
            Start();
        }
    }

    private void SetNewMapStart()
    {
        float newMapStart = GetComponent<ChunkSpawner>().fourierThreshold + increaseFactor;
        newMapStart = Mathf.Clamp(newMapStart, mapStartPos, mapEndPos);
        GetComponent<ChunkSpawner>().fourierThreshold = newMapStart;
        progressOffset = 19689 * (newMapStart - mapStartPos);
    }
}
