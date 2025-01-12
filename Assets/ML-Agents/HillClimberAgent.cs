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
using System.IO.Abstractions;
using System.IO;

public class HillClimberAgent : Agent
{
    [SerializeField]
    private AgentRewards agentRewards;
    [Serializable]
    private struct AgentRewards
    {
        public bool rewardLevelProgress;
        public float rewardLevelProgressForwardThreshold;
        public float rewardLevelProgressForwardMultiplier;
        public float rewardLevelProgressBackwardThreshold;
        public float rewardLevelProgressBackwardValue;

        public bool rewardGroundDistance;
        public float rewardGroundDistanceThreshold;
        public float rewardGroundDistanceMultiplier;
        public bool rewardGroundDistanceReverse;
        public bool rewardGroundNoPunishment;

        public bool rewardFuel;
        public float rewardFuelThreshold; 
        public float rewardFuelMultiplier;

        public bool rewardIdealSpeed;
        public float rewardIdealSpeedValue;
        public float rewardMinSpeed;
        public float rewardSlowSpeedValue;
        public float rewardMaxSpeed;
        public float rewardFastSpeedValue;

        public bool rewardMoney;
        public float rewardMoneyMultiplier;
    }
    [ExecuteInEditMode]
    void OnValidate()
    {
        agentRewards.rewardLevelProgressForwardThreshold = Mathf.Max(agentRewards.rewardLevelProgressForwardThreshold, 0.0f);
        agentRewards.rewardLevelProgressForwardMultiplier = Mathf.Max(agentRewards.rewardLevelProgressForwardMultiplier, 0.0f);

        agentRewards.rewardLevelProgressBackwardThreshold = Mathf.Max(agentRewards.rewardLevelProgressBackwardThreshold, 0.0f);
        agentRewards.rewardLevelProgressBackwardValue = Mathf.Max(agentRewards.rewardLevelProgressBackwardValue, 0.0f);

        agentRewards.rewardGroundDistanceMultiplier = Mathf.Max(agentRewards.rewardGroundDistanceMultiplier, 0.0f);
        agentRewards.rewardGroundDistanceThreshold = Mathf.Max(agentRewards.rewardGroundDistanceThreshold, 0.1f);

        agentRewards.rewardFuelThreshold = Mathf.Clamp01(agentRewards.rewardFuelThreshold);
        agentRewards.rewardFuelMultiplier = Mathf.Max(agentRewards.rewardFuelMultiplier, 0.0f);

        agentRewards.rewardIdealSpeedValue = Mathf.Max(agentRewards.rewardIdealSpeedValue, 0.0f);
        agentRewards.rewardMinSpeed = Mathf.Clamp(agentRewards.rewardMinSpeed, 0.0f, agentRewards.rewardMaxSpeed);
        agentRewards.rewardSlowSpeedValue = Mathf.Max(agentRewards.rewardSlowSpeedValue, 0.0f);
        agentRewards.rewardMaxSpeed = Mathf.Max(agentRewards.rewardMaxSpeed, agentRewards.rewardMinSpeed);
        agentRewards.rewardFastSpeedValue = Mathf.Max(agentRewards.rewardFastSpeedValue, 0.0f);

        agentRewards.rewardMoneyMultiplier = Mathf.Max(agentRewards.rewardMoneyMultiplier, 0.0f);
    }

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

    private int idealSpeedReward = 0;
    private int tooSlowPunishment = 0;
    private int tooFastPunishment = 0;

    private Vector3 mapEndPosition;

    private bool alreadyFuelStatePunished = false;
    private int totalFuelPunishment = 0;

    private GameManager gameManager;
    private CurriculumUpdater curriculumUpdater;

    void Start()
    {
        gameManager = this.transform.parent.parent.Find("GameManager").GetComponent<GameManager>();
        curriculumUpdater = this.transform.parent.parent.Find("Terrain").GetComponent<CurriculumUpdater>();

        GenerateModelInfo();             
        
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

    private void GenerateModelInfo()
    {
        string fsString = "[" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "]" + "\n" +
                          "Agent Rewards Struct: \n" +
                          "\n" +
                          "Reward | LevelProgress : " + agentRewards.rewardLevelProgress + "\n" +
                          "Reward | LevelProgress | ForwardThreshold : " + agentRewards.rewardLevelProgressForwardThreshold + "\n" +
                          "Reward | LevelProgress | ForwardMultiplier : " + agentRewards.rewardLevelProgressForwardMultiplier + "\n" +
                          "Reward | LevelProgress | BackwardThreshold : " + agentRewards.rewardLevelProgressBackwardThreshold + "\n" +
                          "Reward | LevelProgress | BackwardValue : " + agentRewards.rewardLevelProgressBackwardValue + "\n" +
                          "\n" +
                          "Reward | GroundDistance : " + agentRewards.rewardGroundDistance + "\n" +
                          "Reward | GroundDistance | Threshold : " + agentRewards.rewardGroundDistanceThreshold + "\n" +
                          "Reward | GroundDistance | Multiplier : " + agentRewards.rewardGroundDistanceMultiplier + "\n" +
                          "\n" +
                          "Reward | Fuel : " + agentRewards.rewardFuel + "\n" +
                          "Reward | Fuel | Center : " + agentRewards.rewardFuelThreshold + "\n" +
                          "Reward | Fuel | Multiplier : " + agentRewards.rewardFuelMultiplier + "\n" +
                          "\n" +
                          "Reward | IdealSpeed : " + agentRewards.rewardIdealSpeed + "\n" +
                          "Reward | IdealSpeed | Value : " + agentRewards.rewardIdealSpeedValue + "\n" +
                          "Reward | IdealSpeed | MinSpeed : " + agentRewards.rewardMinSpeed + "\n" +
                          "Reward | IdealSpeed | SlowSpeedValue : " + agentRewards.rewardSlowSpeedValue + "\n" +
                          "Reward | IdealSpeed | MaxSpeed : " + agentRewards.rewardMaxSpeed + "\n" +
                          "Reward | IdealSpeed | FastSpeedValue : " + agentRewards.rewardFastSpeedValue + "\n" +
                          "\n" +
                          "Reward | Money : " + agentRewards.rewardMoney + "\n" +
                          "Reward | Money | Multiplier : " + agentRewards.rewardMoneyMultiplier + "\n\n\n";

        try
        {
            File.AppendAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/Ml-Agents_Model_Info.txt", fsString);

        }
        catch { }
    }

    public override void OnEpisodeBegin(bool reloadScene = false)
    {
        if (!gameManager)
        {
            gameManager = this.transform.parent.parent.Find("GameManager").GetComponent<GameManager>();
        }
        gameManager.isControlledByAI = true;
        if (reloadScene)
        {
            Vector3 carStartPosition = this.transform.parent.parent.Find("Terrain").GetComponent<ChunkSpawner>().ReloadAllChunks();
            transform.parent.SetPositionAndRotation(carStartPosition, Quaternion.identity);
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
            gameManager.isDie = false;
            gameManager.FuelCharge();
            transform.parent.GetChild(0).GetChild(0).gameObject.GetComponent<HeadScript>().headHit = false;
            //SceneManager.LoadScene("ProceduralTest", LoadSceneMode.Single);
        }

        lastLevelProgress = 0.0f;
        lastFuel = 1.0f;
        lastSectionTime = DateTime.Now;

        totalLevelProgressReward = 0.0f;
        totalFuelPunishment = 0;
        totalMoneyReward = 0.0f;
        totalAcceleratorReward = 0.0f;
        totalDistanceToGround = 0.0f;
        
        idleReward = 0;
        idealSpeedReward = 0;
        tooFastPunishment = 0;
        tooSlowPunishment = 0;

        gameManager.totalFuelTanksCollected = 0;
        gameManager.totalCoinsCollected = 0;
        totalAcceleratorHeldSteps = 0;

        //Prefill the Velocity-Histroy
        velocityHistory = new List<Vector3>();
        for (int item = 0; item < 5; item++)
        {
            velocityHistory.Add(Vector3.zero);
        }

        mapName = gameManager.mapName;
        vehicleName = gameManager.vehicleName;
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
        float cameraLeftEdge = -2.8824883f + transform.position.x;
        float cameraRightEdge = cameraLeftEdge + 17.783374f;
        float cameraTopEdge = 6.0f + this.transform.parent.parent.position.y;

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
            if (hit.collider.gameObject.transform.GetComponent<SpriteShapeRenderer>() != null && hit.distance <= 10.0f)
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
            Vector3 raycastSource = new Vector3(cameraLeftEdge + terrainRaycastDistance * raycast, cameraTopEdge + 15.0f, 0.0f);
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
        RaycastHit2D[] distanceToGroundHitsBT = Physics2D.RaycastAll(carTransform.Find("BackTireRaycaster").position, -carTransform.up);
        RaycastHit2D[] distanceToGroundHitsFT = Physics2D.RaycastAll(carTransform.Find("FrontTireRaycaster").position, -carTransform.up);

        distanceToGround = -1.0f;
        float backTireHitDistance = -1.0f;
        
        foreach (RaycastHit2D hit in distanceToGroundHitsBT)
        {
            if (hit.collider.gameObject.transform.GetComponent<SpriteShapeRenderer>() != null && hit.distance <= 10.0f)
            {
                Debug.DrawRay(carTransform.position, -carTransform.up * hit.distance, Color.white);
                backTireHitDistance = hit.distance - 0.1f;
                break;
            }
        }
        if (backTireHitDistance != -1.0f)
        {
            foreach (RaycastHit2D hit in distanceToGroundHitsFT)
            {
                if (hit.collider.gameObject.transform.GetComponent<SpriteShapeRenderer>() != null && hit.distance <= 10.0f)
                {
                    Debug.DrawRay(carTransform.position, -carTransform.up * hit.distance, Color.white);
                    distanceToGround = (hit.distance - 0.1f + backTireHitDistance) / 2;
                    totalDistanceToGround += distanceToGround;
                    break;
                }
            }
        }
        sensor.AddObservation(distanceToGround);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float levelProgressReward = 0.00f;
        float moneyReward = 0.00f;
        float acceleratorReward = 0.00f;


        // Agent inputs
        if (agentGasText != null && agentBrakeText != null)
        {
            agentGasText.enabled = gameManager.GasBtnPressed = actions.DiscreteActions[0] > 0.0f ? true : false;
            agentBrakeText.enabled = gameManager.BrakeBtnPressed = actions.DiscreteActions[1] > 0.0f ? true : false;
        }

        // Rewards
        // - Level Progress
        if (agentRewards.rewardLevelProgress)
        {
            if (gameManager.levelProgress - lastLevelProgress >= agentRewards.rewardLevelProgressForwardThreshold)
            {
                levelProgressReward = (gameManager.levelProgress - lastLevelProgress) + (gameManager.levelProgress / gameManager.mapEndPoint.x) * agentRewards.rewardLevelProgressForwardMultiplier; /*+ Mathf.Max(0, 5.0f - (float)(lastSectionTime - DateTime.Now).TotalSeconds)*/;
                //lastSectionTime = DateTime.Now;
            }
            else if (gameManager.levelProgress - lastLevelProgress < -agentRewards.rewardLevelProgressBackwardThreshold)
            {
                levelProgressReward = -agentRewards.rewardLevelProgressBackwardValue; // Punishment for going backwards
            }
            else
            {
                levelProgressReward = 0.0f;
            }

            AddReward(levelProgressReward);
        }

        if (transform.parent.GetComponent<Rigidbody2D>().velocityX > -0.5f &&
            (transform.parent.GetComponent<Rigidbody2D>().velocityX >= agentRewards.rewardMinSpeed &&
             transform.parent.GetComponent<Rigidbody2D>().velocityX <= agentRewards.rewardMaxSpeed))
        {
            if (agentRewards.rewardIdealSpeed)
            {
                AddReward(agentRewards.rewardIdealSpeedValue);
            }

            idealSpeedReward++;
        }
        else if (transform.parent.GetComponent<Rigidbody2D>().velocityX >= 0.0f &&
                 transform.parent.GetComponent<Rigidbody2D>().velocityX < agentRewards.rewardMinSpeed)
        {
            if (agentRewards.rewardIdealSpeed)
            {
                AddReward(-agentRewards.rewardSlowSpeedValue);
            }
            tooSlowPunishment++;
        }
        else if (transform.parent.GetComponent<Rigidbody2D>().velocityX > agentRewards.rewardMaxSpeed)
        {
            if (agentRewards.rewardIdealSpeed)
            {
                AddReward(-agentRewards.rewardFastSpeedValue);
            }
            tooFastPunishment++;
        }
        
        /*
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
        
        // - Punishment if car is too fast
        if (transform.parent.GetComponent<Rigidbody2D>().velocityX >= 7.0f || transform.parent.GetComponent<Rigidbody2D>().velocityX <= 3.0f)
        {
            AddReward(-0.05f);
            tooFastPunishment += 0.05f;
        }
        */

        // - Distance to Ground
        if (gameManager.levelProgress - lastLevelProgress >= agentRewards.rewardLevelProgressForwardThreshold &&
            agentRewards.rewardGroundDistance)
        {
            if (!agentRewards.rewardGroundDistanceReverse)
            {
                if (agentRewards.rewardGroundNoPunishment)
                {
                    AddReward(Mathf.Max(0.0f, (agentRewards.rewardGroundDistanceThreshold - distanceToGround) * agentRewards.rewardGroundDistanceMultiplier));
                }
                else
                {
                    AddReward((agentRewards.rewardGroundDistanceThreshold - distanceToGround) * agentRewards.rewardGroundDistanceMultiplier);
                }
            }
            else
            {
                if (agentRewards.rewardGroundNoPunishment)
                {
                    AddReward(Mathf.Max(0.0f, (distanceToGround - agentRewards.rewardGroundDistanceThreshold) * agentRewards.rewardGroundDistanceMultiplier));
                }
                else
                {
                    AddReward((distanceToGround - agentRewards.rewardGroundDistanceThreshold) * agentRewards.rewardGroundDistanceMultiplier);
                }
            }
            lastLevelProgress = gameManager.levelProgress;
        }
        else if (gameManager.levelProgress - lastLevelProgress >= agentRewards.rewardLevelProgressForwardThreshold)
        {
            lastLevelProgress = gameManager.levelProgress; // Must be updated even if no reward is given
        }
        // - Fuel State
        // -- Fuel > 75% = Reward
        // -- Fuel < 75% = Punishment
        if (agentRewards.rewardFuel && transform.parent.gameObject.GetComponent<CarController>().Fuel <= agentRewards.rewardFuelThreshold && !alreadyFuelStatePunished)
        {
            alreadyFuelStatePunished = true;
            AddReward(-agentRewards.rewardFuelMultiplier);
            totalFuelPunishment++;
        }
        if (agentRewards.rewardFuel && gameManager.hasCollectedFuel)
        {
            gameManager.hasCollectedFuel = false;
            alreadyFuelStatePunished = false;
            AddReward(agentRewards.rewardFuelMultiplier * 2.0f);
        }
        
        // - Collected coin
        int moneyDifference = lastTotalMoney < gameManager.totalMoney ? gameManager.totalMoney - lastTotalMoney : 0;
        moneyReward = moneyDifference * agentRewards.rewardMoneyMultiplier;
        if (agentRewards.rewardMoney)
        {
            AddReward(moneyReward);
        }
        lastTotalMoney += moneyDifference;

        // - Holding the Pedals
        if (gameManager.GasBtnPressed)
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
        brakeHeldSteps = gameManager.BrakeBtnPressed ? brakeHeldSteps += 1 : 0;
        if (brakeHeldStepsText != null)
        {
            brakeHeldStepsText.enabled = gameManager.BrakeBtnPressed ? true : false;
        }

        // End Episode conditions
        Dictionary<string, float> stats = new Dictionary<string, float>
        {
            { "totalLevelProgress",  gameManager.levelProgress + curriculumUpdater.progressOffset },
            { "totalLevelProgressOffset", curriculumUpdater.progressOffset },
            { "totalLevelProgressReward", totalLevelProgressReward },
            { "totalFuelTanksCollected",  gameManager.totalFuelTanksCollected },
            { "totalFuelPunishment", totalFuelPunishment },
            { "totalCollectedCoins", gameManager.totalCoinsCollected },
            { "totalMoneyReward", totalMoneyReward },
            { "totalAcceleratorHeld", totalAcceleratorHeldSteps },
            { "totalAcceleratorReward", totalAcceleratorReward },
            { "totalDistanceToGround", totalDistanceToGround },
            { "totalIdleReward", idleReward },
            { "idealSpeedReward", idealSpeedReward },
            { "tooSlowPunishment", tooSlowPunishment },
            { "tooFastPunishment", tooFastPunishment },
        };

        // - No Fuel
        if (transform.parent.gameObject.GetComponent<CarController>().Fuel <= 0.0f)
        {
            stats.Add("deathCause", 1.0f);
            curriculumUpdater.UpdateOnEpisodeEnd((int)gameManager.levelProgress);
            
            EndEpisode(stats, mapName, vehicleName, reloadScene: true);
            //gameManager.FuelCharge();
        }
        // - Car on Roof
        else if (transform.parent.GetChild(0).GetChild(0).gameObject.GetComponent<HeadScript>().headHit)
        {
            stats.Add("deathCause", 2.0f);
            curriculumUpdater.UpdateOnEpisodeEnd((int)gameManager.levelProgress);
            EndEpisode(stats, mapName, vehicleName, reloadScene: true);
        }
        // - Car out of Map
        else if (transform.parent.localPosition.y <= -50.0f)
        {
            stats.Add("deathCause", 3.0f);
            curriculumUpdater.UpdateOnEpisodeEnd((int)gameManager.levelProgress);
            EndEpisode(stats, mapName, vehicleName, reloadScene: true);
        }
        // - Car reached Goal
        else if (transform.parent.position.x >= gameManager.mapEndPoint.x - 1.0f)
        {
            AddReward(100.0f);
            stats.Add("deathCause", 0.0f);
            curriculumUpdater.UpdateOnEpisodeEnd((int)gameManager.levelProgress);
            EndEpisode(stats, mapName, vehicleName, reloadScene: true);
        }

        // Print Debug Info
        if (rewardInfoText != null)
        {
            PrintRewardInfo(levelProgressReward, 0, moneyReward, acceleratorReward);
        }
    }

    #region --> Total Rewards for Debug
    private float totalLevelProgressReward = 0.0f;
    private int totalFuelStateReward = 0;
    private float totalMoneyReward = 0.0f;
    private float totalAcceleratorReward = 0.0f;
    #endregion

    private void PrintRewardInfo(float levelProgressReward, float fuelStateReward, float moneyReward, float acceleratorReward)
    {
        if (printRewardInfo)
        {
            rewardInfoText.SetActive(true);
        }
       
        levelProgressRewardText.text = "LVL-Prog: " + levelProgressReward.ToString("00.000");
        fuelStateRewardText.text = "Fuel: " + fuelStateReward.ToString(".00000");
        moneyRewardText.text = "Coins: " + moneyReward.ToString(".000");
        acceleratorRewardText.text = "ACC-Mult." + acceleratorReward.ToString("0.0000");

        acceleratorHeldStepsText.text = acceleratorHeldSteps + "x";
        brakeHeldStepsText.text = brakeHeldSteps + "x";

        totalLevelProgressReward += levelProgressReward;
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
