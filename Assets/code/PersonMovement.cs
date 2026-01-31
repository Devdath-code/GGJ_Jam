using UnityEngine;
using System.Collections.Generic;

public class PersonMovement : MonoBehaviour
{
    [Header("Infection")]
    public bool isInfected = false;
    [Range(0f, 1f)] public float infectionProgress = 0f;
    public float infectionTimeRequired = 3f;
    public float infectionRadius = 0.8f;

    private Dictionary<PersonMovement, float> proximityTimers = new Dictionary<PersonMovement, float>();

    [Header("Movement (Smooth AI)")]
    public float moveSpeed = 1.2f;
    public float steeringSmoothness = 8f;
    public float roamRadius = 4f;
    public float stoppingDistance = 0.25f;

    [Header("Obstacle Avoidance")]
    public LayerMask obstacleMask;
    public float avoidCheckDistance = 1.1f;
    public float avoidStrength = 1.3f;

    [Header("AI Decisions")]
    public float decisionInterval = 2f;
    [Range(0f, 1f)] public float chanceToGoToZone = 0.35f;

    [Header("Conversation")]
    public float conversationDuration = 6f;

    [Header("Seat Idle Movement (Natural)")]
    public float seatWiggleRadius = 0.12f;     // tiny motion
    public float seatWiggleSpeed = 1.8f;

    private SpriteRenderer sr;
    private Rigidbody2D rb;

    private Vector2 targetPos;
    private float decisionTimer = 0f;

    public bool isInConversation = false;
    private float conversationTimer = 0f;

    private ConversationZone currentZone = null;

    // Wiggle
    private float wiggleSeed;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        wiggleSeed = Random.Range(0f, 100f);
    }

    void Start()
    {
        PickRoamTarget();
        UpdateColor();
    }

    void Update()
    {
        decisionTimer += Time.deltaTime;

        if (!isInConversation)
        {
            if (decisionTimer >= decisionInterval)
            {
                decisionTimer = 0f;
                DecideNextAction();
            }
        }
        else
        {
            HandleConversation();
        }

        if (isInfected)
        {
            TryInfectOthers();
        }

        CleanupProximityTimers();
    }

    void FixedUpdate()
    {
        if (isInConversation)
        {
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, steeringSmoothness * Time.fixedDeltaTime);
            return;
        }

        MoveSmoothlyToTarget();
    }

    // ---------------------------
    // AI Decision Logic
    // ---------------------------
    void DecideNextAction()
    {
        // if already going to zone -> update seat position occasionally
        if (currentZone != null)
        {
            targetPos = currentZone.GetSeatPosition(this);
            return;
        }

        // choose closest zone with free slot
        if (Random.value < chanceToGoToZone)
        {
            ConversationZone zone = FindClosestFreeZone();
            if (zone != null && zone.TryAssignSeat(this))
            {
                currentZone = zone;
                targetPos = currentZone.GetSeatPosition(this);
                return;
            }
        }

        PickRoamTarget();
    }

    ConversationZone FindClosestFreeZone()
    {
        GameObject[] zones = GameObject.FindGameObjectsWithTag("ConversationZone");

        ConversationZone best = null;
        float bestDist = float.MaxValue;

        foreach (GameObject z in zones)
        {
            ConversationZone zone = z.GetComponent<ConversationZone>();
            if (zone == null) continue;
            if (!zone.HasFreeSlot()) continue;

            float dist = Vector2.Distance(transform.position, zone.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = zone;
            }
        }

        return best;
    }

    void PickRoamTarget()
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
    }

    // ---------------------------
    // Movement + Obstacle Avoidance
    // ---------------------------
    void MoveSmoothlyToTarget()
    {
        Vector2 currentPos = rb.position;
        float dist = Vector2.Distance(currentPos, targetPos);

        // reached target
        if (dist <= stoppingDistance)
        {
            if (currentZone != null)
            {
                isInConversation = true;
                conversationTimer = 0f;
                rb.linearVelocity = Vector2.zero;
            }
            else
            {
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, steeringSmoothness * Time.fixedDeltaTime);
            }
            return;
        }

        Vector2 desiredDir = (targetPos - currentPos).normalized;

        // ✅ obstacle avoidance
        Vector2 avoidDir = GetAvoidanceVector(desiredDir);

        Vector2 finalDir = (desiredDir + avoidDir * avoidStrength).normalized;
        Vector2 desiredVelocity = finalDir * moveSpeed;

        rb.linearVelocity = Vector2.Lerp(
            rb.linearVelocity,
            desiredVelocity,
            steeringSmoothness * Time.fixedDeltaTime
        );
    }

    Vector2 GetAvoidanceVector(Vector2 desiredDir)
    {
        // cast forward to detect obstacles (tables, walls)
        RaycastHit2D hit = Physics2D.Raycast(rb.position, desiredDir, avoidCheckDistance, obstacleMask);

        if (hit.collider == null)
            return Vector2.zero;

        // move perpendicular around obstacle
        Vector2 perp = Vector2.Perpendicular(desiredDir);

        // choose side randomly but stable
        float side = Mathf.PerlinNoise(wiggleSeed, Time.time * 0.2f) > 0.5f ? 1f : -1f;

        return perp * side;
    }

    // ---------------------------
    // Conversation Behavior
    // ---------------------------
    void HandleConversation()
    {
        conversationTimer += Time.deltaTime;

        if (currentZone != null)
        {
            Vector2 seat = currentZone.GetSeatPosition(this);

            // ✅ tiny natural movement around the seat (not running)
            Vector2 wiggle = new Vector2(
                Mathf.Sin(Time.time * seatWiggleSpeed + wiggleSeed),
                Mathf.Cos(Time.time * seatWiggleSpeed + wiggleSeed)
            ) * seatWiggleRadius;

            Vector2 desired = seat + wiggle;

            transform.position = Vector2.Lerp(
                transform.position,
                desired,
                Time.deltaTime * 3f
            );
        }

        if (conversationTimer >= conversationDuration)
        {
            EndConversation();
        }
    }

    void EndConversation()
    {
        isInConversation = false;
        conversationTimer = 0f;

        if (currentZone != null)
        {
            currentZone.Leave(this);
            currentZone = null;
        }

        PickRoamTarget();
    }

    // ---------------------------
    // Infection
    // ---------------------------
    void UpdateColor()
    {
        if (sr == null) return;
        sr.color = Color.Lerp(Color.white, Color.red, infectionProgress);
    }

    public void SetInfected()
    {
        isInfected = true;
        infectionProgress = 1f;
        UpdateColor();
    }

    void TryInfectOthers()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, infectionRadius);

        foreach (Collider2D hit in hits)
        {
            if (hit.transform == transform) continue;

            PersonMovement other = hit.GetComponent<PersonMovement>();
            if (other == null || other.isInfected) continue;

            if (!proximityTimers.ContainsKey(other))
                proximityTimers[other] = 0f;

            proximityTimers[other] += Time.deltaTime;

            float requiredTime = isInConversation ? 0.5f : 2f;
            float rateMultiplier = isInConversation ? 1.5f : 0.4f;

            if (proximityTimers[other] >= requiredTime)
            {
                other.infectionProgress += (Time.deltaTime / infectionTimeRequired) * rateMultiplier;
                other.infectionProgress = Mathf.Clamp01(other.infectionProgress);
                other.UpdateColor();

                if (other.infectionProgress >= 1f)
                    other.SetInfected();
            }
        }
    }

    void CleanupProximityTimers()
    {
        List<PersonMovement> remove = new List<PersonMovement>();

        foreach (var kvp in proximityTimers)
        {
            if (kvp.Key == null ||
                Vector2.Distance(transform.position, kvp.Key.transform.position) > infectionRadius)
            {
                remove.Add(kvp.Key);
            }
        }

        foreach (var p in remove)
            proximityTimers.Remove(p);
    }

    // ---------------------------
    // Helpers
    // ---------------------------
    bool IsInsideBounds(Vector2 pos)
    {
        return pos.x > -5.5f && pos.x < 5.5f &&
               pos.y > -5.5f && pos.y < 5.5f;
    }

    void OnDisable()
    {
        if (currentZone != null)
        {
            currentZone.Leave(this);
            currentZone = null;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, infectionRadius);
    }
}
