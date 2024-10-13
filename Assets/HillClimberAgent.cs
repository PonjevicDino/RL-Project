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

    private Text agentGasText;
    private Text agentBrakeText;

    void Start()
    {
        agentGasText = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == "AgentGas").ToList()[0].GetComponent<Text>();
        agentBrakeText = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == "AgentBrake").ToList()[0].GetComponent<Text>();
        OnEpisodeBegin(); 
    }

    public override void OnEpisodeBegin(bool reloadScene = false)
    {
        if (reloadScene)
        {
            SceneManager.LoadScene("ProceduralTest", LoadSceneMode.Single);
        }
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
        sensor.AddObservation(carTransform.gameObject.GetComponent<CarController>().Fuel);

        // Velocity of the player car
        sensor.AddObservation(carRidgidBody.velocityX);
        sensor.AddObservation(carRidgidBody.velocityY);

        // Pitch angle of the player car
        sensor.AddObservation(carRidgidBody.rotation);

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
        foreach (var coin in coins)
        {
            float currentCoinDistance = Vector3.Distance(coin.gameObject.transform.position, transform.parent.position);
            coinDistance = currentCoinDistance > coinDistance ? currentCoinDistance : coinDistance;
        }
        sensor.AddObservation(coinDistance);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Agent inputs
        agentGasText.enabled = GameManager.Instance.GasBtnPressed = Convert.ToBoolean(Mathf.Abs(actions.DiscreteActions[0]));
        agentBrakeText.enabled = GameManager.Instance.BrakeBtnPressed = Convert.ToBoolean(Mathf.Abs(actions.DiscreteActions[1]));

        // Rewards
        // - Level Progress
        if (GameManager.Instance.levelProgress - lastLevelProgress >= 1.0f)
        {
            SetReward(GameManager.Instance.levelProgress - lastLevelProgress);
            lastLevelProgress = GameManager.Instance.levelProgress;
        }
        // - Fuel State
        SetReward(transform.parent.gameObject.GetComponent<CarController>().Fuel / 100.0f);
        // - Collected coin
        int moneyDifference = lastTotalMoney < GameManager.Instance.totalMoney ? GameManager.Instance.totalMoney - lastTotalMoney : 0;
        SetReward(moneyDifference / 10.0f);
        lastTotalMoney += moneyDifference;
        

        // End Episode conditions
        // - No Fuel
        if (transform.parent.gameObject.GetComponent<CarController>().Fuel <= 0.0f)
        {
            EndEpisode(reloadScene: true);
        }
        // - Car on Roof
        if (transform.parent.GetChild(0).GetChild(0).gameObject.GetComponent<HeadScrpit>().headHit)
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
