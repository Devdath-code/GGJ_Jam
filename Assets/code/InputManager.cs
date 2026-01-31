using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [Header("Mask Settings")]
    public int maskCount = 10;
    public LayerMask personLayer;

    [Header("References")]
    public Camera mainCamera;
    public ScreenShake screenShake;

    void Update()
    {
        if (Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryApplyMask();
        }
    }

    void TryApplyMask()
    {
        if (maskCount <= 0)
            return;

        Vector2 mousePos = mainCamera.ScreenToWorldPoint(
            Mouse.current.position.ReadValue()
        );

        RaycastHit2D hit = Physics2D.Raycast(
            mousePos,
            Vector2.zero,
            0f,
            personLayer
        );

        if (hit.collider == null)
            return;

        PersonMovement person = hit.collider.GetComponent<PersonMovement>();

        if (person != null && !person.isMasked)
        {
            person.SetMask(true);
            maskCount--;

            if (screenShake != null)
                screenShake.Shake();
        }
    }
}
