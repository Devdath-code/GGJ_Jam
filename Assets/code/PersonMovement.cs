using UnityEngine;
using System.Collections.Generic;

public class PersonMovement : MonoBehaviour
{
    public bool isMasked = false;

    [Header("Infection")]
    public bool isInfected = false;
    [Range(0f, 1f)] public float infectionProgress = 0f;
    public float infectionTimeRequired = 3f;
    public float infectionRadius = 0.8f;

    private Dictionary<PersonMovement, float> proximityTimers = new Dictionary<PersonMovement, float>();

    [Header("Movement")]
    public float moveSpeed = 1.2f;
    public float steeringSmoothness = 8f;
    public float roamRadius = 4f;
    public float stoppingDistance = 0.25f;

    [Header("Arrive Slowdown")]
    public float arriveSlowDistance = 1.0f;

    [Header("Obstacle Avoidance")]
    public LayerMask obstacleMask;
    public float avoidCheckDistance = 1.1f;
    public float avoidStrength = 1.3f;

    [Header("AI")]
    public float decisionInterval = 2f;
    [Range(0f, 1f)] public float chanceToGoToZone = 0.3f;

    [Header("Conversation")]
    public float conversationDuration = 6f;

    [Header("Seat Larp (Snap Into Seat)")]
    public float snapTriggerDistance = 0.35f;
    public float snapToSeatTime = 0.25f;

    [Header("Separation / Push")]
    public float separationRadius = 0.55f;
    public float separationStrength = 1.0f;

    [Header("Stability")]
    public float maxSpeed = 2.0f;

    private SpriteRenderer sr;
    private Rigidbody2D rb;

    private Vector2 targetPos;
    private float decisionTimer = 0f;

    public bool isInConversation = false;
    private float conversationTimer = 0f;

    public ConversationZone currentZone = null;

    // ✅ Larp snap state
    private bool isSnappingToSeat = false;
    private float snapTimer = 0f;
    private Vector2 snapStartPos;

    private float wiggleSeed;

    void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
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

        // ✅ Roaming decision
        if (!isInConversation && currentZone == null)
        {
            if (decisionTimer >= decisionInterval)
            {
                decisionTimer = 0f;
                DecideNextAction();
            }
        }

        // ✅ Talking timer
        if (isInConversation)
        {
            conversationTimer += Time.deltaTime;
            if (conversationTimer >= conversationDuration)
                EndConversation();
        }

        // Infection spread
        if (isInfected && !isMasked)
            TryInfectOthers();

        CleanupProximityTimers();
    }

    void FixedUpdate()
    {
        // ✅ if seated & snapping -> do the larp
        if (isSnappingToSeat)
        {
            DoSeatSnap();
            ClampVelocity();
            return;
        }

        // ✅ if talking: freeze RB
        if (isInConversation && currentZone != null)
        {
            rb.linearVelocity = Vector2.zero;
            ClampVelocity();
            return;
        }

        // ✅ going to table but not seated yet: RB approach
        if (currentZone != null && !isInConversation)
        {
            MoveTowardsSeatStable();

            // separation still on while approaching
            ApplySeparation();
            ClampVelocity();
            return;
        }

        // ✅ normal roaming
        ApplySeparation();
        MoveSmoothlyToTarget();
        ClampVelocity();
    }

    void DecideNextAction()
    {
        // try going to a table
        if (Random.value < chanceToGoToZone)
        {
            ConversationZone zone = FindClosestFreeZone();
            if (zone != null && zone.TryAssignSeat(this))
            {
                currentZone = zone;
                return;
            }
        }

        // roam
        PickRoamTarget();
    }

    ConversationZone FindClosestFreeZone()
    {
        GameObject[] zones = GameObject.FindGameObjectsWithTag("ConversationZone");

        ConversationZone best = null;
        float bestDist = float.MaxValue;

        foreach (var z in zones)
        {
            ConversationZone zone = z.GetComponent<ConversationZone>();
            if (zone == null) continue;
            if (!zone.HasFreeSlot()) continue;

            float d = Vector2.Distance(transform.position, zone.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
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

    void MoveSmoothlyToTarget()
    {
        Vector2 currentPos = rb.position;
        float dist = Vector2.Distance(currentPos, targetPos);

        if (dist <= stoppingDistance)
        {
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, steeringSmoothness * Time.fixedDeltaTime);
            return;
        }

        float speedFactor = 1f;
        if (dist < arriveSlowDistance)
            speedFactor = Mathf.Clamp01(dist / arriveSlowDistance);

        Vector2 desiredDir = (targetPos - currentPos).normalized;
        Vector2 avoidDir = GetAvoidanceVector(desiredDir);

        Vector2 finalDir = (desiredDir + avoidDir * avoidStrength).normalized;
        Vector2 desiredVelocity = finalDir * moveSpeed * speedFactor;

        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, desiredVelocity, steeringSmoothness * Time.fixedDeltaTime);
    }

    // ✅ stable RB approach to seat
    void MoveTowardsSeatStable()
    {
        if (currentZone == null) return;

        Vector2 seat = currentZone.GetSeatPosition(this);
        float dist = Vector2.Distance(rb.position, seat);

        // ✅ start larp snap only when close enough
        if (dist <= snapTriggerDistance)
        {
            StartSeatSnap(seat);
            return;
        }

        Vector2 dir = (seat - rb.position).normalized;
        Vector2 desiredVel = dir * moveSpeed;

        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, desiredVel, steeringSmoothness * Time.fixedDeltaTime);
    }

    void StartSeatSnap(Vector2 seatPos)
    {
        isSnappingToSeat = true;
        snapTimer = 0f;
        snapStartPos = transform.position;
        rb.linearVelocity = Vector2.zero;

        // instantly start conversation state but we will snap first
        isInConversation = true;
        conversationTimer = 0f;
    }

    void DoSeatSnap()
    {
        if (currentZone == null)
        {
            isSnappingToSeat = false;
            isInConversation = false;
            return;
        }

        Vector2 seat = currentZone.GetSeatPosition(this);

        snapTimer += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(snapTimer / snapToSeatTime);

        transform.position = Vector2.Lerp(snapStartPos, seat, t);
        rb.linearVelocity = Vector2.zero;

        if (t >= 1f)
        {
            isSnappingToSeat = false;
            rb.linearVelocity = Vector2.zero;
        }
    }

    Vector2 GetAvoidanceVector(Vector2 desiredDir)
    {
        RaycastHit2D hit = Physics2D.Raycast(rb.position, desiredDir, avoidCheckDistance, obstacleMask);
        if (hit.collider == null) return Vector2.zero;

        Vector2 perp = Vector2.Perpendicular(desiredDir);
        float side = Mathf.PerlinNoise(wiggleSeed, Time.time * 0.2f) > 0.5f ? 1f : -1f;

        return perp * side;
    }

    void ApplySeparation()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, separationRadius);

        Vector2 push = Vector2.zero;
        int count = 0;

        foreach (var hit in hits)
        {
            if (hit.transform == transform) continue;

            PersonMovement other = hit.GetComponent<PersonMovement>();
            if (other == null) continue;

            Vector2 diff = (Vector2)(transform.position - other.transform.position);
            float dist = diff.magnitude;

            if (dist > 0.001f)
            {
                push += diff.normalized / dist;
                count++;
            }
        }

        if (count > 0)
        {
            push /= count;
            rb.AddForce(push * separationStrength, ForceMode2D.Force);
        }
    }

    void ClampVelocity()
    {
        rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, maxSpeed);
    }

    bool IsInsideBounds(Vector2 pos)
    {
        return pos.x > -5.5f && pos.x < 5.5f &&
               pos.y > -5.5f && pos.y < 5.5f;
    }

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

    public void SetMask(bool value)
    {
        isMasked = value;

        if (isMasked)
            sr.color = Color.blue;
        else
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
        List<PersonMovement> toRemove = new List<PersonMovement>();

        foreach (var kvp in proximityTimers)
        {
            if (kvp.Key == null || Vector2.Distance(transform.position, kvp.Key.transform.position) > infectionRadius)
                toRemove.Add(kvp.Key);
        }

        foreach (var p in toRemove)
            proximityTimers.Remove(p);
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

    void OnDisable()
    {
        if (currentZone != null)
        {
            currentZone.Leave(this);
            currentZone = null;
        }
    }
}
