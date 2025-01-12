using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectSelector : MonoBehaviour
{
    public enum Vehicle
    {
        HillClimber = 0,
        Motorcycle = 1
    }

    public enum Map
    {
        Ground = 0,
        Moon = 1
    }

    [SerializeField] public Vehicle selectedVehicle = Vehicle.HillClimber;
    [SerializeField] public Map selectedMap = Map.Ground;

    [SerializeField] public bool useInference = true;

    void Start()
    {
        PlayerPrefs.SetInt("Stage", (int) selectedMap);
        PlayerPrefs.SetInt("Vehicle", (int) selectedVehicle);
    }
}
