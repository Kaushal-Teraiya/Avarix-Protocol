using Mirror;
using UnityEngine;

public class GunRecoil : MonoBehaviour
{
    [Header("Recoil Settings")]
    public Vector3 recoilKick = new Vector3(-0.1f, 0f, 0f); // Z-backwards kick
    public float recoilReturnSpeed = 5f;
    public float recoilSnappiness = 10f;

    [Header("Input Settings")]
    public string fireInput = "Fire1"; // Left-click or assigned input

    private Vector3 originalPosition;
    private Vector3 currentRecoil;
    private Vector3 targetRecoil;

    void Start()
    {
        originalPosition = transform.localPosition;
    }

    void Update()
    {
        // Smoothly interpolate recoil return
        targetRecoil = Vector3.Lerp(targetRecoil, Vector3.zero, recoilReturnSpeed * Time.deltaTime);
        currentRecoil = Vector3.Slerp(
            currentRecoil,
            targetRecoil,
            recoilSnappiness * Time.deltaTime
        );
        transform.localPosition = originalPosition + currentRecoil;
    }

    public void ApplyRecoil()
    {
        targetRecoil += recoilKick;
    }
}
