using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject personPrefab;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    [Header("Wave Settings")]
    public float spawnInterval = 0.6f;
    public float breakBetweenWaves = 5f;

    [Header("Wave 1")]
    public int wave1People = 30;
    public int wave1Infected = 3;

    [Header("Wave 2")]
    public int wave2People = 40;
    public int wave2Infected = 8;

    [Header("Wave 3")]
    public int wave3People = 55;
    public int wave3Infected = 15;

    [Header("Tracking")]
    public List<PersonMovement> activePeople = new List<PersonMovement>();

    private int currentWave = 0;

    void Start()
    {
        StartCoroutine(WaveRoutine());
    }

    IEnumerator WaveRoutine()
    {
        yield return new WaitForSeconds(1f);

        // Wave 1
        currentWave = 1;
        yield return StartCoroutine(SpawnWave(wave1People, wave1Infected));
        yield return new WaitForSeconds(breakBetweenWaves);

        // Wave 2
        currentWave = 2;
        yield return StartCoroutine(SpawnWave(wave2People, wave2Infected));
        yield return new WaitForSeconds(breakBetweenWaves);

        // Wave 3
        currentWave = 3;
        yield return StartCoroutine(SpawnWave(wave3People, wave3Infected));

        Debug.Log("✅ All waves completed!");
    }

    IEnumerator SpawnWave(int totalPeople, int infectedPeople)
    {
        if (personPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("SpawnManager: Missing prefab or spawn points!");
            yield break;
        }

        infectedPeople = Mathf.Clamp(infectedPeople, 0, totalPeople);

        // Decide which spawn indices are infected
        HashSet<int> infectedIndexes = new HashSet<int>();
        while (infectedIndexes.Count < infectedPeople)
        {
            infectedIndexes.Add(Random.Range(0, totalPeople));
        }

        Debug.Log($"🌊 Wave {currentWave} started: {totalPeople} people, {infectedPeople} infected");

        for (int i = 0; i < totalPeople; i++)
        {
            Transform spawn = spawnPoints[Random.Range(0, spawnPoints.Length)];

            GameObject obj = Instantiate(personPrefab, spawn.position, Quaternion.identity);

            PersonMovement person = obj.GetComponent<PersonMovement>();
            if (person != null)
            {
                if (infectedIndexes.Contains(i))
                {
                    person.isInfected = true;
                    person.infectionProgress = 1f;
                }

                activePeople.Add(person);
            }

            yield return new WaitForSeconds(spawnInterval);
        }

        Debug.Log($"✅ Wave {currentWave} completed!");
    }
}
