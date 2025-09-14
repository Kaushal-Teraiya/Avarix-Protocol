using UnityEngine;
using Mirror;
using System.Collections;
using UnityEngine.Rendering.PostProcessing;

public class ForwardDash : NetworkBehaviour
{
    [SerializeField] private float dashDistance = 5f; 
    [SerializeField] private float dashDuration = 0.2f; 
    [SerializeField] private float fovIncrease = 10f; 
   // [SerializeField] private float fovLerpSpeed = 10f; 
    [SerializeField] private float fovResetDelay = 0.2f; 
    [SerializeField] private Material radialLinesMaterial;
    //private float fadeSpeed = 5f;
    [SerializeField] private float dashCooldown = 10f;
    private float lastDashTime;

    [SyncVar(hook = nameof(OnIsDashingChanged))] private bool isDashing = false;
    private CharacterController controller;
    private playerController playerMovement;
    private float startFOV;

    private PostProcessVolume postProcessVolume;
    private MotionBlur motionBlur;
    public AudioSource dashSound;

    private void OnIsDashingChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"[{netId}] OnIsDashingChanged: {oldValue} -> {newValue}");
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerMovement = GetComponent<playerController>();
        startFOV = Camera.main.fieldOfView;
        postProcessVolume = Camera.main.GetComponent<PostProcessVolume>();
        radialLinesMaterial.SetFloat("_Alpha", 0f);

        if (postProcessVolume != null)
            postProcessVolume.profile.TryGetSettings(out motionBlur);
    }

    private void EnableMotionBlur(bool enable)
    {
        if (motionBlur != null)
            motionBlur.active = enable;
    }

    void Update()
    {
        if (!isLocalPlayer) return; 

        if (Input.GetKeyDown(KeyCode.J))
        {
            Debug.Log($"[{netId}] Dash button pressed. isDashing={isDashing}, cooldown={Time.time - lastDashTime}");
        }

        if (Input.GetKeyDown(KeyCode.J) && !isDashing && Time.time > lastDashTime + dashCooldown)
        {
            Debug.Log($"[{netId}] Dash initiated!");
            CmdPerformDash();
            StartCoroutine(LerpCameraFOV(startFOV + fovIncrease, 0.1f));
            lastDashTime = Time.time;
        }
    }

    [Command]
    private void CmdPerformDash()
    {
        if (isDashing)
        {
            Debug.LogWarning($"[{netId}] CmdPerformDash aborted: already dashing.");
            return;
        }

        isDashing = true;
        Debug.Log($"[{netId}] CmdPerformDash executing dash.");
        RpcApplyDash(transform.position + transform.forward * dashDistance);

        StartCoroutine(ResetDashStatus());
    }

    [ClientRpc]
    private void RpcApplyDash(Vector3 dashTarget)
    {
        if (!isOwned)
        {
            Debug.LogWarning($"[{netId}] RpcApplyDash ignored (not local player).");
            return;
        }

        Debug.Log($"[{netId}] RpcApplyDash received on client. Starting dash.");
        StartCoroutine(SmoothDash(dashTarget));
    }

    private IEnumerator SmoothDash(Vector3 targetPosition)
    {
        Debug.Log($"[{netId}] SmoothDash started. Moving to {targetPosition}");

        EnableMotionBlur(true);
        dashSound?.Play();
        StartCoroutine(FadeRadialLines(1f, 0.1f));

        Vector3 startPosition = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < dashDuration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / dashDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;
        Debug.Log($"[{netId}] Dash completed. Resetting effects.");

        yield return new WaitForSeconds(fovResetDelay);
        EnableMotionBlur(false);
        StartCoroutine(FadeRadialLines(0f, 0.3f));
        StartCoroutine(LerpCameraFOV(startFOV, 0.3f));

        Debug.Log($"[{netId}] Dash sequence fully completed.");
    }

    private IEnumerator ResetDashStatus()
    {
        yield return new WaitForSeconds(dashDuration + 0.1f);
        Debug.Log($"[{netId}] ResetDashStatus: isDashing now false.");
        isDashing = false;
    }

    private IEnumerator LerpCameraFOV(float targetFOV, float duration)
    {
        float elapsedTime = 0f;
        float startFOV = Camera.main.fieldOfView;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            t = t * t * (3f - 2f * t);
            Camera.main.fieldOfView = Mathf.Lerp(startFOV, targetFOV, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Camera.main.fieldOfView = targetFOV;
    }

    private IEnumerator FadeRadialLines(float targetAlpha, float duration)
    {
        float startAlpha = radialLinesMaterial.GetFloat("_Alpha");
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration);
            radialLinesMaterial.SetFloat("_Alpha", alpha);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        radialLinesMaterial.SetFloat("_Alpha", targetAlpha);
    }
}
