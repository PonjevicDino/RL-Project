using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.SceneManagement;
using System;
using System.Linq;
using UnityEngine.U2D;
using UnityEngine.UI;

public class HillClimberAgent : Agent
{
    private float lastLevelProgress = 0.0f;
    private int lastTotalMoney;
    private float lastFuel;
    private List<Vector3> velocityHistory;
    private DateTime lastSectionTime;
    private int acceleratorHeldSteps = 0;

    private Text agentGasText;
    private Text agentBrakeText;

    void Start()
    {
        agentGasText = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == "AgentGas").ToList()[0].GetComponent<Text>();
        agentBrakeText = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == "AgentBrake").ToList()[0].GetComponent<Text>();
    }

    public override void OnEpisodeBegin(bool reloadScene = false)
    {
        GameManager.Instance.isControlledByAI = true;
        if (reloadScene)
        {
            Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == "Terrain").ToList()[0].GetComponent<ChunkSpawner>().ReloadAllChunks();
            transform.parent.SetPositionAndRotation(new Vector3(-5.8757452f, 0.527813f, 0.0f), Quaternion.identity);
            transform.parent.gameObject.GetComponent<Rigidbody2D>().velocity = Vector3.zero;
            GameManager.Instance.isDie = false;
            GameManager.Instance.FuelCharge();
            transform.parent.GetChild(0).GetChild(0).gameObject.GetComponent<HeadScript>().headHit = false;
            //SceneManager.LoadScene("ProceduralTest", LoadSceneMode.Single);
        }
        lastLevelProgress = 0.0f;

        //Prefill the Velocity-Histroy
        velocityHistory = new List<Vector3>();
        for (int item = 0; item < 5; item++)
        {
            velocityHistory.Add(Vector3.zero);
        }
        lastFuel = 1.0f;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = Input.GetKey(KeyCode.RightArrow) ? 1 : 0;
        discreteActionsOut[1] = Input.GetKey(KeyCode.LeftArrow) ? 1 : 0;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Transform carTransform = transform.parent;
        Rigidbody2D carRidgidBody = carTransform.gameObject.GetComponent<Rigidbody2D>();

        // Fuel state
        float currentFuel = carTransform.gameObject.GetComponent<CarController>().Fuel;
        sensor.AddObservation(currentFuel);
        // Fuel consumption
        float fuelConsumption = currentFuel != lastFuel ? lastFuel - currentFuel : 0.0f;
        sensor.AddObservation(fuelConsumption);
        lastFuel = currentFuel;

        // Velocity of the player car
        sensor.AddObservation(carRidgidBody.velocity.normalized);
        // History: Velocity of the player car
        velocityHistory.RemoveAt(0);
        velocityHistory.Add(carRidgidBody.velocity);
        for (int historyIndex = 0; historyIndex < velocityHistory.Count(); historyIndex++)
        {
            sensor.AddObservation(velocityHistory[0].normalized);
        }

        // Pitch angle of the player car
        sensor.AddObservation(carRidgidBody.rotation / 180.0f);

        // Distance between player car and terrain to hit
        // Normal rotation vector on terrain hit
        Vector2 carTrajectory = new Vector2(carRidgidBody.velocityX, carRidgidBody.velocityY);
        RaycastHit2D[] hits = Physics2D.RaycastAll(carTransform.position, carTrajectory);
        bool hitTerrain = false;
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider.gameObject.transform.GetComponent<SpriteShapeRenderer>() != null)
            {
                Debug.DrawRay(carTransform.position, carTrajectory * 999.0f, Color.red);
                Debug.Log("Trajectory | Hit distance: " + hit.distance.ToString("00.000"));
                sensor.AddObservation(hit.distance);
                sensor.AddObservation(hit.normal);
                hitTerrain = true;
                break;
            }
        }
        if (!hitTerrain)
        {
            Debug.DrawRay(carTransform.position, carTrajectory * 999.9f, Color.red);
            Debug.Log("Trajectory | upwards...");
            sensor.AddObservation(1000.0f);
            sensor.AddObservation(Vector2.zero);
        }

        // Distance to next coin
        float coinDistance = 999.9f;
        var coins = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name.Contains("Coin "));
        GameObject nearestCoin = this.gameObject;
        foreach (var coin in coins)
        {
            float currentCoinDistance = Vector3.Distance(coin.gameObject.transform.position, transform.parent.position);
            if (currentCoinDistance < coinDistance)
            {
                coinDistance = currentCoinDistance;
                nearestCoin = coin.gameObject;
            }
        }
        sensor.AddObservation(coinDistance);
        // Angle to next coin
        Vector3.Angle(transform.parent.position, nearestCoin.transform.position);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Agent inputs
        if (agentGasText != null && agentBrakeText != null)
        {
            agentGasText.enabled = GameManager.Instance.GasBtnPressed = actions.DiscreteActions[0] > 0.0f ? true : false;
            agentBrakeText.enabled = GameManager.Instance.BrakeBtnPressed = actions.DiscreteActions[1] > 0.0f ? true : false;
        }

        // Rewards
        // - Level Progress
        if (GameManager.Instance.levelProgress - lastLevelProgress >= 10.0f)
        {
            SetReward((GameManager.Instance.levelProgress - lastLevelProgress) + Mathf.Max(0, 10.0f - (float)(lastSectionTime - DateTime.Now).TotalSeconds));
            lastLevelProgress = GameManager.Instance.levelProgress;
            lastSectionTime = DateTime.Now;
        }
        else if (GameManager.Instance.levelProgress - lastLevelProgress < 0.0f)
        {
            SetReward(-0.05f); // Punishment for going backwards
        }
        // - Fuel State
        SetReward((1.0f - transform.parent.gameObject.GetComponent<CarController>().Fuel) / 10000.0f);
        // - Collected coin
        int moneyDifference = lastTotalMoney < GameManager.Instance.totalMoney ? GameManager.Instance.totalMoney - lastTotalMoney : 0;
        SetReward(moneyDifference / 100.0f);
        lastTotalMoney += moneyDifference;
        // - Holding the Gas-Pedal
        if (GameManager.Instance.GasBtnPressed)
        {
            acceleratorHeldSteps++;
            if (acceleratorHeldSteps > 3)
            {
                AddReward(0.1f);  // Small reward for holding accelerator
            }
        }
        else
        {
            acceleratorHeldSteps = 0;
        }


        // End Episode conditions
        // - No Fuel
        if (transform.parent.gameObject.GetComponent<CarController>().Fuel <= 0.0f)
        {
            EndEpisode(reloadScene: true);
        }
        // - Car on Roof
        if (transform.parent.GetChild(0).GetChild(0).gameObject.GetComponent<HeadScript>().headHit)
        {
            EndEpisode(reloadScene: true);
        }
        // - Car out of Map
        if (transform.parent.position.y <= -100.0f)
        {
            EndEpisode(reloadScene: true);
        }
        // - Car reached Goal
        if (transform.parent.position.x >= 9900.0f)
        {
            SetReward(1000.0f);
            EndEpisode(reloadScene: true);
        }
    }
}
