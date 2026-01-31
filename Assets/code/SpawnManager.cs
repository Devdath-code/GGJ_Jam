using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject personPrefab;

    [Header("Spawn Points (Left & Right)")]
    public Transform[] spawnPoints; // size = 2

    [Header("Spawn Timing")]
    public float spawnInterval = 1.2f;

    [Header("Population Control")]
    public int maxActivePeople = 80;

    [Header("Infection")]
    [Range(0f, 1f)]
    public float infectedPercentage = 0.2f;

    public List<PersonMovement> activePeople = new List<PersonMovement>();

    void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            if (activePeople.Count < maxActivePeople)
            {
                SpawnPerson();
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnPerson()
    {
        if (personPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("SpawnManager: Missing prefab or spawn points!");
            return;
        }

        Transform spawn =
            spawnPoints[Random.Range(0, spawnPoints.Length)];

        GameObject obj = Instantiate(
            personPrefab,
            spawn.position,
            Quaternion.identity
        );

        PersonMovement person = obj.GetComponent<PersonMovement>();

        if (Random.value < infectedPercentage)
        {
            person.SetInfected();
        }

        activePeople.Add(person);
    }
}
