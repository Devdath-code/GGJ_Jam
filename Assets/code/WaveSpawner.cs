using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    [Header("Population Settings")]
    [Range(0f, 1f)]
    public float infectedPercentage = 0.2f;
    public GameObject personPrefab;
    public int startingPeople = 30; // Increased from 10
    public int peopleIncreasePerWave = 10;

    [Header("Spawn Settings")]
    public float timeBetweenSpawns = 0.2f;
    public float timeBetweenWaves = 5f;
    public float screenEdgeOffset = 1f; // Distance outside camera view

    [Header("Leave Settings")]
    public float minTimeBeforeLeaving = 8f;
    public float maxTimeBeforeLeaving = 15f;
    public float leaveSpeed = 3f;

    private int currentWave = 0;
    private List<GameObject> activePeople = new List<GameObject>();
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        StartCoroutine(SpawnWaves());
        StartCoroutine(ManageLeavingPeople());
    }

    IEnumerator SpawnWaves()
    {
        while (true)
        {
            currentWave++;
            Debug.Log($"=== WAVE {currentWave} START ===");

            int peopleThisWave = startingPeople + (currentWave - 1) * peopleIncreasePerWave;

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
        // Get camera bounds
        float camHeight = mainCamera.orthographicSize;
        float camWidth = camHeight * mainCamera.aspect;

        // Choose random edge (0=top, 1=right, 2=bottom, 3=left)
        int edge = Random.Range(0, 4);
        Vector2 spawnPos = Vector2.zero;
        Vector2 targetPos = Vector2.zero;

        switch (edge)
        {
            case 0: // Top
                spawnPos = new Vector2(
                    Random.Range(-camWidth, camWidth),
                    camHeight + screenEdgeOffset
                );
                targetPos = new Vector2(
                    Random.Range(-camWidth * 0.7f, camWidth * 0.7f),
                    Random.Range(-camHeight * 0.5f, camHeight * 0.7f)
                );
                break;

            case 1: // Right
                spawnPos = new Vector2(
                    camWidth + screenEdgeOffset,
                    Random.Range(-camHeight, camHeight)
                );
                targetPos = new Vector2(
                    Random.Range(-camWidth * 0.5f, camWidth * 0.7f),
                    Random.Range(-camHeight * 0.7f, camHeight * 0.7f)
                );
                break;

            case 2: // Bottom
                spawnPos = new Vector2(
                    Random.Range(-camWidth, camWidth),
                    -camHeight - screenEdgeOffset
                );
                targetPos = new Vector2(
                    Random.Range(-camWidth * 0.7f, camWidth * 0.7f),
                    Random.Range(-camHeight * 0.7f, camHeight * 0.5f)
                );
                break;

            case 3: // Left
                spawnPos = new Vector2(
                    -camWidth - screenEdgeOffset,
                    Random.Range(-camHeight, camHeight)
                );
                targetPos = new Vector2(
                    Random.Range(-camWidth * 0.7f, camWidth * 0.5f),
                    Random.Range(-camHeight * 0.7f, camHeight * 0.7f)
                );
                break;
        }

        // Spawn person
        GameObject obj = Instantiate(personPrefab, spawnPos, Quaternion.identity);
        PersonMovement person = obj.GetComponent<PersonMovement>();

        // Set if infected
        if (Random.value < infectedPercentage)
        {
            person.SetInfected();
        }

        // Add entering behavior
        StartCoroutine(EnterScene(person, targetPos));

        // Track active people
        activePeople.Add(obj);
    }

    IEnumerator EnterScene(PersonMovement person, Vector2 targetPos)
    {
        // Disable normal movement while entering
        person.enabled = false;

        // Move to target position
        while (Vector2.Distance(person.transform.position, targetPos) > 0.1f)
        {
            person.transform.position = Vector2.MoveTowards(
                person.transform.position,
                targetPos,
                leaveSpeed * Time.deltaTime
            );
            yield return null;
        }

        // Enable normal behavior
        person.enabled = true;
    }

    IEnumerator ManageLeavingPeople()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(2f, 4f));

            // Only make people leave if we have enough
            if (activePeople.Count > 15)
            {
                // Remove null entries
                activePeople.RemoveAll(p => p == null);

                if (activePeople.Count > 0)
                {
                    // Pick random person to leave
                    int randomIndex = Random.Range(0, activePeople.Count);
                    GameObject personToLeave = activePeople[randomIndex];

                    if (personToLeave != null)
                    {
                        StartCoroutine(MakePersonLeave(personToLeave));
                        activePeople.RemoveAt(randomIndex);
                    }
                }
            }
        }
    }

    IEnumerator MakePersonLeave(GameObject person)
    {
        PersonMovement movement = person.GetComponent<PersonMovement>();
        if (movement == null) yield break;

        // Wait random time before leaving
        yield return new WaitForSeconds(Random.Range(minTimeBeforeLeaving, maxTimeBeforeLeaving));

        // Disable normal movement
        movement.enabled = false;

        // Choose exit point (opposite side or random)
        float camHeight = mainCamera.orthographicSize;
        float camWidth = camHeight * mainCamera.aspect;

        int exitEdge = Random.Range(0, 4);
        Vector2 exitPos = Vector2.zero;

        switch (exitEdge)
        {
            case 0: // Top
                exitPos = new Vector2(person.transform.position.x, camHeight + screenEdgeOffset);
                break;
            case 1: // Right
                exitPos = new Vector2(camWidth + screenEdgeOffset, person.transform.position.y);
                break;
            case 2: // Bottom
                exitPos = new Vector2(person.transform.position.x, -camHeight - screenEdgeOffset);
                break;
            case 3: // Left
                exitPos = new Vector2(-camWidth - screenEdgeOffset, person.transform.position.y);
                break;
        }

        // Move to exit
        while (person != null && Vector2.Distance(person.transform.position, exitPos) > 0.1f)
        {
            person.transform.position = Vector2.MoveTowards(
                person.transform.position,
                exitPos,
                leaveSpeed * Time.deltaTime
            );
            yield return null;
        }

        // Destroy person
        if (person != null)
        {
            Destroy(person);
        }
    }

    void Update()
    {
        // Debug info
        if (Input.GetKeyDown(KeyCode.Space)) 
        {
            activePeople.RemoveAll(p => p == null);
            Debug.Log($"Active people: {activePeople.Count}");
        }
    }
}