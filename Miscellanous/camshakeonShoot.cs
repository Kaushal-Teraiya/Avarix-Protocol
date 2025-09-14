using UnityEngine;

public class CameraShakeonShoot : MonoBehaviour
{
    public static CameraShakeonShoot Instance;

    private Vector3 originalPos;
    private float shakeDuration = 0f;
    private float shakeMagnitude = 0.1f;
    private float dampingSpeed = 1.0f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this); // Destroy this script only, not the camera
        }
    }

    void OnEnable()
    {
        originalPos = transform.localPosition;
    }

    void Update()
    {
        if (shakeDuration > 0)
        {
            transform.localPosition = originalPos + Random.insideUnitSphere * shakeMagnitude;

            shakeDuration -= Time.deltaTime * dampingSpeed;
        }
        else
        {
            shakeDuration = 0f;
            transform.localPosition = originalPos;
        }
    }

    public void Shake(float duration = 0.2f, float magnitude = 0.1f)
    {
        shakeDuration = duration;
        shakeMagnitude = magnitude;
    }
}
