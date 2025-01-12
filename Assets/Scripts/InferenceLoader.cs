using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Policies;
using Unity.Sentis;
using UnityEngine;

public class InferenceLoader : MonoBehaviour
{
    [SerializeField] private ModelAsset hillClimberGround;
    [SerializeField] private ModelAsset hillClimberMoon;
    [SerializeField] private ModelAsset motorcycleGround;
    [SerializeField] private ModelAsset motorcycleMoon;


    void Start()
    {
        if (GameObject.Find("EnvLoader").GetComponent<ObjectSelector>().useInference)
        {
            if (GameObject.Find("EnvLoader").GetComponent<ObjectSelector>().selectedMap == ObjectSelector.Map.Ground)
            {
                if (GameObject.Find("EnvLoader").GetComponent<ObjectSelector>().selectedVehicle == ObjectSelector.Vehicle.HillClimber)
                {
                    GetComponent<BehaviorParameters>().Model = hillClimberGround;
                }
                else
                {
                    GetComponent<BehaviorParameters>().Model = motorcycleGround;
                }
            }
            else
            {
                if (GameObject.Find("EnvLoader").GetComponent<ObjectSelector>().selectedVehicle == ObjectSelector.Vehicle.HillClimber)
                {
                    GetComponent<BehaviorParameters>().Model = hillClimberMoon;
                }
                else
                {
                    GetComponent<BehaviorParameters>().Model = motorcycleMoon;
                }
            }
        }
    }
}
