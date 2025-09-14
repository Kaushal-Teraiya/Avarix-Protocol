using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class NetworkedGroundSlam : NetworkBehaviour
{
    public float slamRadius = 5f;
    public float throwForce = 10f;
    public LayerMask playerLayer;
    public GameObject slamEffectPrefab;
    public GameObject ShockWaveVFX;

    public Animator animator;
    public RigBuilder rigBuilder;
    public Rig handRig; // Player's hand rig for holding weapons

    private void Update()
    {
        if (!isLocalPlayer)
            return;

        if (Input.GetKeyDown(KeyCode.J))
        {
            StartCoroutine(PerformSlamWithDelay());
        }
    }

    IEnumerator PerformSlamWithDelay()
    {
        // Disable hand rig so the animation plays correctly
        handRig.weight = 0f;
        animator.SetTrigger("GroundSlam");

        for (int i = 0; i < 3; i++)
        {
            CmdPerformSlam();
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(4f); // Wait for animation to finish
        handRig.weight = 1f; // Re-enable the rig after animation
    }

    [Command]
    void CmdPerformSlam()
    {
        RpcSpawnSlamEffect(transform.position);

        Collider[] hitPlayers = Physics.OverlapSphere(transform.position, slamRadius);
        Debug.Log($"Ground Slam triggered! Players found: {hitPlayers.Length}");

        foreach (Collider player in hitPlayers)
        {
            if (!player.CompareTag("Player"))
                continue; // ✅ Only affect players
            if (player.gameObject == gameObject)
                continue; // ✅ Ignore the slam performer

            if (player.TryGetComponent(out NetworkIdentity identity))
            {
                TargetApplyKnockback(identity.connectionToClient, player.gameObject);
            }
        }
    }

    [ClientRpc]
    void RpcSpawnSlamEffect(Vector3 position)
    {
        if (slamEffectPrefab != null)
        {
            GameObject effect = Instantiate(slamEffectPrefab, position, Quaternion.Euler(90, 0, 0));
            GameObject shockWave = Instantiate(ShockWaveVFX, position, Quaternion.identity);
            Destroy(effect, 3f);
            Destroy(shockWave, 3f);
        }

        if (CameraShake.instance != null)
        {
            CameraShake.instance.ShakeOnPower();
        }
    }

    [TargetRpc]
    void TargetApplyKnockback(NetworkConnection target, GameObject playerObject)
    {
        if (!playerObject.CompareTag("Player"))
            return; // ✅ Ensure it's a player before proceeding

        Debug.Log($"Applying knockback to: {playerObject.name}");

        if (playerObject.TryGetComponent(out Rigidbody rb))
        {
            Vector3 direction = (playerObject.transform.position - transform.position).normalized;
            if (direction == Vector3.zero)
                direction = playerObject.transform.forward;

            rb.AddForce(direction * throwForce, ForceMode.Impulse);
        }
    }
}
