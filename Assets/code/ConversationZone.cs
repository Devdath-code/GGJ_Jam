using System.Collections.Generic;
using UnityEngine;

public class ConversationZone : MonoBehaviour
{
    [Header("Zone Settings")]
    public int maxSlots = 4;
    public float slotRadius = 0.85f;

    [Header("Full Zone Behavior")]
    public float fullKickDelay = 20f; // ✅ editable in inspector (not hardcoded)
    public Color fullColor = Color.black;
    public Color normalColor = Color.white;

    private SpriteRenderer sr;

    private Dictionary<PersonMovement, int> seatIndex = new Dictionary<PersonMovement, int>();

    private float fullTimer = 0f;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            normalColor = sr.color; // store original color automatically
        }
    }

    void Update()
    {
        CleanupNulls();

        bool isFull = seatIndex.Count >= maxSlots;

        // ✅ change table color
        if (sr != null)
        {
            sr.color = isFull ? fullColor : normalColor;
        }

        // ✅ if table stays full too long, kick one person out
        if (isFull)
        {
            fullTimer += Time.deltaTime;

            if (fullTimer >= fullKickDelay)
            {
                KickOnePerson();
                fullTimer = 0f;
            }
        }
        else
        {
            fullTimer = 0f;
        }
    }

    public bool HasFreeSlot()
    {
        CleanupNulls();
        return seatIndex.Count < maxSlots;
    }

    public bool TryAssignSeat(PersonMovement person)
    {
        CleanupNulls();
        if (person == null) return false;

        if (seatIndex.ContainsKey(person)) return true;
        if (seatIndex.Count >= maxSlots) return false;

        for (int i = 0; i < maxSlots; i++)
        {
            if (!seatIndex.ContainsValue(i))
            {
                seatIndex[person] = i;
                return true;
            }
        }

        return false;
    }

    public Vector2 GetSeatPosition(PersonMovement person)
    {
        CleanupNulls();
        if (person == null) return transform.position;

        if (!seatIndex.ContainsKey(person))
            return transform.position;

        int index = seatIndex[person];
        float angle = (360f / maxSlots) * index;

        Vector2 offset = new Vector2(
            Mathf.Cos(angle * Mathf.Deg2Rad),
            Mathf.Sin(angle * Mathf.Deg2Rad)
        ) * slotRadius;

        return (Vector2)transform.position + offset;
    }

    public void Leave(PersonMovement person)
    {
        CleanupNulls();
        if (person == null) return;

        if (seatIndex.ContainsKey(person))
            seatIndex.Remove(person);
    }

    // ✅ kick 1 random person if zone full too long
    void KickOnePerson()
    {
        if (seatIndex.Count == 0) return;

        // convert dictionary keys to list
        List<PersonMovement> people = new List<PersonMovement>(seatIndex.Keys);
        people.RemoveAll(p => p == null);

        if (people.Count == 0) return;

        PersonMovement kicked = people[Random.Range(0, people.Count)];

        // remove seat
        Leave(kicked);

        // force them to stop being in conversation and roam
        if (kicked != null)
        {
            kicked.ForceLeaveConversation();
        }
    }

    void CleanupNulls()
    {
        List<PersonMovement> remove = new List<PersonMovement>();

        foreach (var kvp in seatIndex)
        {
            if (kvp.Key == null)
                remove.Add(kvp.Key);
        }

        foreach (var p in remove)
            seatIndex.Remove(p);
    }
}
