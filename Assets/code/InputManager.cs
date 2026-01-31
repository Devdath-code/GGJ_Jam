using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public Camera mainCamera;
    public MaskUI maskUI;
    public ScreenShake screenShake;

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryMaskPerson();
        }
    }

    void TryMaskPerson()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 worldPos = mainCamera.ScreenToWorldPoint(mousePos);

        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
        if (!hit) return;

        PersonMovement person = hit.collider.GetComponent<PersonMovement>();
        if (person == null) return;
        if (person.isMasked) return;
        if (!maskUI.UseMask()) return;

        person.SetMask(true);
        screenShake.Shake();
    }
}
