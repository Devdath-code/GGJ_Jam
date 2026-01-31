using UnityEngine;
using TMPro;

public class MaskUI : MonoBehaviour
{
    public int masks = 10;
    public TextMeshProUGUI maskText;

    void Start()
    {
        UpdateUI();   // ← THIS is what makes it show from the beginning
    }

    public bool UseMask()
    {
        if (masks <= 0) return false;

        masks--;
        UpdateUI();
        return true;
    }

    void UpdateUI()
    {
        maskText.text = masks.ToString(); // ← NO "x"
    }
}
    