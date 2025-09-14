using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(ConfigurableJoint))]
[RequireComponent(typeof(playerMotor))]
public class playerController : MonoBehaviour
{
    [SerializeField]
    private float lookSensitivity = 5f;

    [SerializeField]
    private float globalSpeed = 10f;

    [SerializeField]
    private float speedMultiplier;
    private playerMotor motor;
    private ConfigurableJoint joint;
    private Animator animator;

    [SerializeField]
    private float thrusterForce = 1000f;

    [SerializeField]
    private float ThrusterFuelBurnSpeed = 1f;

    [SerializeField]
    private float ThrusterFuelRegenSpeed = 0.3f;
    private float ThrusterFuelAmount = 1f;

    [SerializeField]
    private float SprintFuelBurnSpeed = 1f;

    [SerializeField]
    private float SprintFuelRegenSpeed = 0.3f;
    private float SprintFuelAmount = 1f;

    [SerializeField]
    private LayerMask EnvironmentMask;

    [Header("Spring Settings:")]
    [SerializeField]
    private JointDrive jointMode;

    [SerializeField]
    private float jointSpring = 20f;

    [SerializeField]
    private float jointMaxForce = 40f;

    void Start()
    {
        motor = GetComponent<playerMotor>();
        joint = GetComponent<ConfigurableJoint>();
        animator = GetComponent<Animator>();

        SetJointSettings(jointSpring);
    }

    void Update()
    {
        if (MatchTimer.Instance.hasMatchEnded)
            return;

        RaycastHit _hit;
        if (Physics.Raycast(transform.position, Vector3.down, out _hit, 100f, EnvironmentMask))
        {
            joint.targetPosition = new Vector3(0f, -_hit.point.y, 0f);
        }
        else
        {
            joint.targetPosition = new Vector3(0f, 0f, 0f);
        }
        float _xMov = Input.GetAxis("Horizontal");
        float _zMov = Input.GetAxis("Vertical");

        bool shiftPressed = Input.GetKey(KeyCode.LeftShift);
        bool isRunning = _zMov > 0 && shiftPressed;
        bool isJumping = Input.GetButton("Jump") && ThrusterFuelAmount > 0f;

        Vector3 _moveHorizontal = transform.right * _xMov;
        Vector3 _moveVertical = transform.forward * _zMov;
        //  Vector3 _velocity = (_moveHorizontal + _moveVertical) * speed;

        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.W))
        {
            motor.Move(Run(_moveHorizontal, _moveVertical, globalSpeed));
            SprintFuelAmount -= SprintFuelBurnSpeed * Time.deltaTime;
        }
        else
        {
            motor.Move(Walk(_moveHorizontal, _moveVertical, globalSpeed));
        }
        //animator.SetFloat("forwardVelocity", _zMov);


        float _yRot = Input.GetAxisRaw("Mouse X");
        Vector3 _rotation = new Vector3(0f, _yRot, 0f) * lookSensitivity;
        motor.Rotate(_rotation);

        float _xRot = Input.GetAxisRaw("Mouse Y");
        float _cameraRotationX = _xRot * lookSensitivity;
        motor.RotateCamera(_cameraRotationX);

        Vector3 _thrusterForce = Vector3.zero;
        if (Input.GetButton("Jump") && ThrusterFuelAmount > 0f)
        {
            ThrusterFuelAmount -= ThrusterFuelBurnSpeed * Time.deltaTime;
            if (ThrusterFuelAmount >= 0.01f)
            {
                _thrusterForce = Vector3.up * thrusterForce;
                SetJointSettings(0f);
            }
        }
        else
        {
            ThrusterFuelAmount += ThrusterFuelRegenSpeed * Time.deltaTime;
            SetJointSettings(jointSpring);
        }

        ThrusterFuelAmount = Mathf.Clamp(ThrusterFuelAmount, 0f, 1f);
        SprintFuelAmount = Mathf.Clamp(SprintFuelAmount, 0f, 1f);

        motor.ApplyThruster(_thrusterForce);
    }

    public float getThrusterFuelAmount()
    {
        return ThrusterFuelAmount;
    }

    public float getSprintFuelAmount()
    {
        return SprintFuelAmount;
    }

    private void SetJointSettings(float _jointSpring)
    {
        joint.yDrive = new JointDrive
        {
            positionSpring = jointSpring,
            maximumForce = jointMaxForce,
        };
    }

    public bool IsGrounded()
    {
        float checkDistance = 2f; // Adjust this if needed
        return Physics.Raycast(transform.position, Vector3.down, checkDistance, EnvironmentMask);
    }

    private Vector3 Run(Vector3 horizontal, Vector3 vertical, float globalSpeed)
    {
        float runSpeed = globalSpeed * speedMultiplier;
        Vector3 _velocity;
        if (SprintFuelAmount >= 0.2f)
        {
            _velocity = (horizontal + vertical) * runSpeed;
            return _velocity;
        }
        else
        {
            return Walk(horizontal, vertical, globalSpeed);
        }
    }

    private Vector3 Walk(Vector3 horizontal, Vector3 vertical, float globalSpeed)
    {
        float walkSpeed = globalSpeed - 2;
        Vector3 _velocity = (horizontal + vertical) * walkSpeed;
        SprintFuelAmount += SprintFuelRegenSpeed * Time.deltaTime;
        return _velocity;
    }
}
