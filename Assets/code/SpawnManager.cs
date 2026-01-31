using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject personPrefab;

    [Header("Spawn Points (Left & Right)")]
    public Transform[] spawnPoints;

    [Header("Population Control")]
    public int maxActivePeople = 80;

    [Header("Infection")]
    [Range(0f, 1f)]
    public float infectedPercentage = 0.2f;

    [Header("Wave Settings")]
    public int startWaveSize = 10;
    public int waveIncrease = 5;
    public float timeBetweenWaves = 5f;
    public float spawnDelayInWave = 0.2f;

    public List<PersonMovement> activePeople = new List<PersonMovement>();

    private int currentWave = 0;
    private bool spawningWave = false;

    void Start()
    {
        StartCoroutine(WaveLoop());
    }

    IEnumerator WaveLoop()
    {
        while (true)
        {
            activePeople.RemoveAll(p => p == null);

            if (!spawningWave && activePeople.Count < maxActivePeople)
            {
                currentWave++;
                int waveSize = startWaveSize + (waveIncrease * (currentWave - 1));

                spawningWave = true;
                yield return StartCoroutine(SpawnWave(waveSize));
                spawningWave = false;

                yield return new WaitForSeconds(timeBetweenWaves);
            }

            yield return null;
        }
    }

    IEnumerator SpawnWave(int waveSize)
    {
        int spawned = 0;

        while (spawned < waveSize)
        {
            activePeople.RemoveAll(p => p == null);

            if (activePeople.Count >= maxActivePeople)
            {
                yield return null;
                continue;
            }

            SpawnPerson();
            spawned++;

            yield return new WaitForSeconds(spawnDelayInWave);
        }
    }

    void SpawnPerson()
    {
        if (personPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("SpawnManager: Missing prefab or spawn points!");
            return;
        }

        Transform spawn = spawnPoints[Random.Range(0, spawnPoints.Length)];

        GameObject obj = Instantiate(personPrefab, spawn.position, Quaternion.identity);

        PersonMovement person = obj.GetComponent<PersonMovement>();

        if (person != null)
        {
            if (Random.value < infectedPercentage)
                person.SetInfected();

            activePeople.Add(person);
        }
    }
}
