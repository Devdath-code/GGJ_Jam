using UnityEngine;
using TMPro;

public class StatsUI : MonoBehaviour
{
    public SpawnManager spawnManager;
    public TextMeshProUGUI statsText;

    void Start()
    {
        if (spawnManager == null)
            spawnManager = FindFirstObjectByType<SpawnManager>();

        if (statsText == null)
            statsText = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if (spawnManager == null || statsText == null) return;

        // remove nulls so count is correct
        spawnManager.activePeople.RemoveAll(p => p == null);

        int total = spawnManager.activePeople.Count;
        int infected = 0;

        foreach (var p in spawnManager.activePeople)
        {
            if (p != null && p.isInfected)
                infected++;
        }

        int healthy = total - infected;
        float ratio = (total > 0) ? (infected / (float)total) * 100f : 0f;

        statsText.text =
            $"TOTAL: {total}\n" +
            $"INFECTED: {infected}\n" +
            $"HEALTHY: {healthy}\n" +
            $"INFECTED %: {ratio:0.0}%";
    }
}
