using UnityEngine;
using System.Collections;

public class PersonLifetime : MonoBehaviour
{
    [Header("Lifetime")]
    public float lifetime = 2f;

    [Header("Exit Movement")]
    public float exitSpeed = 3f;
    public float exitDistanceThreshold = 0.2f;

    SpawnManager spawnManager;
    PersonMovement personMovement;
    Transform exitTarget;

    void Start()
    {
        personMovement = GetComponent<PersonMovement>();
        spawnManager = FindFirstObjectByType<SpawnManager>();

        StartCoroutine(LifeTimer());
    }

    IEnumerator LifeTimer()
    {
        yield return new WaitForSeconds(lifetime);

        ChooseExitPoint();
        StartCoroutine(MoveToExit());
    }

    void ChooseExitPoint()
    {
        if (spawnManager == null || spawnManager.spawnPoints.Length == 0)
            return;

        // Pick left or right randomly
        exitTarget = spawnManager.spawnPoints[
            Random.Range(0, spawnManager.spawnPoints.Length)
        ];
    }

    IEnumerator MoveToExit()
    {
        // Stop roaming while exiting
        if (personMovement != null)
            personMovement.enabled = false;

        while (exitTarget != null &&
               Vector2.Distance(transform.position, exitTarget.position) > exitDistanceThreshold)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                exitTarget.position,
                exitSpeed * Time.deltaTime
            );

            yield return null;
        }

        ExitAndDestroy();
    }

    void ExitAndDestroy()
    {
        if (spawnManager != null && personMovement != null)
        {
            spawnManager.activePeople.Remove(personMovement);
        }

        Destroy(gameObject);
    }
}
