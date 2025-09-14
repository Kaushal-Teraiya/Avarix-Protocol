using Mirror;
using UnityEngine;
using UnityEngine.AI;

public class AIAnimationController : NetworkBehaviour
{
    private Animator animator;
    private NavMeshAgent agent;

    [SyncVar(hook = nameof(OnMoveSpeedChanged))]
    private float syncedMoveSpeed;

    [SyncVar(hook = nameof(OnStrafeSpeedChanged))]
    private float syncedStrafeSpeed;

    [SyncVar(hook = nameof(OnJumpStateChanged))]
    private bool syncedIsJumping;

    void Start()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        if (isServer)
        {
            syncedMoveSpeed = 0f;
            syncedStrafeSpeed = 0f;
            syncedIsJumping = false;
        }
    }

    void Update()
    {
        if (!isServer)
            return; // AI logic runs only on the server

        Vector3 localVelocity = transform.InverseTransformDirection(agent.desiredVelocity);

        float moveSpeed = 0f;
        if (localVelocity.z > 0.1f)
            moveSpeed = Mathf.Clamp(localVelocity.z / agent.speed, 0.6f, 1f);
        else if (localVelocity.z < -0.1f)
            moveSpeed = Mathf.Clamp(localVelocity.z / agent.speed, -1f, -0.6f);

        float strafeSpeed = 0f;
        if (localVelocity.x > 0.1f)
            strafeSpeed = Mathf.Clamp(localVelocity.x / agent.speed, 0.6f, 1f);
        else if (localVelocity.x < -0.1f)
            strafeSpeed = Mathf.Clamp(localVelocity.x / agent.speed, -1f, -0.6f);

        bool isJumping = false; // AI doesn't jump for now

        // Instead of Cmd, call RpcUpdateAnimation to update animation state on all clients
        RpcUpdateAnimation(moveSpeed, strafeSpeed, isJumping);
    }

    [ClientRpc]
    void RpcUpdateAnimation(float moveSpeed, float strafeSpeed, bool isJumping)
    {
        // Update animation on all clients
        animator.SetFloat("speed", moveSpeed);
        animator.SetFloat("Strafe", strafeSpeed);
        animator.SetBool("IsJumping", isJumping);
    }

    void OnMoveSpeedChanged(float oldValue, float newValue)
    {
        animator.SetFloat("speed", newValue);
    }

    void OnStrafeSpeedChanged(float oldValue, float newValue)
    {
        animator.SetFloat("Strafe", newValue);
    }

    void OnJumpStateChanged(bool oldValue, bool newValue)
    {
        animator.SetBool("IsJumping", newValue);
    }
}
