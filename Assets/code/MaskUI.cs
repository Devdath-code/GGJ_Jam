using UnityEngine;
using TMPro;

public class MaskUI : MonoBehaviour
{
    [Header("References")]
    public InputManager inputManager;
    public TextMeshProUGUI maskCountText;

    void Update()
    {
        if (inputManager == null || maskCountText == null)
            return;

        maskCountText.text = inputManager.maskCount.ToString();
    }
}
