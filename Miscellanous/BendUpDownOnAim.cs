using UnityEngine;

public class BendUpDownOnAim : MonoBehaviour
{
    public Transform aimTarget; // Reference to the empty GameObject controlling spine rotation
    public Transform cameraTransform; // Reference to the player camera
    public float aimOffset = 2.0f; // Adjust based on how much the spine should bend
    public Transform HeadTarget;

    void Update()
    {
        if (aimTarget != null && cameraTransform != null && HeadTarget != null)
        {
            Vector3 targetPosition = cameraTransform.position + cameraTransform.forward * aimOffset;
            aimTarget.position = Vector3.Lerp(
                aimTarget.position,
                targetPosition,
                Time.deltaTime * 10f
            );
            HeadTarget.position = Vector3.Lerp(
                HeadTarget.position,
                targetPosition,
                Time.deltaTime * 10f
            );
        }
    }
}
