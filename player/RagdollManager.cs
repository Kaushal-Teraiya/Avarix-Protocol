using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class RagdollManager : NetworkBehaviour
{
    // ────────────────────────────────────────────────
    // Fields & References
    // ────────────────────────────────────────────────
    private Rigidbody[] ragdollRigidBodies;
    private Collider[] ragdollColliders;
    private Dictionary<Transform, (Vector3, Quaternion)> originalPose = new();

    private Animator animator;
    private playerShoot PS;
    private Collider mainCollider;
    private Rigidbody mainRigidbody;

    [SerializeField]
    public float RagdollForce;

    [SerializeField]
    private Rigidbody[] upperBodyParts;

    [SerializeField]
    private Rigidbody[] lowerBodyParts;

    [SerializeField]
    private Rigidbody hipsRb; // Assign in inspector

    public Rigidbody hipsRigidbody;
    private Rigidbody[] allRigidbodies;

    // ────────────────────────────────────────────────
    // Unity Methods
    // ────────────────────────────────────────────────
    private void Awake()
    {
        ragdollRigidBodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();
        PS = GetComponent<playerShoot>();
        animator = GetComponent<Animator>();
        mainCollider = GetComponent<Collider>();
        mainRigidbody = GetComponent<Rigidbody>();

        StoreOriginalPose(); // Save initial bone positions
        DisableRagdoll(); // Ensure ragdoll is disabled at start
    }

    private void Start() { }

    // ────────────────────────────────────────────────
    // Public API
    // ────────────────────────────────────────────────
    public Rigidbody GetHipsRigidbody()
    {
        return hipsRigidbody; // Assign this in Unity Inspector or find it in Awake()
    }

    public void ResetRagdollPose()
    {
        foreach (var bone in originalPose)
        {
            bone.Key.localPosition = bone.Value.Item1;
            bone.Key.localRotation = bone.Value.Item2;
        }

        DisableRagdoll(); // Reset physics properly
    }

    public void ApplyExplosionToHips(float force)
    {
        hipsRigidbody.AddForce(Vector3.up * RagdollForce, ForceMode.Impulse);
        hipsRigidbody.isKinematic = false;

        Debug.Log("Applying force to: " + hipsRigidbody.name);
        Debug.Log("IsKinematic: " + hipsRigidbody.isKinematic);
        Debug.Log("Mass: " + hipsRigidbody.mass);
        Debug.Log($"Applying force: {Vector3.up * RagdollForce} to {hipsRigidbody.name}");
    }

    public Rigidbody GetRagdollPartRigidbody(Collider hitCollider)
    {
        Transform current = hitCollider.transform;

        // Traverse up the hierarchy to find the nearest Rigidbody
        while (current != null)
        {
            Rigidbody rb = current.GetComponent<Rigidbody>();
            if (rb != null)
                return rb;

            current = current.parent;
        }

        return null; // Return null if no Rigidbody is found
    }

    public Rigidbody GetRigidbodyByName(string boneName)
    {
        foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
        {
            if (rb.name == boneName)
                return rb;
        }
        return null;
    }

    // Call this once to cache all Rigidbodies in children
    public void InitializeRagdollParts()
    {
        allRigidbodies = GetComponentsInChildren<Rigidbody>(true); // true includes inactive ones
    }

    // Call this to get all Rigidbodies in the ragdoll
    public Rigidbody[] GetAllRigidbodies()
    {
        if (allRigidbodies == null || allRigidbodies.Length == 0)
        {
            InitializeRagdollParts();
        }
        return allRigidbodies;
    }

    public void ApplyForceToRagdoll(Vector3 hitPoint, Vector3 hitNormal)
    {
        float hitY = hitPoint.y;
        float hipsY = hipsRb.transform.position.y;

        Rigidbody targetRb =
            (hitY > hipsY)
                ? upperBodyParts[Random.Range(0, upperBodyParts.Length)]
                : lowerBodyParts[Random.Range(0, lowerBodyParts.Length)];

        targetRb.AddForceAtPosition(-hitNormal * RagdollForce, hitPoint, ForceMode.Impulse);

        Debug.Log(
            $"[RAGDOLL FORCE] HitY: {hitY} vs HipsY: {hipsY} | Target: {targetRb.name} | Force at: {hitPoint}"
        );
    }

    // ────────────────────────────────────────────────
    // Private Helpers
    // ────────────────────────────────────────────────
    private void DisableRagdoll()
    {
        foreach (var rb in ragdollRigidBodies)
        {
            rb.isKinematic = true;
        }

        foreach (var collider in ragdollColliders)
        {
            collider.enabled = false;
        }

        if (mainCollider)
            mainCollider.enabled = true;
        if (mainRigidbody)
            mainRigidbody.isKinematic = false;
        if (animator)
            animator.enabled = true;
    }

    private void StoreOriginalPose()
    {
        foreach (Transform bone in GetComponentsInChildren<Transform>())
        {
            originalPose[bone] = (bone.localPosition, bone.localRotation);
        }
    }

    // ────────────────────────────────────────────────
    // Networking
    // ────────────────────────────────────────────────
    [Command(requiresAuthority = false)]
    public void CmdDisableRagdoll()
    {
        RpcDisableRagdoll();
    }

    [ClientRpc]
    private void RpcDisableRagdoll()
    {
        DisableRagdoll();
    }

    [Command(requiresAuthority = false)]
    public void CmdEnableRagdoll()
    {
        RpcEnableRagdoll();
    }

    [ClientRpc]
    private void RpcEnableRagdoll()
    {
        if (mainCollider)
            mainCollider.enabled = false; // Disable player collider

        foreach (var rb in ragdollRigidBodies)
        {
            rb.isKinematic = false;
        }

        foreach (var collider in ragdollColliders)
        {
            collider.enabled = true;
        }

        Debug.Log("Ragdoll enabled!");
    }
}
