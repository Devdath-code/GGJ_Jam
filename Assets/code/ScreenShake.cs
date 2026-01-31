using UnityEngine;
using System.Collections;

public class ScreenShake : MonoBehaviour
{
    [Header("Shake Settings")]
    public float duration = 0.15f;
    public float magnitude = 0.25f;

    Vector3 basePosition;
    Coroutine shakeRoutine;

    void Awake()
    {
        // Cache the true camera position
        basePosition = transform.position;
    }

    public void Shake()
    {
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeCoroutine());
    }

    IEnumerator ShakeCoroutine()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            Vector2 randomOffset = Random.insideUnitCircle * magnitude;

            transform.position = basePosition + new Vector3(
                randomOffset.x,
                randomOffset.y,
                0f
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Snap back cleanly
        transform.position = basePosition;
        shakeRoutine = null;
    }
}
