using UnityEngine;
using System.Collections;

public class ScreenShake : MonoBehaviour
{
    public float duration = 0.15f;
    public float strength = 0.15f;

    Vector3 originalPos;

    void Awake()
    {
        originalPos = transform.localPosition;
    }

    public void Shake()
    {
        StopAllCoroutines();
        StartCoroutine(ShakeRoutine());
    }

    IEnumerator ShakeRoutine()
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            transform.localPosition =
                originalPos + (Vector3)Random.insideUnitCircle * strength;
            yield return null;
        }

        transform.localPosition = originalPos;
    }
}
