using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake instance;

    private void Awake()
    {
        instance = this;
    }

    public void StartShake(float duration, float magnitude, float rawShakeRange = 0.5f)
    {
        StartCoroutine(Shake(duration, magnitude, rawShakeRange));
    }

    private IEnumerator Shake(float duration, float magnitude, float rawShakeRange)
    {
        Vector3 originalPos = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-rawShakeRange, rawShakeRange) * magnitude;
            float y = Random.Range(-rawShakeRange, rawShakeRange) * magnitude;

            transform.localPosition = originalPos + new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPos;
    }

    // 🔫 Gun-specific shake: subtle and tight
    public void ShakeOnShoot()
    {
        StartShake(0.06f, 0.07f, 0.5f); // Calmer range
    }

    // 💥 Superpower-specific shake: wild and powerful
    public void ShakeOnPower()
    {
        StartShake(0.3f, 1.0f, 2.0f); // -2 to 2 range times 1.0 magnitude
    }
}
