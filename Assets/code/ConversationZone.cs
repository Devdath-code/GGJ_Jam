using System.Collections.Generic;
using UnityEngine;

public class ConversationZone : MonoBehaviour
{
    [Header("Zone Settings")]
    public int maxSlots = 4;
    public float slotRadius = 0.85f;

    private Dictionary<PersonMovement, int> seatIndex = new Dictionary<PersonMovement, int>();

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
