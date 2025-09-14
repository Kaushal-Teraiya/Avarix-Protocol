using UnityEngine;
using Mirror;

public class AnimationController : NetworkBehaviour
{
    private Animator animator;
    private playerController PC;
    private float strafeSpeed;
    public float moveSpeedRef;
    private NetworkIdentity networkIdentity;

    [SyncVar(hook = nameof(OnMoveSpeedChanged))] 
    private float syncedMoveSpeed;

    [SyncVar(hook = nameof(OnStrafeSpeedChanged))] 
    private float syncedStrafeSpeed;

    [SyncVar(hook = nameof(OnJumpStateChanged))] 
    private bool syncedIsJumping;

    void Start()
    {
        animator = GetComponent<Animator>();
        PC = GetComponent<playerController>();
        networkIdentity = GetComponent<NetworkIdentity>();

        if (isServer) 
        {
            syncedMoveSpeed = 0f;
            syncedStrafeSpeed = 0f;
            syncedIsJumping = false;
        }
    }

    [ClientCallback]
    void Update()
    {
        if (!isOwned) { return; }

        float FuelAmount = PC.getThrusterFuelAmount();
        float SprintAmount = PC.getSprintFuelAmount();

        float verticalInput = Input.GetAxis("Vertical");
        float horizontalInput = Input.GetAxis("Horizontal");
        bool isRunning = Input.GetKey(KeyCode.LeftShift) && (Mathf.Abs(verticalInput) > 0.1f || Mathf.Abs(horizontalInput) > 0.1f) && SprintAmount > 0.1f;
        bool isJumping = Input.GetKeyDown(KeyCode.Space) && FuelAmount > 0.1f;

        float moveSpeed = 0f;
        if (verticalInput > 0.1f) moveSpeed = isRunning ? 1f : 0.6f;
        else if (verticalInput < -0.1f) moveSpeed = isRunning ? -1f : -0.6f;

        float strafeSpeed = 0f;
        if (horizontalInput > 0.1f) strafeSpeed = 1f;
        else if (horizontalInput < -0.1f) strafeSpeed = -1f;

        if (PC.IsGrounded()) isJumping = false;

        CmdUpdateAnimation(moveSpeed, strafeSpeed, isJumping);
    }

    [Command]
    void CmdUpdateAnimation(float moveSpeed, float strafeSpeed, bool isJumping)
    {
        syncedMoveSpeed = moveSpeed;
        syncedStrafeSpeed = strafeSpeed;
        syncedIsJumping = isJumping;
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
