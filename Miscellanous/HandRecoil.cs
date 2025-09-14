using UnityEngine;

public class HandRecoilFollower : MonoBehaviour
{
    public Transform gunTransform; // assign your gun here
    public Vector3 positionOffset;
    public Vector3 rotationOffset;

    void LateUpdate()
    {
        if (gunTransform != null)
        {
            transform.position = gunTransform.position + gunTransform.TransformVector(positionOffset);
            transform.rotation = gunTransform.rotation * Quaternion.Euler(rotationOffset);
        }
    }
}
