using System.Collections;
using UnityEngine;


public class WaveSpawner : MonoBehaviour
{
    [Range(0f, 1f)]
    public float infectedPercentage = 0.2f; // 20% infected
    public GameObject personPrefab;

    public int startingPeople = 10;
    public int peopleIncreasePerWave = 5;

    public float spawnRadius = 6f;
    public float timeBetweenSpawns = 0.3f;
    public float timeBetweenWaves = 5f;

    int currentWave = 0;

    void Start()
    {
        StartCoroutine(SpawnWaves());
    }

    IEnumerator SpawnWaves()
    {
        while (true)
        {
            currentWave++;
            Debug.Log($"=== WAVE {currentWave} START ===");

            int peopleThisWave =
                startingPeople + (currentWave - 1) * peopleIncreasePerWave;

            for (int i = 0; i < peopleThisWave; i++)
            {
                SpawnPerson();
                yield return new WaitForSeconds(timeBetweenSpawns);
            }

            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    void SpawnPerson()
    {
        Vector2 spawnPos = Random.insideUnitCircle * spawnRadius;
        GameObject obj = Instantiate(personPrefab, spawnPos, Quaternion.identity);

        PersonMovement person = obj.GetComponent<PersonMovement>();

        if (Random.value < infectedPercentage)
        {
            person.SetInfected();
        }
    }

}
