using UnityEngine;
using System.Collections.Generic;
using NUnit.Framework;

public class CoinGenerator : MonoBehaviour
{
    [SerializeField] private List<Sprite> coinTextures;
    [SerializeField] private float levelProgress;

    public void SpawnCoins(Vector3 spawnPosition)
    {
        GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
        levelProgress = gameManager.levelProgress / 2000.0f;

        // Define the coin values
        int[] coinTypes = { 500, 100, 25, 5 };
        // Calculate the number of coins to spawn based on level progress
        levelProgress = Mathf.Clamp(levelProgress, 0.0f, 1.0f);

        int minCoins = Mathf.FloorToInt(Mathf.Lerp(3, 6, levelProgress));  // Min increases with progress
        int maxCoins = Mathf.FloorToInt(Mathf.Lerp(6, 13, levelProgress)); // Max increases with progress
        int coinCount = Random.Range(minCoins, maxCoins);
        // List to hold the spawned coins
        List<int> spawnedCoins = new List<int>();
        // Spawn coins procedurally, based on progress
        for (int i = 0; i < coinCount; i++)
        {
            // Weighted randomization: Higher levels have more chance to spawn higher value coins
            int coinToSpawn = GetRandomCoinBasedOnProgress(coinTypes, levelProgress);
            spawnedCoins.Add(coinToSpawn);
        }
        // Sort the coins in descending order (highest value first)
        spawnedCoins.Sort((a, b) => b.CompareTo(a));
        // Log the spawned coins
        //Debug.Log("Spawned Coins: " + string.Join(", ", spawnedCoins));
        for (int coinIndex = 0; coinIndex < spawnedCoins.Count; coinIndex++)
        {
            int coinValue = spawnedCoins[coinIndex];
            int index = System.Array.IndexOf(coinTypes, coinValue);

            GameObject coin = gameObject.transform.GetChild(coinIndex).gameObject;
            coin.GetComponent<SpriteRenderer>().sprite = coinTextures[index];
            coin.GetComponent<CollidingObject>().price = coinValue;
            coin.SetActive(true);
        }
    }
    private int GetRandomCoinBasedOnProgress(int[] coinTypes, float levelProgress)
    {
        // Weighted probability based on level progress (higher progress = higher value coins more likely)
        float[] weights = {
            Mathf.Lerp(0.00f, 0.10f, levelProgress), // 500
            Mathf.Lerp(0.05f, 0.20f, levelProgress), // 100
            Mathf.Lerp(0.30f, 0.20f, levelProgress), // 25
            Mathf.Lerp(0.40f, 0.10f, levelProgress)  // 5
        };
        float totalWeight = 0;
        foreach (var weight in weights) totalWeight += weight;
        float randomValue = Random.Range(0, totalWeight);
        for (int i = 0; i < weights.Length; i++)
        {
            if (randomValue < weights[i])
            {
                return coinTypes[i];
            }
            randomValue -= weights[i];
        }
        // Fallback (should never reach here due to weight distribution)
        return coinTypes[coinTypes.Length - 1];
    }
}