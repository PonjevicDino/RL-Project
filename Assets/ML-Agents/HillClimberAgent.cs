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
using Unity.Burst.CompilerServices;
using TMPro;

public class HillClimberAgent : Agent
{
    [SerializeField] private string categoryName;

    [SerializeField] private bool printRewardInfo = true;
    private GameObject rewardInfoText;
    private TextMeshProUGUI levelProgressRewardText;
    private TextMeshProUGUI fuelStateRewardText;
    private TextMeshProUGUI moneyRewardText;
    private TextMeshProUGUI acceleratorRewardText;
    private Text acceleratorHeldStepsText;
    private Text brakeHeldStepsText;

    [SerializeField] private int terrainRaycastsInViewport = 11; // Value should be uneven for better Raycast separation
    private float terrainRaycastDistance = 0.0f;

    private float lastLevelProgress = 0.0f;
    private int lastTotalMoney;
    private float lastFuel;
    private List<Vector3> velocityHistory;
    private DateTime lastSectionTime;
    private int acceleratorHeldSteps = 0;
    private int brakeHeldSteps = 0;
    private float distanceToGround = 0.0f; 

    private Text agentGasText;
    private Text agentBrakeText;

    private string mapName = string.Empty;
    private string vehicleName = string.Empty;
    private int totalAcceleratorHeldSteps = 0;
    private float totalDistanceToGround = 0.0f;
    private int idleReward = 0;
    private int tooFastPunishment = 0;

    void Start()
    {
        agentGasText = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == "AgentGas").ToList()[0].GetComponent<Text>();
        agentBrakeText = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == "AgentBrake").ToList()[0].GetComponent<Text>();

        rewardInfoText = GameObject.Find("RewardInfoBox");
        levelProgressRewardText = GameObject.Find("LevelProgressReward").GetComponent<TextMeshProUGUI>();
        fuelStateRewardText = GameObject.Find("FuelStateReward").GetComponent<TextMeshProUGUI>();
        moneyRewardText = GameObject.Find("MoneyReward").GetComponent<TextMeshProUGUI>();
        acceleratorRewardText = GameObject.Find("AcceleratorReward").GetComponent<TextMeshProUGUI>();
        rewardInfoText.SetActive(false);

        acceleratorHeldStepsText = GameObject.Find("AcceleratorHoldText").GetComponent<Text>();
        brakeHeldStepsText = GameObject.Find("BrakeHoldText").GetComponent<Text>();

        terrainRaycastDistance = 17.783374f / (terrainRaycastsInViewport - 1);
    }

    public override void OnEpisodeBegin(bool reloadScene = false)
    {
        GameManager.Instance.isControlledByAI = true;
        if (reloadScene)
        {
            Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == "Terrain").ToList()[0].GetComponent<ChunkSpawner>().ReloadAllChunks();
            transform.parent.SetPositionAndRotation(new Vector3(3.108311f, 4.527813f, 0.0f), Quaternion.identity);
            transform.parent.gameObject.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            transform.parent.gameObject.GetComponent<Rigidbody2D>().angularVelocity = 0.0f;
            transform.parent.gameObject.GetComponent<Rigidbody2D>().Sleep();
            transform.parent.Find("FrontTire").gameObject.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            transform.parent.Find("FrontTire").gameObject.GetComponent<Rigidbody2D>().angularVelocity = 0.0f;
            transform.parent.Find("FrontTire").gameObject.GetComponent<Rigidbody2D>().Sleep();
            transform.parent.Find("BackTire").gameObject.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            transform.parent.Find("BackTire").gameObject.GetComponent<Rigidbody2D>().angularVelocity = 0.0f;
            transform.parent.Find("BackTire").gameObject.GetComponent<Rigidbody2D>().Sleep();
            transform.parent.Find("driver-head").gameObject.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            transform.parent.Find("driver-head").gameObject.GetComponent<Rigidbody2D>().angularVelocity = 0.0f;
            transform.parent.Find("driver-head").gameObject.GetComponent<Rigidbody2D>().Sleep();
            GameManager.Instance.isDie = false;
            GameManager.Instance.FuelCharge();
            transform.parent.GetChild(0).GetChild(0).gameObject.GetComponent<HeadScript>().headHit = false;
            //SceneManager.LoadScene("ProceduralTest", LoadSceneMode.Single);
        }

        lastLevelProgress = 0.0f;
        lastFuel = 1.0f;
        lastSectionTime = DateTime.Now;

        totalLevelProgressReward = 0.0f;
        totalFuelStateReward = 0.0f;
        totalMoneyReward = 0.0f;
        totalAcceleratorReward = 0.0f;
        totalDistanceToGround = 0.0f;
        idleReward = 0;
        tooFastPunishment = 0;

        GameManager.Instance.totalFuelTanksCollected = 0;
        GameManager.Instance.totalCoinsCollected = 0;
        totalAcceleratorHeldSteps = 0;

        //Prefill the Velocity-Histroy
        velocityHistory = new List<Vector3>();
        for (int item = 0; item < 5; item++)
        {
            velocityHistory.Add(Vector3.zero);
        }

        mapName = GameManager.Instance.mapName;
        vehicleName = GameManager.Instance.vehicleName;
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

        // Viewport values for later usage
        float cameraLeftEdge = Camera.main.ViewportToWorldPoint(Vector3.zero).x;
        float cameraRightEdge = Camera.main.ViewportToWorldPoint(Vector3.right).x;
        float cameraTopEdge = Camera.main.ViewportToWorldPoint(Vector3.up).y;

        // Fuel state
        float currentFuel = carTransform.gameObject.GetComponent<CarController>().Fuel;
        sensor.AddObservation(currentFuel);
        // Fuel consumption
        float fuelConsumption = currentFuel != lastFuel ? lastFuel - currentFuel : 0.0f;
        sensor.AddObservation(fuelConsumption);
        lastFuel = currentFuel;
        // Fuel tank distance (if in viewport)
        float fuelTankDistance = 999.9f;
        var fuelTanks = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name.Contains("FuelTank"));
        GameObject nearestFuelTank = this.gameObject;
        foreach (var fuelTank in fuelTanks)
        {
            float currentFuelTankDistance = Vector3.Distance(fuelTank.gameObject.transform.position, transform.parent.position);
            if (currentFuelTankDistance < fuelTankDistance && (fuelTank.transform.position.x >= cameraLeftEdge && fuelTank.transform.position.x <= cameraRightEdge))
            {
                fuelTankDistance = currentFuelTankDistance;
                nearestFuelTank = fuelTank.gameObject;
            }
        }
        sensor.AddObservation(fuelTankDistance);
        // Angle to next Fuel Tank
        float fuelTankAngle = 0.0f;
        if (nearestFuelTank != this.gameObject)
        {
           fuelTankAngle = Vector3.Angle(transform.parent.position, nearestFuelTank.transform.position);
        }
        sensor.AddObservation(fuelTankAngle);

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
        float trajectoryHitDistance = 1000.0f;
        Vector3 trajectoryHitNormal = Vector3.zero;
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider.gameObject.transform.GetComponent<SpriteShapeRenderer>() != null)
            {
                Debug.DrawRay(carTransform.position, carTrajectory * hit.distance, Color.green);
                //Debug.Log("Trajectory | Hit distance: " + hit.distance.ToString("00.000"));
                hitTerrain = true;
                trajectoryHitDistance = hit.distance;
                trajectoryHitNormal = hit.normal;
                break;
            }
        }
        if (!hitTerrain)
        {
            Debug.DrawRay(carTransform.position, carTrajectory * 999.9f, Color.red);
            //Debug.Log("Trajectory | upwards...");
        }
        sensor.AddObservation(trajectoryHitDistance);
        sensor.AddObservation(trajectoryHitNormal);

        // Distance to next coin - if coin in Viewport
        float coinDistance = 999.9f;
        var coins = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name.Contains("Coin "));
        GameObject nearestCoin = this.gameObject;
        foreach (var coin in coins)
        {
            float currentCoinDistance = Vector3.Distance(coin.gameObject.transform.position, transform.parent.position);
            if (currentCoinDistance < coinDistance && (coin.transform.position.x >= cameraLeftEdge && coin.transform.position.x <= cameraRightEdge))
            {
                coinDistance = currentCoinDistance;
                nearestCoin = coin.gameObject;
            }
        }
        sensor.AddObservation(coinDistance);
        // Angle to next coin
        float coinAngle = 0.0f;
        if (nearestCoin != this.gameObject)
        {
            coinAngle = Vector3.Angle(transform.parent.position, nearestCoin.transform.position);
        }
        sensor.AddObservation(coinAngle);


        // Terrain shape details in Viewport
        List<float> terrainHeights = new List<float>();
        for (int raycast = 0; raycast < terrainRaycastsInViewport; raycast++)
        {
            Vector3 raycastSource = new Vector3(cameraLeftEdge + terrainRaycastDistance * raycast, cameraTopEdge + 50.0f, 0.0f);
            hits = Physics2D.RaycastAll(raycastSource, Vector3.down);
            hitTerrain = false;
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.gameObject.transform.GetComponent<SpriteShapeRenderer>() != null)
                {
                    Debug.DrawRay(raycastSource, Vector3.down * hit.distance, Color.yellow);
                    terrainHeights.Add(hit.point.y);
                    hitTerrain = true;
                    break;
                }
            }
            if (!hitTerrain)
            {
                Debug.DrawRay(raycastSource, Vector3.down * 999.0f, Color.red);
                terrainHeights.Add(-1.0f);
            }
        }
        sensor.AddObservation(terrainHeights);

        // Get distance to ground
        RaycastHit2D[] distanceToGroundHits = Physics2D.RaycastAll(carTransform.position, -carTransform.up);
        distanceToGround = -1.0f;
        foreach (RaycastHit2D hit in distanceToGroundHits)
        {
            if (hit.collider.gameObject.transform.GetComponent<SpriteShapeRenderer>() != null)
            {
                Debug.DrawRay(carTransform.position, -carTransform.up * hit.distance, Color.white);
                distanceToGround = hit.distance;
                totalDistanceToGround += distanceToGround;
                break;
            }
        }
        sensor.AddObservation(distanceToGround);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float levelProgressReward = 0.00f;
        float fuelStateReward = 0.00f;
        float moneyReward = 0.00f;
        float acceleratorReward = 0.00f;


        // Agent inputs
        if (agentGasText != null && agentBrakeText != null)
        {
            agentGasText.enabled = GameManager.Instance.GasBtnPressed = actions.DiscreteActions[0] > 0.0f ? true : false;
            agentBrakeText.enabled = GameManager.Instance.BrakeBtnPressed = actions.DiscreteActions[1] > 0.0f ? true : false;
        }

        // Rewards
        // - Level Progress
        if (GameManager.Instance.levelProgress - lastLevelProgress >= 1.0f)
        {
            levelProgressReward = (GameManager.Instance.levelProgress - lastLevelProgress) /*+ Mathf.Max(0, 5.0f - (float)(lastSectionTime - DateTime.Now).TotalSeconds)*/;
            AddReward(levelProgressReward);
            //lastSectionTime = DateTime.Now;
        }
        else if (GameManager.Instance.levelProgress - lastLevelProgress < -5.0f)
        {
            levelProgressReward = -0.05f;
            AddReward(levelProgressReward); // Punishment for going backwards
        }
        else
        {
            levelProgressReward = 0.0f;
        }

        // - No input action if car is moving
        if (transform.parent.GetComponent<Rigidbody2D>().velocityX >= 3.0f && !GameManager.Instance.BrakeBtnPressed)
        {
            AddReward(0.001f);
            idleReward++;
        }
        // - Punishment if agent doesn't do anything
        else if (transform.parent.GetComponent<Rigidbody2D>().velocityX <= 1.0f && !GameManager.Instance.GasBtnPressed && !GameManager.Instance.BrakeBtnPressed)
        {
            AddReward(-0.001f);
            idleReward--;
        }

        // - Punishment if car too fast
        if (transform.parent.GetComponent<Rigidbody2D>().velocityX >= 7.0f && GameManager.Instance.GasBtnPressed)
        {
            AddReward(-0.005f);
            tooFastPunishment++;
        }

        // - Distance to Ground
        if (GameManager.Instance.levelProgress - lastLevelProgress >= 1.0f)
        {
            AddReward((2.0f - distanceToGround) / 50.0f);
            lastLevelProgress = GameManager.Instance.levelProgress;
        }
        // - Fuel State
        // -- Fuel > 75% = Reward
        // -- Fuel < 75% = Punishment
        fuelStateReward = (transform.parent.gameObject.GetComponent<CarController>().Fuel - 0.75f) / 1000.0f; 
        //SetReward(fuelStateReward);
        // - Collected coin
        int moneyDifference = lastTotalMoney < GameManager.Instance.totalMoney ? GameManager.Instance.totalMoney - lastTotalMoney : 0;
        moneyReward = moneyDifference / 100.0f;
        //SetReward(moneyReward);
        lastTotalMoney += moneyDifference;
        // - Holding the Pedals
        if (GameManager.Instance.GasBtnPressed)
        {
            totalAcceleratorHeldSteps++;
            acceleratorHeldSteps++;
            acceleratorHeldStepsText.enabled = true;
            if (acceleratorHeldSteps > 3)
            {
                acceleratorReward = 0.01f * acceleratorHeldSteps / 5000.0f;
                //AddReward(acceleratorReward);  // Small reward for holding accelerator
            }
        }
        else
        {
            if (acceleratorHeldStepsText != null)
            {
                acceleratorHeldStepsText.enabled = false;
            }
            acceleratorHeldSteps = 0;
        }
        brakeHeldSteps = GameManager.Instance.BrakeBtnPressed ? brakeHeldSteps += 1 : 0;
        if (brakeHeldStepsText != null)
        {
            brakeHeldStepsText.enabled = GameManager.Instance.BrakeBtnPressed ? true : false;
        }

        // End Episode conditions
        Dictionary<string, float> stats = new Dictionary<string, float>
        {
            { "totalLevelProgress",  GameManager.Instance.levelProgress },
            { "totalLevelProgressReward", totalLevelProgressReward },
            { "totalFuelTanksCollected",  GameManager.Instance.totalFuelTanksCollected },
            { "totalFuelStateReward", totalFuelStateReward },
            { "totalCollectedCoins", GameManager.Instance.totalCoinsCollected },
            { "totalMoneyReward", totalMoneyReward },
            { "totalAcceleratorHeld", totalAcceleratorHeldSteps },
            { "totalAcceleratorReward", totalAcceleratorReward },
            { "totalDistanceToGround", totalDistanceToGround },
            { "totalIdleReward", idleReward },
            { "totalTooFastPunishment", tooFastPunishment },
        };

        // - No Fuel
        if (transform.parent.gameObject.GetComponent<CarController>().Fuel <= 0.0f)
        {
            stats.Add("deathCause", 1.0f);
            EndEpisode(stats, mapName, vehicleName, reloadScene: true);
        }
        // - Car on Roof
        else if (transform.parent.GetChild(0).GetChild(0).gameObject.GetComponent<HeadScript>().headHit)
        {
            stats.Add("deathCause", 2.0f);
            EndEpisode(stats, mapName, vehicleName, reloadScene: true);
        }
        // - Car out of Map
        else if (transform.parent.position.y <= -100.0f)
        {
            stats.Add("deathCause", 3.0f);
            EndEpisode(stats, mapName, vehicleName, reloadScene: true);
        }
        // - Car reached Goal
        else if (transform.parent.position.x >= 19900.0f)
        {
            AddReward(1000.0f);
            stats.Add("deathCause", 0.0f);
            EndEpisode(stats, mapName, vehicleName, reloadScene: true);
        }

        // Print Debug Info
        if (printRewardInfo && rewardInfoText != null)
        {
            PrintRewardInfo(levelProgressReward, fuelStateReward, moneyReward, acceleratorReward);
        }
    }

    #region --> Total Rewards for Debug
    private float totalLevelProgressReward = 0.0f;
    private float totalFuelStateReward = 0.0f;
    private float totalMoneyReward = 0.0f;
    private float totalAcceleratorReward = 0.0f;
    #endregion

    private void PrintRewardInfo(float levelProgressReward, float fuelStateReward, float moneyReward, float acceleratorReward)
    {
        rewardInfoText.SetActive(true);
       
        levelProgressRewardText.text = "LVL-Prog: " + levelProgressReward.ToString("00.000");
        fuelStateRewardText.text = "Fuel: " + fuelStateReward.ToString(".00000");
        moneyRewardText.text = "Coins: " + moneyReward.ToString(".000");
        acceleratorRewardText.text = "ACC-Mult." + acceleratorReward.ToString("0.0000");

        acceleratorHeldStepsText.text = acceleratorHeldSteps + "x";
        brakeHeldStepsText.text = brakeHeldSteps + "x";

        totalLevelProgressReward += levelProgressReward;
        totalFuelStateReward += fuelStateReward;
        totalMoneyReward += moneyReward;
        totalAcceleratorReward += acceleratorReward;
        //Debug.Log("Rewards: LVL - " + totalLevelProgressReward + " | Fuel - " + totalFuelStateReward +
        //          " | Money - " + totalMoneyReward + " | ACC - " + totalAcceleratorReward);
    }
    void DisplayScores(Dictionary<string, int> scores)
    {
        foreach (KeyValuePair<string, int> entry in scores)
        {
            Debug.Log($"Player: {entry.Key}, Score: {entry.Value}");
        }
    }
}
