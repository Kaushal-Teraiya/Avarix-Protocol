using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; // URP

// For HDRP: using UnityEngine.Rendering.HighDefinition;

[RequireComponent(typeof(LensFlareComponentSRP))]
public class LensFlareOcclusionFade : MonoBehaviour
{
    public float fadeSpeed = 3f; // How fast flare fades
    public LayerMask occlusionMask = ~0; // Layers to check for occlusion

    private LensFlareComponentSRP flare;
    private float currentIntensity;
    private float targetIntensity;
    public Camera mainCam;
    

    void Awake()
    {
        flare = GetComponent<LensFlareComponentSRP>();
        mainCam = GetComponent<Camera>();
        if (mainCam == null)
            mainCam = Camera.main;
        currentIntensity = 5f;
        flare.intensity = 5f;
    }

    void Update()
    {
        if (mainCam == null)
        {
            Debug.LogError("Main Camera not found!");
            return;
        }

        Vector3 dir = transform.position - mainCam.transform.position;
        float dist = dir.magnitude;

        // Raycast to check if something blocks the flare
        if (
            Physics.Raycast(
                mainCam.transform.position,
                dir.normalized,
                out RaycastHit hit,
                dist,
                occlusionMask
            )
        )
        {
            // Blocked
            targetIntensity = 0.2f;
        }
        else
        {
            // Visible
            targetIntensity = 5f;
        }

        // Smooth fade
        currentIntensity = Mathf.Lerp(
            currentIntensity,
            targetIntensity,
            Time.deltaTime * fadeSpeed
        );
        flare.intensity = currentIntensity;
    }
}
