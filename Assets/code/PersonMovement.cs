using UnityEngine;
using System.Collections.Generic;

public class PersonMovement : MonoBehaviour
{
    public bool isMasked = false;

    [Header("Infection")]
    public bool isInfected = false;
    [Range(0f, 1f)]
    public float infectionProgress = 0f;
    public float infectionTimeRequired = 3f;
    public float infectionRadius = 0.8f;
    private Dictionary<PersonMovement, float> proximityTimers = new Dictionary<PersonMovement, float>();

    [Header("Movement")]
    public float moveSpeed = 1.5f;
    public float roamRadius = 5f;
    public float stoppingDistance = 1.2f;

    [Header("Social Behavior")]
    public float conversationDuration = 5f; // How long to stay in group
    public float detectionRadius = 3f; // How far they can see others
    public int maxGroupSize = 4;

    private SpriteRenderer sr;
    private Vector2 targetPos;
    private bool isMoving = false;
    private bool isInConversation = false;
    private float conversationTimer = 0f;
    private List<PersonMovement> currentGroup = new List<PersonMovement>();

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        ChooseNewAction();
        UpdateColor();
    }

    void Update()
    {
        if (isInConversation)
        {
            HandleConversation();
        }
        else if (isMoving)
        {
            HandleMovement();
        }

        if (isInfected)
        {
            TryInfectOthers();
        }

        // Clean up proximity timers for people who moved away
        CleanupProximityTimers();
    }

    void HandleMovement()
    {
        // Check if we're close to someone to start a conversation
        PersonMovement nearbyPerson = FindNearbyPerson();
        if (nearbyPerson != null && !nearbyPerson.isInConversation)
        {
            StartConversation(nearbyPerson);
            return;
        }

        // Continue moving to target
        transform.position = Vector2.MoveTowards(
            transform.position,
            targetPos,
            moveSpeed * Time.deltaTime
        );

        if (Vector2.Distance(transform.position, targetPos) < 0.1f)
        {
            isMoving = false;
            Invoke(nameof(ChooseNewAction), Random.Range(1f, 3f));
        }
    }

    void HandleConversation()
    {
        conversationTimer += Time.deltaTime;

        // End conversation after duration
        if (conversationTimer >= conversationDuration)
        {
            EndConversation();
        }
    }

    PersonMovement FindNearbyPerson()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius);

        foreach (Collider2D hit in hits)
        {
            if (hit.transform == transform) continue;

            PersonMovement other = hit.GetComponent<PersonMovement>();
            if (other != null && !other.isInConversation)
            {
                // Check if their group isn't full
                if (other.currentGroup.Count < maxGroupSize)
                {
                    float distance = Vector2.Distance(transform.position, other.transform.position);
                    if (distance < stoppingDistance)
                    {
                        return other;
                    }
                }
            }
        }

        return null;
    }

    void StartConversation(PersonMovement other)
    {
        isInConversation = true;
        isMoving = false;
        CancelInvoke(nameof(ChooseNewAction));
        conversationTimer = 0f;

        // Join their group or create new one
        if (other.currentGroup.Count > 0)
        {
            currentGroup = other.currentGroup;
        }
        else
        {
            currentGroup = new List<PersonMovement> { other };
            other.currentGroup = currentGroup;
            other.isInConversation = true;
            other.isMoving = false;
            other.CancelInvoke(nameof(ChooseNewAction));
            other.conversationTimer = 0f;
        }

        if (!currentGroup.Contains(this))
        {
            currentGroup.Add(this);
        }
    }

    void EndConversation()
    {
        isInConversation = false;
        currentGroup.Clear();
        Invoke(nameof(ChooseNewAction), Random.Range(0.5f, 1.5f));
    }

    void ChooseNewAction()
    {
        Vector2 newTarget;
        int attempts = 0;
        do
        {
            newTarget = (Vector2)transform.position + Random.insideUnitCircle * roamRadius;
            attempts++;
        }
        while (!IsInsideBounds(newTarget) && attempts < 10);

        targetPos = newTarget;
        isMoving = true;
    }

    bool IsInsideBounds(Vector2 pos)
    {
        return pos.x > -5.5f && pos.x < 5.5f &&
               pos.y > -5.5f && pos.y < 5.5f;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            ChooseNewAction();
        }
    }

    void UpdateColor()
    {
        if (sr == null) return;
        Color healthy = Color.white;
        Color infected = Color.red;
        sr.color = Color.Lerp(healthy, infected, infectionProgress);
    }

    public void SetInfected()
    {
        isInfected = true;
        infectionProgress = 1f;
        UpdateColor();
    }

    void TryInfectOthers()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            infectionRadius
        );

        foreach (Collider2D hit in hits)
        {
            if (hit.transform == transform) continue;

            PersonMovement other = hit.GetComponent<PersonMovement>();
            if (other != null && !other.isInfected)
            {
                // Track how long they've been near each other
                if (!proximityTimers.ContainsKey(other))
                {
                    proximityTimers[other] = 0f;
                }

                proximityTimers[other] += Time.deltaTime;

                // Only infect if they've been close for a while (especially in conversation)
                float requiredProximityTime = isInConversation ? 1f : 2f;

                if (proximityTimers[other] >= requiredProximityTime)
                {
                    // Slower infection rate
                    float infectionRate = isInConversation ? 1f : 0.5f;
                    other.infectionProgress += (Time.deltaTime / infectionTimeRequired) * infectionRate;
                    other.infectionProgress = Mathf.Clamp01(other.infectionProgress);
                    other.UpdateColor();

                    if (other.infectionProgress >= 1f)
                    {
                        other.SetInfected();
                    }
                }
            }
        }
    }

    void CleanupProximityTimers()
    {
        List<PersonMovement> toRemove = new List<PersonMovement>();

        foreach (var kvp in proximityTimers)
        {
            if (kvp.Key == null || Vector2.Distance(transform.position, kvp.Key.transform.position) > infectionRadius)
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var person in toRemove)
        {
            proximityTimers.Remove(person);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, infectionRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

    void OnDisable()
    {
        CancelInvoke();
    }

    public void SetMask(bool value)
    {
        isMasked = value;

        // Optional visual feedback
        if (isMasked)
            sr.color = Color.blue; // masked person
        else
            UpdateColor(); // return to infection-based color
    }

}