using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.AI;

public class AIController : NetworkBehaviour
{
    private enum AIState
    {
        Idle, // Default state when AI has no immediate goal
        SeekFlag, // Moving to capture the enemy flag
        ReturnFlag, // Bringing back the enemy flag to base
        AttackEnemy, // Fighting nearby enemies
        RunToCover, // Sprinting to cover under heavy fire
        InCover, // Staying in cover and waiting
        RetrieveFlag, // Returning our own stolen/dropped flag
        ChaseFlagCarrier, // Chasing the enemy who stole the flag
        DefendBase, // Staying at base to defend
        Patrol, //patrol around base
        SearchHealth, // Looking for health pickups when low HP
        MoveToHealth, // Actively moving to a health pickup
    }

    private enum RoleType
    {
        Combat,
        Defender,
        FlagCarrier,
    }

    [SerializeField]
    private RoleType role; // Set this in the Inspector

    private AIRoles.AICombatState combatState;
    private AIRoles.AIDefenderState defenderState;
    private AIRoles.AIFlagCarrierState flagCarrierState;

    #region  Variables

    private AIState currentState = AIState.Idle;
    private string team;
    public LayerMask obstacleLayerMask;
    private Coroutine AIloop;

    // private LayerMask coverLayerMask;
    private RaycastHit hitInfo;

    #region  scriptReferences

    private FlagHandler flagHandler;
    private Animator animator;
    private NavMeshAgent agent;
    private player playerHealth;
    private playerShoot playerShootScript;
    private playerWeapon currentWeapon;
    private NetworkIdentity attackerId;

    #endregion scriptReferences

    #region  GameObjects

    private GameObject enemyFlag;
    private GameObject homeBase;
    public GameObject basePosition;
    List<GameObject> detectedEnemies = new List<GameObject>();
    public GameObject redFlagBasePosition;
    public GameObject blueFlagBasePosition;

    #endregion GameObjects

    #region Transforms

    private Transform targetEnemy;
    private Transform coverTarget;
    private AIState previousState;

    [SerializeField]
    private Transform[] patrolPoints;

    [SerializeField]
    private Transform FlagCarrier;

    #endregion Transforms

    #region  TimeVariables

    public float attackRange = 5f;
    public float strafeSpeed = 2f; // Reduced speed for smoother movement
    public float strafeDuration = 2f; // Ensure AI strafes in one direction for a while
    private float patrolStartTime = 0f; // Track patrol start time
    private float maxPatrolTime = 8f; // Max time before switching state
    private float stateUpdateCooldown = 2f; // Prevents rapid state switching
    private float lastStateUpdateTime = 0f; // Tracks when AI last updated state
    private float lastHitTime;
    private float lastDefenseTime;
    public float timeInterval = 0.5f;

    #endregion TimeVariables

    #region SpeedAndIndex

    public float runSpeed = 7;
    private int currentPatrolIndex = 0;

    public int walkSpeed = 4;

    #endregion SpeedAndIndex

    #region Booleans

    private bool isUnderAttack = false;
    private bool isStrafing = false;
    private float nextFireTime;

    #endregion Booleans


    #endregion Variables


    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        flagHandler = GetComponent<FlagHandler>();
        playerShootScript = GetComponent<playerShoot>();
        animator = GetComponent<Animator>();
        playerHealth = GetComponent<player>();

        team = flagHandler.Team; // Get bot's team

        // Find enemy flag and home base

        AssignPatrolPoints();
        AssignFlagLocationsAndProperties();
        currentState = AIState.Idle;

        if (!isServer)
            return;

        InvokeRepeating(nameof(BotThinkLoop), 0.2f, 0.5f);

        // Debug log for team and flag target
        Debug.Log(
            $"<color={(team == "Red" ? "red" : "blue")}>[BOT TEAM: {team}]</color> Targeting flag: <color=yellow>{enemyFlag.tag}</color>"
        );
    }

    private void BotThinkLoop()
    {
        FindTargetEnemy();
        CheckFlagCarrierStatus();

        if (currentState == AIState.AttackEnemy && targetEnemy != null)
        {
            AimAtTarget(targetEnemy.position);
        }

        if (currentState == AIState.DefendBase || currentState == AIState.Idle)
        {
            CheckFlagStatus();
        }

        // FSM with debug logs
        switch (currentState)
        {
            case AIState.Idle:
                Debug.Log(
                    $"<color={(team == "Red" ? "red" : "blue")}>[{team} BOT]</color> Idle, waiting for action..."
                );
                break;

            case AIState.SeekFlag:
                Debug.Log(
                    $"<color={(team == "Red" ? "red" : "blue")}>[{team} BOT]</color> Seeking <color=yellow>{enemyFlag.tag}</color>"
                );
                SeekFlag();
                break;

            case AIState.ReturnFlag:
                Debug.Log(
                    $"<color={(team == "Red" ? "red" : "blue")}>[{team} BOT]</color> Returning to <color=green>{homeBase.tag}</color>"
                );
                ReturnFlag();
                break;

            case AIState.AttackEnemy:
                Debug.Log(
                    $"<color={(team == "Red" ? "red" : "blue")}>[{team} BOT]</color> Attacking enemy <color=cyan>{targetEnemy?.name}</color>"
                );
                AttackEnemy();
                break;

            case AIState.RunToCover:
                Debug.Log(
                    $"<color={(team == "Red" ? "red" : "blue")}>[{team} BOT]</color> Running to cover!"
                );
                RunToCover();
                break;

            case AIState.InCover:
                Debug.Log(
                    $"<color={(team == "Red" ? "red" : "blue")}>[{team} BOT]</color> Taking cover!"
                );
                StayInCover();
                break;

            case AIState.ChaseFlagCarrier:
                Debug.Log(
                    $"<color={(team == "Red" ? "red" : "blue")}>[{team} BOT]</color> Chasing enemy flag carrier!"
                );
                CmdChaseFlagCarrier();
                break;

            case AIState.RetrieveFlag:
                Debug.Log(
                    $"<color={(team == "Red" ? "red" : "blue")}>[{team} BOT]</color> Retrieving our dropped flag!"
                );
                CmdRetrieveFlag();
                break;

            case AIState.DefendBase:
                Debug.Log(
                    $"<color={(team == "Red" ? "red" : "blue")}>[{team} BOT]</color> Defending our base!"
                );
                DefendBase();
                break;

            case AIState.Patrol:
                Debug.Log(
                    $"<color={(team == "Red" ? "red" : "blue")}>[{team} BOT]</color> Patrolling our base!"
                );
                Patrol();
                break;

            case AIState.MoveToHealth:
                Debug.Log(
                    $"<color={(team == "Red" ? "red" : "blue")}>[{team} BOT]</color> Moving towards the health pickup!"
                );
                EvaluateSurvivalState();
                break;

            case AIState.SearchHealth:
                Debug.Log(
                    $"<color={(team == "Red" ? "red" : "blue")}>[{team} BOT]</color> Searching for health pickups!"
                );
                FindHealthPickup(20f);
                break;
        }
    }

    private void Update()
    {
        UpdateAIState();
    }

    // private void Update()
    // {
    //     if (!isServer)
    //         return;

    //     FindTargetEnemy();
    //     CheckFlagCarrierStatus();
    //     UpdateAIState();

    //     if (currentState == AIState.AttackEnemy && targetEnemy != null)
    //     {
    //         AimAtTarget(targetEnemy.position); // Keep adjusting aim every frame
    //     }
    //     if (currentState == AIState.DefendBase || currentState == AIState.Idle)
    //     {
    //         CheckFlagStatus(); // Re-evaluate flag conditions
    //     }

    //     #region  Debugging

    //     if (Input.GetKeyDown(KeyCode.C)) // Press 'C' to test cover selection
    //     {
    //         Transform cover = FindBestCover();
    //         if (cover != null)
    //         {
    //             Debug.Log($"[{gameObject.name}] Moving to Cover: {cover.name}");
    //         }
    //     }

    //     if (FindBestCover() != null)
    //     {
    //         Debug.DrawLine(transform.position, FindBestCover().position, Color.green, 3f);
    //     }
    //     #endregion Debugging

    //     #region FiniteStateMachine
    //     switch (currentState)
    //     {
    //         case AIState.Idle:
    //             Debug.Log(
    //                 $"<color={(team == "Red" ? "red" : "blue")}>[{team} BOT]</color> Idle, waiting for action..."
    //             );
    //             break;

    //         case AIState.SeekFlag:
    //             Debug.Log(
    //                 $"<color={(team == "Red" ? "red" : "blue")}>[{team} BOT]</color> Seeking <color=yellow>{enemyFlag.tag}</color>"
    //             );
    //             SeekFlag();
    //             break;

    //         case AIState.ReturnFlag:
    //             Debug.Log(
    //                 $"<color={(team == "Red" ? "red" : "blue")}>[{team} BOT]</color> Returning to <color=green>{homeBase.tag}</color>"
    //             );
    //             ReturnFlag();
    //             break;

    //         case AIState.AttackEnemy:
    //             Debug.Log(
    //                 $"<color={(team == "Red" ? "red" : "blue")}>[{team} BOT]</color> Attacking enemy <color=cyan>{targetEnemy?.name}</color>"
    //             );
    //             AttackEnemy();
    //             break;
    //         case AIState.RunToCover:
    //             Debug.Log(
    //                 $"<color={(team == "Red" ? "red" : "blue")}>[{team} BOT]</color> Running to cover!"
    //             );
    //             RunToCover();
    //             break;
    //         case AIState.InCover:
    //             Debug.Log(
    //                 $"<color={(team == "Red" ? "red" : "blue")}>[{team} BOT]</color> Taking cover!"
    //             );
    //             StayInCover();
    //             break;

    //         case AIState.ChaseFlagCarrier:
    //             Debug.Log(
    //                 $"<color={(team == "Red" ? "red" : "blue")}>[{team} BOT]</color> Chasing enemy flag carrier!"
    //             );
    //             CmdChaseFlagCarrier();
    //             break;

    //         case AIState.RetrieveFlag:
    //             Debug.Log(
    //                 $"<color={(team == "Red" ? "red" : "blue")}>[{team} BOT]</color> Retrieving our dropped flag!"
    //             );
    //             CmdRetrieveFlag();
    //             break;

    //         case AIState.DefendBase:
    //             Debug.Log(
    //                 $"<color={(team == "Red" ? "red" : "blue")}>[{team} BOT]</color> Defending our base!"
    //             );
    //             DefendBase();
    //             break;
    //         case AIState.Patrol:
    //             Debug.Log(
    //                 $"<color={(team == "Red" ? "red" : "blue")}>[{team} BOT]</color> patrolling our base!"
    //             );
    //             Patrol();
    //             break;
    //         case AIState.MoveToHealth:
    //             Debug.Log(
    //                 $"<color={(team == "Red" ? "red" : "blue")}>[{team} BOT]</color> Moving towards the health pickup!"
    //             );
    //             EvaluateSurvivalState();
    //             break;
    //         case AIState.SearchHealth:
    //             Debug.Log(
    //                 $"<color={(team == "Red" ? "red" : "blue")}>[{team} BOT]</color> Searching for health pickups!"
    //             );
    //             FindHealthPickup(20f);
    //             break;
    //     }
    //     #endregion FiniteStateMachine
    // }

    #region  FlagConditions
    private void OnTriggerEnter(Collider other)
    {
        if (flagHandler.heldFlag == null)
        {
            if (
                other.CompareTag("BlueFlag")
                && flagHandler.Team == "Red"
                && GameManager.instance.canPickUpBlueFlag
                && !playerHealth.isDead
            )
            {
                CmdPickFlag(other.gameObject);
            }
            else if (
                other.CompareTag("RedFlag")
                && flagHandler.Team == "Blue"
                && GameManager.instance.canPickUpRedFlag
                && !playerHealth.isDead
            )
            {
                CmdPickFlag(other.gameObject);
            }
        }

        if (
            other.CompareTag("BlueFlag")
            && flagHandler.Team == "Blue"
            && GameManager.instance.isStolenBlue
            && GameManager.instance.canPickUpBlueFlag
        )
        {
            flagHandler.CmdReturnFlag(other.gameObject, "Blue");
        }
        else if (
            other.CompareTag("RedFlag")
            && flagHandler.Team == "Red"
            && GameManager.instance.isStolenRed
            && GameManager.instance.canPickUpRedFlag
        )
        {
            flagHandler.CmdReturnFlag(other.gameObject, "Red");
        }
        if (flagHandler.heldFlag != null)
        {
            if (
                other.CompareTag("BL")
                && flagHandler.Team == "Blue"
                && flagHandler.heldFlag.CompareTag("RedFlag")
                && !GameManager.instance.isStolenBlue
            )
            {
                CmdCaptureFlag("Blue");
            }
            else if (
                other.CompareTag("RL")
                && flagHandler.Team == "Red"
                && flagHandler.heldFlag.CompareTag("BlueFlag")
                && !GameManager.instance.isStolenRed
            )
            {
                CmdCaptureFlag("Red");
            }
        }
    }
    #endregion FlagConditions

    #region CombatBrain


    private Transform FindTargetEnemy()
    {
        detectedEnemies.Clear(); // Reset list

        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange);
        Transform closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player") && hit.transform != transform) // Ignore itself
            {
                string enemyTeam = GetPlayerTeam(hit.gameObject); // Get player's team
                if (enemyTeam == team)
                    continue; // Ignore teammates (human or AI)
                detectedEnemies.Add(hit.transform.gameObject);

                // Check if this enemy is the closest one
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = hit.transform;
                }
            }
        }

        targetEnemy = closestEnemy; // Set closest enemy as the main target

        if (targetEnemy != null)
        {
            currentState = AIState.AttackEnemy;
        }
        else
        {
            // No enemy found, reset state
            if (currentState == AIState.AttackEnemy)
            {
                if (flagHandler.heldFlag != null)
                {
                    Debug.Log("No enemy found, but I have the flag! Returning to base.");
                    currentState = AIState.ReturnFlag;
                }
                else
                {
                    Debug.Log("No enemy found. Seeking flag.");
                    currentState = AIState.SeekFlag;
                }
            }
        }

        return targetEnemy;
    }

    private void AttackEnemy()
    {
        if (targetEnemy == null)
        {
            //targetEnemy = FindTargetEnemy(); // Try finding a new target

            StopShooting();
            StopStrafing();
            agent.isStopped = false;

            // ✅ If bot has the flag, return to base instead of seeking flag
            if (flagHandler.heldFlag != null)
            {
                Debug.Log("No enemy, but I have the flag! Returning to base.");
                currentState = AIState.ReturnFlag;
            }
            else
            {
                Debug.Log("No enemy. Seeking flag.");
                currentState = AIState.SeekFlag;
            }
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, targetEnemy.position);

        // If enemy is slightly out of range, move forward instead of giving up
        if (distanceToTarget > attackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(targetEnemy.position); // Chase enemy
            StopStrafing();
            return;
        }

        // Keep aiming at the enemy
        AimAtTarget(targetEnemy.position);

        // Stop moving while shooting
        agent.isStopped = true;

        // Get weapon info
        currentWeapon = playerShootScript.WeaponManager.GetcurrentWeapon();
        if (currentWeapon == null)
            return;

        // Shooting logic
        if (currentWeapon.FireRate <= 0f) // Sniper (Single Shot)
        {
            if (Time.time >= nextFireTime)
            {
                playerShootScript.Shoot();
                nextFireTime = Time.time + 2f;
            }
        }
        else // Automatic weapon
        {
            if (!IsInvoking(nameof(ShootAI)))
            {
                InvokeRepeating(nameof(ShootAI), 0f, 1 / currentWeapon.FireRate);
            }
        }

        // Restart strafing if needed
        if (
            !isStrafing
            && targetEnemy != null
            && Vector3.Distance(transform.position, targetEnemy.position) <= attackRange
        )
        {
            StopStrafing();
            StartCoroutine(StrafeSideways()); // Start a fresh strafing cycle
        }
    }

    // Stop strafing when enemy is lost
    private void StopStrafing()
    {
        isStrafing = false;
        StopAllCoroutines(); // Ensures all running coroutines are stopped
    }

    // Smoothly rotate towards the enemy
    private void AimAtTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0; // Prevent tilting up/down

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(direction),
            Time.deltaTime * 8f
        );
    }

    [Server] // Ensure this only runs on the server
    void ShootAI()
    {
        NetworkIdentity attacker = null;
        if (currentWeapon == null)
            return;

        Debug.Log($"🤖 AI {gameObject.name} is shooting!");

        Vector3 shootOrigin = transform.position + transform.forward * 1.5f; // Adjust based on weapon position
        Vector3 shootDirection =
            transform.forward
            + new Vector3(
                UnityEngine.Random.Range(
                    -playerShootScript.ActualBulletSpread.x,
                    playerShootScript.ActualBulletSpread.x
                ),
                UnityEngine.Random.Range(
                    -playerShootScript.ActualBulletSpread.y,
                    playerShootScript.ActualBulletSpread.y
                ),
                UnityEngine.Random.Range(
                    -playerShootScript.ActualBulletSpread.z,
                    playerShootScript.ActualBulletSpread.z
                )
            );

        int layerMask = ~LayerMask.GetMask("Ignore Raycast");
        RaycastHit[] hits = Physics.RaycastAll(
            shootOrigin,
            shootDirection,
            currentWeapon.range,
            layerMask
        );
        hits = hits.OrderBy(h => h.distance).ToArray();

        foreach (RaycastHit hit in hits)
        {
            Debug.Log($"🤖 AI Hit: {hit.collider.gameObject.name}");
            hitInfo = hit;
            // Damage Player
            player hitPlayer = hit.collider.GetComponentInParent<player>();
            if (hitPlayer != null && hitPlayer != this)
            {
                // player hitPlayer = hit.collider.GetComponentInParent<player>();
                if (
                    hitPlayer != null
                    && hitPlayer != this
                    && GetPlayerTeam(hitPlayer.gameObject) != team
                    && (hit.collider.CompareTag("Player") || hit.collider.CompareTag("Ragdoll"))
                )
                {
                    CmdPlayerShot(
                        hit.collider.name,
                        currentWeapon.damage,
                        GetComponent<NetworkIdentity>()
                    );
                }
            }

            // Damage Turret
            if (hit.collider.CompareTag("Turret"))
            {
                Turret turret = hit.collider.GetComponent<Turret>();
                if (turret != null)
                    turret.CmdTakeDamage(1f);
            }

            if (hit.collider.CompareTag("Shield"))
            {
                Debug.Log("🔵 Bullet hit an enemy shield! Triggering ripple effect...");

                SpawnShieldRipples ripples = hit.collider.GetComponent<SpawnShieldRipples>();

                if (ripples != null)
                {
                    //Debug.Log("🔵 Skipping ripple effect test...");
                    ripples.TriggerRippleEffect(hit.point, hit.normal);
                }
                else
                {
                    Debug.LogError("❌ SpawnShieldRipples script is NULL!");
                }

                //CmdOnHit(hit.point, hit.normal);
                Debug.LogError("Bullets stopped");
                return;
            }

            // Reflect Bullet Logic (if needed)
            if (hit.collider.CompareTag("ReflectBulletShield"))
            {
                ReflectBulletShield reflectBullet =
                    hit.collider.GetComponent<ReflectBulletShield>();
                if (reflectBullet != null)
                {
                    reflectBullet.CmdReflectBullet(hit.point, shootDirection, gameObject, attacker);
                    return;
                }
            }

            // Show impact effects
            // playerShootScript.CmdOnHit(hit.point, hit.normal);
        }
    }

    [Server]
    public void CmdPlayerShot(string _playerID, int _damage, NetworkIdentity attackerIdentity)
    {
        if (!isServer)
        {
            Debug.LogError("CmdTakeDamage called on client! This should be on server.");
            return;
        }

        Debug.Log(_playerID + " has been shot");

        player _player = GameManager.GetPlayer(_playerID);
        if (_player == null)
        {
            Debug.LogError("Player not found! Maybe they already died?");
            return;
        }

        // 🔥 Access the shield component
        DreyarShield shield = _player.GetComponent<DreyarShield>();
        SpawnShieldRipples ripples = _player.GetComponent<SpawnShieldRipples>();
        ReflectBulletShield reflectBullet = _player.GetComponent<ReflectBulletShield>();

        if (reflectBullet != null)
        {
            Debug.Log("Bullet Reflected");
            //reflectBullet.ReflectBullet(hitInfo.point, this.gameObject);
            return;
        }

        if (ripples != null)
        {
            //Debug.Log("🔵 Skipping ripple effect test...");
            ripples.TriggerRippleEffect(hitInfo.point, hitInfo.normal);
        }

        if (shield != null && shield.IsShieldActive)
        {
            Debug.Log("Shot blocked! " + _playerID + " has an active shield.");

            return; // Stop the function if shield is active
        }

        // Apply damage
        _player.RpctakeDamageP(
            _damage,
            attackerIdentity,
            playerHealth.storedHitPoint,
            playerHealth.storedHitDirection,
            playerHealth.storedHitBodyPartName
        );

        // Update health on UI
        _player.RpcUpdateHealthUI(_player.currentHealth);
        _player.UpdateHealth(_player.currentHealth);
        PlayerHealthBar _playerHealthBar = _player.GetComponent<PlayerHealthBar>();
        if (_playerHealthBar != null)
        {
            _playerHealthBar.RpcUpdateHealthBar(_player.currentHealth);
        }
        else
        {
            Debug.LogError("PlayerHealthBar not found on target player!");
        }
    }

    private IEnumerator StrafeSideways()
    {
        isStrafing = true;

        while (true) // Infinite loop to keep switching directions
        {
            float strafeDirection = UnityEngine.Random.Range(0, 2) == 0 ? -1f : 1f;
            float strafeTime = UnityEngine.Random.Range(0.5f, 1f); // Adjusted time for smooth animation

            animator.SetFloat("Strafe", strafeDirection); // Set animation direction

            float timer = 0f;
            while (timer < strafeTime)
            {
                // **Check if target is still in range**
                if (
                    targetEnemy == null
                    || Vector3.Distance(transform.position, targetEnemy.position) > attackRange + 1f
                )
                {
                    Debug.Log("Target lost! Switching state.");
                    StopShooting();
                    agent.isStopped = false; // Resume movement
                    // ✅ **Check if bot has flag and return instead of seeking**
                    if (flagHandler.heldFlag != null)
                    {
                        Debug.Log("I have the flag! Returning to base.");
                        currentState = AIState.ReturnFlag;
                    }
                    else
                    {
                        Debug.Log("Enemy gone. Seeking flag.");
                        currentState = AIState.SeekFlag;
                    }
                    StopStrafing();
                    yield break; // Exit coroutine completely
                }

                transform.position +=
                    transform.right * strafeDirection * (strafeSpeed * Time.deltaTime);
                timer += Time.deltaTime;

                // **Keep updating the animation while strafing**
                animator.SetFloat("Strafe", strafeDirection);

                yield return null;
            }

            // **Immediately switch direction without resetting animation**
        }
    }

    private void StopShooting()
    {
        CancelInvoke(nameof(ShootAI));
        agent.isStopped = false;
    }

    #endregion CombatBrain

    #region CTFBrain
    private void SeekFlag()
    {
        if (enemyFlag == null)
            return;

        if (IsEnemyFlagHeld()) // If someone has already picked up the flag, chase them
        {
            Debug.Log("🚨 Enemy flag is held! Switching to ChaseFlagCarrier.");
            currentState = AIState.ChaseFlagCarrier;
            return;
        }

        agent.SetDestination(enemyFlag.transform.position);
        agent.speed = 6;
    }

    private void ReturnFlag()
    {
        if (homeBase == null)
            return;
        if (flagHandler.heldFlag == null)
        {
            Debug.Log("❌ Lost the flag! Re-evaluating action.");
            currentState = GetRandomState();
            return;
        }
        agent.SetDestination(homeBase.transform.position);
    }

    // Multiplayer Commands
    [Command(requiresAuthority = false)]
    private void CmdPickFlag(GameObject flag)
    {
        Debug.Log(
            $"<color={(team == "Red" ? "red" : "blue")}>[{team} BOT]</color> Picked up <color=yellow>{flag.tag}</color>"
        );
        flagHandler.CmdPickFlag(flag);
        currentState = AIState.ReturnFlag;
    }

    [Command(requiresAuthority = false)]
    private void CmdCaptureFlag(string teamColor)
    {
        if (flagHandler.heldFlag == null) // 🛑 Prevent capturing if not carrying a flag
        {
            Debug.Log("❌ ERROR: Tried to capture but no flag is held!");
            currentState = AIState.DefendBase;
        }

        currentState = GetRandomState();
        Debug.Log("Random State: " + currentState);
        Debug.Log(
            $"<color={(team == "Red" ? "red" : "blue")}>[{team} BOT]</color> Captured the flag!"
        );
        flagHandler.CmdCaptureFlag(teamColor);
    }

    private void CheckFlagStatus()
    {
        Transform flagCarrier = FindFlagCarrier();
        if (flagCarrier != null)
        {
            Debug.Log($"⚔️ Enemy stole our flag! Chasing them: {flagCarrier.name}");
            targetEnemy = flagCarrier;
            currentState = AIState.ChaseFlagCarrier;
            return;
        }

        // Check if our flag is missing (stolen or dropped)
        if (IsFlagMissing())
        {
            Debug.Log("🚨 Our flag is missing! Retrieving it.");
            currentState = AIState.RetrieveFlag;
            return;
        }

        // If everything is fine, just defend
        Debug.Log("🏠 Flag is safe. Defending base.");
        currentState = AIState.DefendBase;
    }

    // Chase the enemy who stole our flag
    [Command(requiresAuthority = false)]
    private void CmdChaseFlagCarrier()
    {
        Transform flagCarrier = FlagCarrier;

        if (flagCarrier != null)
        {
            Debug.Log("⚔️ AI chasing flag carrier: " + FlagCarrier.name);
            targetEnemy = FlagCarrier;
            agent.SetDestination(FlagCarrier.position);
            return;
        }
        else
        {
            // If the flag was dropped after the carrier died, retrieve it
            if (IsFlagMissing())
            {
                Debug.Log("🚨 Flag carrier died, retrieving dropped flag!");
                currentState = AIState.RetrieveFlag;
                return;
            }
            else
            {
                Debug.Log("❌ No enemy flag carrier found! Choosing next action...");
                agent.ResetPath();
                currentState = GetRandomState(); // Pick a random state instead of always defending
                Debug.Log("Random State: " + currentState);
            }
        }
    }

    // AI returns to touch its dropped flag
    [Command(requiresAuthority = false)]
    private void CmdRetrieveFlag()
    {
        string flagTag = (team == "Red") ? "RedFlag" : "BlueFlag";
        GameObject flag = GameObject.FindGameObjectWithTag(flagTag);

        if (flag == null)
        {
            Debug.Log("❌ ERROR: Flag not found! Retrying search...");
            currentState = AIState.SeekFlag; // New state to retry finding the flag
            return;
        }

        if (IsFlagAtBase(flag))
        {
            Debug.Log("✅ Flag is already at base! Switching to new state.");
            return;
        }
        Debug.Log("🔄 Moving to retrieve our flag at " + flag.transform.position);
        agent.SetDestination(flag.transform.position);
        currentState = AIState.RetrieveFlag;
    }

    #endregion CTFBrain

    #region SurvivalBrain

    private void EvaluateSurvivalState()
    {
        if (playerHealth == null)
            return;
        int enemyCount = detectedEnemies.Count;
        bool isUnderAttack = IsGettingShotAt(); // Function that checks if bullets are incoming
        bool enemyInRange = enemyCount > 0 && IsEnemyInAttackRange(); // Function to check enemy attack range

        Debug.Log("<color=#FFD700>🧠 [SurvivalBrain] Evaluating Survival State...</color>");

        // Prioritize health pickups if HP is below 60%
        if (playerHealth.currentHealth <= 60)
        {
            Transform nearestHealth = FindHealthPickup(20f);
            if (nearestHealth != null)
            {
                Debug.Log("<color=#32CD32>💊 Low HP! Going for health pickup.</color>");
                agent.SetDestination(nearestHealth.position);
                currentState = AIState.MoveToHealth;
                return;
            }
        }

        // If AI is moving to health but gets attacked, run to cover instead
        if (currentState == AIState.MoveToHealth && isUnderAttack && !enemyInRange)
        {
            Transform bestCover = FindBestCover();
            if (bestCover != null)
            {
                Debug.Log(
                    "<color=#FF4500>⚠️ Under fire while moving to health! Running to cover.</color>"
                );
                coverTarget = bestCover;
                currentState = AIState.RunToCover;
                return;
            }
        }

        // If AI is getting shot but no enemy is in range, take cover
        if (isUnderAttack && !enemyInRange)
        {
            Transform bestCover = FindBestCover();

            if (bestCover != null)
            {
                Debug.Log(
                    "<color=#32CD32>🏃 Running to cover at: " + bestCover.position + "</color>"
                );
                coverTarget = bestCover;
                currentState = AIState.RunToCover;

                // Set AI to move to cover immediately
                agent.isStopped = false;
                agent.SetDestination(bestCover.position);

                if (agent.pathPending)
                {
                    Debug.Log("<color=#FFD700>🛤️ Path is pending...</color>");
                }
            }
            else
            {
                Debug.LogError("<color=#FF0000>❌ Could not find a valid cover location!</color>");
            }
        }

        // If an enemy is in range, fight!
        if (enemyInRange)
        {
            Debug.Log("<color=#FF4500>🎯 Enemy detected! Engaging in combat.</color>");
            currentState = AIState.AttackEnemy;
            return;
        }

        // Default to patrol if no threats
        Debug.Log("<color=#1E90FF>🚶 No threats detected. Patrolling.</color>");
        currentState = AIState.Patrol;
    }

    private void RunToCover()
    {
        if (coverTarget == null)
        {
            Debug.LogError("<color=#FF0000>❌ No valid cover point found!</color>");
            return;
        }

        Debug.Log($"<color=#32CD32>🏃 Running to cover point at: {coverTarget.position}</color>");

        agent.isStopped = false;
        agent.speed = runSpeed;
        agent.SetDestination(coverTarget.position);

        StartCoroutine(CheckIfReachedCover());
    }

    IEnumerator StayInCover()
    {
        Debug.Log("<color=#FFD700>🛡️ AI reached cover. Staying in cover...</color>");

        float elapsedTime = 0f;
        float maxCoverTime = 5f; // AI won't stay in cover forever

        while (elapsedTime < maxCoverTime)
        {
            yield return new WaitForSeconds(1f);
            elapsedTime += 1f;

            bool stillUnderAttack = IsGettingShotAt();
            bool enemyStillInRange = detectedEnemies.Count > 0 && IsEnemyInAttackRange();

            // Stay in cover if still in danger
            if (stillUnderAttack || enemyStillInRange)
            {
                Debug.Log("<color=#FF4500>🔄 Still under attack! Staying in cover.</color>");
                continue;
            }

            // Check for nearby health pickups
            Transform nearestHealth = FindHealthPickup(20f);
            if (nearestHealth != null)
            {
                Debug.Log(
                    "<color=#32CD32>💊 Found new health while in cover! Moving to health.</color>"
                );
                agent.SetDestination(nearestHealth.position);
                currentState = AIState.MoveToHealth;
                yield break; // Stop coroutine and leave cover
            }
            else
            {
                Debug.Log("No Health pickup found");
                yield return new WaitForSeconds(5f);
                currentState = GetRandomState();
            }
        }

        // If no threats and no health found, decide next action
        Debug.Log("<color=#FFD700>🛡️ Cover time expired. Deciding next move...</color>");
        if (playerHealth.currentHealth > 60)
        {
            Debug.Log("<color=#1E90FF>🔄 No health needed. Resuming patrol.</color>");
            currentState = GetRandomState();
        }
        else if (detectedEnemies.Count > 0)
        {
            Debug.Log(
                "<color=#FF4500>🔥 No health found, but enemy is nearby. Engaging in combat.</color>"
            );
            currentState = AIState.AttackEnemy;
        }
    }

    #endregion SurvivalBrain

    #region StrategicBrain
    private void Patrol()
    {
        if (currentState == AIState.Patrol && patrolStartTime == 0f)
        {
            patrolStartTime = Time.time;
        }
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Debug.LogError($"⚠️ No patrol points set for {team} team!");
            return;
        }

        if (!agent.pathPending && agent.remainingDistance < 1f) // Reached last patrol point
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            Debug.Log(
                $"🚶‍♂️ {team} AI patrolling: Moving to {patrolPoints[currentPatrolIndex].position}"
            );
        }

        // Transition after patrolling for too long
        if (Time.time > patrolStartTime + maxPatrolTime)
        {
            Debug.Log("🔄 Patrolled too long! Switching to a new state...");
            currentState = GetRandomState();
            patrolStartTime = 0;
        }
    }

    private Transform FindDroppedFlag()
    {
        string flagTag = team == "Red" ? "RedFlag" : "BlueFlag";
        GameObject droppedFlag = GameObject.FindGameObjectWithTag(flagTag);

        if (droppedFlag != null)
        {
            Debug.Log($"🚩 My team's flag is on the ground! Going to retrieve it.");
            return droppedFlag.transform;
        }

        return null;
    }

    // Stay at base to defend
    private void DefendBase()
    {
        agent.SetDestination(basePosition.transform.position);
        Debug.Log("🏠 AI is defending the base.");

        // Start a coroutine to transition to patrol after 5 seconds
        StopAllCoroutines(); // Prevent multiple coroutines from stacking up
        StartCoroutine(TransitionToPatrol());
    }

    private Transform FindBestCover()
    {
        float searchRadius = 100f; // Adjust if needed
        Collider[] coverPoints = Physics.OverlapSphere(transform.position, searchRadius);

        Debug.Log(
            $"<color=#FFD700>🔍 Found {coverPoints.Length} objects in radius {searchRadius}</color>"
        );

        List<Transform> validCoverPoints = new List<Transform>();

        foreach (Collider c in coverPoints)
        {
            if (c.CompareTag("CoverPoint"))
            {
                Debug.Log(
                    $"<color=#32CD32>🛡️ Found valid CoverPoint at: {c.transform.position}</color>"
                );
                validCoverPoints.Add(c.transform);
            }
            else
            {
                Debug.Log($"<color=#FF4500>❌ Skipping {c.gameObject.name}, tag: {c.tag}</color>");
            }
        }

        if (validCoverPoints.Count == 0)
        {
            Debug.LogError(
                "<color=#FF0000>❌ No valid CoverPoints found! Check tags and radius.</color>"
            );
            return null;
        }

        // Find the closest cover point
        Transform bestCoverPoint = validCoverPoints
            .OrderBy(p => Vector3.Distance(transform.position, p.position))
            .FirstOrDefault();

        Debug.Log($"<color=#00FF7F>✅ Best CoverPoint chosen at: {bestCoverPoint.position}</color>");
        return bestCoverPoint;
    }

    private Transform FindHealthPickup(float range)
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, range);
        Transform nearestHealth = null;
        float minDistance = Mathf.Infinity;

        foreach (Collider col in colliders)
        {
            if (col.CompareTag("HealthDrop"))
            {
                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearestHealth = col.transform;
                }
            }
        }
        return nearestHealth;
    }

    #endregion StrategicBrain

    #region LogicalBrain
    private bool IsFlagMissing()
    {
        GameObject flag =
            (team == "Red")
                ? GameObject.FindGameObjectWithTag("RedFlag")
                : GameObject.FindGameObjectWithTag("BlueFlag");

        // If the flag is at the base, it is not missing
        if (IsFlagAtBase(flag))
        {
            return false;
        }

        // Otherwise, check if the flag is stolen or dropped
        return (
                team == "Red"
                && (GameManager.instance.isStolenRed || GameManager.instance.isDroppedRed)
            )
            || (
                team == "Blue"
                && (GameManager.instance.isStolenBlue || GameManager.instance.isDroppedBlue)
            );
    }

    private void CheckFlagCarrierStatus()
    {
        if (FlagCarrier != null)
        {
            player playerScript = FlagCarrier.GetComponent<player>();
            if (playerScript != null && playerScript.isDead)
            {
                Debug.Log($"🚨 Flag Carrier {FlagCarrier.name} is dead! Resetting flagCarrier.");
                FlagCarrier = null;
                FlagCarrier = FindFlagCarrier();
            }
        }
    }

    private bool IsEnemyFlagHeld()
    {
        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject playerObj in allPlayers)
        {
            FlagHandler flagHandler = playerObj.GetComponent<FlagHandler>();
            if (flagHandler != null && flagHandler.heldFlag != null)
            {
                // If the player is carrying the enemy's flag
                if (flagHandler.heldFlag.CompareTag(team == "Red" ? "BlueFlag" : "RedFlag"))
                {
                    return true; // Someone has the enemy flag
                }
            }
        }

        return false; // Flag is still on the ground
    }

    bool HasClearPath(Transform cover)
    {
        Vector3 direction = cover.position - transform.position;
        float distance = direction.magnitude;

        if (Physics.Raycast(transform.position, direction.normalized, distance, obstacleLayerMask))
        {
            return false;
        }
        return true;
    }

    bool IsValidCover(Transform cover)
    {
        foreach (GameObject enemy in detectedEnemies)
        {
            Vector3 enemyPos = enemy.transform.position;
            Vector3 direction = cover.position - enemyPos;

            if (Physics.Raycast(enemyPos, direction, out RaycastHit hit, direction.magnitude))
            {
                if (hit.transform == cover)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool IsEnemyInAttackRange()
    {
        if (targetEnemy != null)
        {
            return true;
        }
        return false;
    }

    private bool IsFlagAtBase(GameObject flag)
    {
        if (team == "Red")
        {
            return Vector3.Distance(flag.transform.position, redFlagBasePosition.transform.position)
                < 1f;
        }
        else
        {
            return Vector3.Distance(
                    flag.transform.position,
                    blueFlagBasePosition.transform.position
                ) < 1f;
        }
    }

    private bool IsGettingShotAt()
    {
        if (!isUnderAttack)
            return false;

        if (Time.time - lastHitTime > 2f) // If last hit was more than 2 seconds ago
        {
            isUnderAttack = false;
        }

        return isUnderAttack;
    }

    private void UpdateAIState()
    {
        // Prevent updating state too frequently
        if (Time.time < lastStateUpdateTime + stateUpdateCooldown)
            return;

        lastStateUpdateTime = Time.time; // Update timestamp

        CheckFlagCarrierStatus(); // Ensure AI updates flag carrier status

        // ✅ AI should take cover if HP < 60% while under attack, BUT NOT if attacking
        if (
            currentState != AIState.AttackEnemy
            && playerHealth.currentHealth <= 60
            && IsGettingShotAt()
        )
        {
            Transform bestCover = FindBestCover();
            if (bestCover != null)
            {
                Debug.Log("<color=#FFA500>⚠️ AI is under fire with low HP! Taking cover.</color>");
                coverTarget = bestCover;
                previousState = currentState; // Store the previous state to resume later
                currentState = AIState.RunToCover;
                return;
            }
        }

        if (
            currentState == AIState.AttackEnemy
            ||
            // currentState == AIState.RetrieveFlag ||
            // currentState == AIState.ReturnFlag ||
            currentState == AIState.ChaseFlagCarrier
            || currentState == AIState.DefendBase
            || currentState == AIState.SeekFlag
            || currentState == AIState.RunToCover
            || currentState == AIState.InCover
        )
        {
            // 🚨 If AI is in an active state, don't override it!
            return;
        }

        // If AI finds an enemy, switch to ATTACK mode
        targetEnemy = FindTargetEnemy();
        if (
            targetEnemy != null
            && Vector3.Distance(transform.position, targetEnemy.position) <= attackRange
        )
        {
            Debug.Log("🎯 AI found an enemy! Switching to ATTACK mode.");
            currentState = AIState.AttackEnemy;
            return;
        }

        // If AI detects a dropped flag, switch to retrieving it
        if (IsFlagMissing())
        {
            Transform droppedFlag = FindDroppedFlag();
            if (droppedFlag != null)
            {
                Debug.Log("🚩 AI detected dropped flag! Switching to retrieve it.");
                currentState = AIState.RetrieveFlag;
                return;
            }
        }

        // If AI is holding a flag, return it to base
        if (flagHandler.heldFlag != null)
        {
            Debug.Log("🏃 AI is holding the flag! Returning to base.");
            currentState = AIState.ReturnFlag;
            return;
        }

        // If an enemy has the flag, chase them
        FlagCarrier = FindFlagCarrier();
        if (FlagCarrier != null)
        {
            Debug.Log("⚠️ AI is chasing enemy flag carrier!");
            currentState = AIState.ChaseFlagCarrier;
            return;
        }

        EvaluateSurvivalState();

        // **If AI is already patrolling or defending, don't override it.**
        if (currentState == AIState.Patrol || currentState == AIState.DefendBase)
        {
            return;
        }
    }

    // 🕒 Coroutine to transition from DefendBase to Patrol

    #endregion LogicalBrain

    #region UtilityFunctions
    private AIState GetRandomState()
    {
        AIState[] possibleStates = { AIState.Patrol, AIState.SeekFlag };
        if (
            Vector3.Distance(transform.position, enemyFlag.transform.position) < 20f
            && flagHandler.heldFlag == null
        )
        {
            return AIState.SeekFlag;
        }
        return possibleStates[UnityEngine.Random.Range(0, possibleStates.Length)];
    }

    private string GetPlayerTeam(GameObject playerObj)
    {
        FlagHandler player = playerObj.GetComponent<FlagHandler>(); // Human players
        AIController aiCharacter = playerObj.GetComponent<AIController>(); // Bots

        if (player != null)
            return player.Team; // Get human player's team
        if (aiCharacter != null)
            return aiCharacter.team; // Get AI bot's team

        return null; // No team found (should never happen)
    }

    private void AssignPatrolPoints()
    {
        // Determine which patrol points to use based on team
        GameObject[] foundPatrolPoints;

        if (team == "Red")
        {
            foundPatrolPoints = GameObject.FindGameObjectsWithTag("RedPatrol");
        }
        else
        {
            foundPatrolPoints = GameObject.FindGameObjectsWithTag("BluePatrol");
        }

        if (foundPatrolPoints.Length == 0)
        {
            Debug.LogError($"⚠️ No patrol points found for {team} team!");
            return;
        }

        patrolPoints = new Transform[foundPatrolPoints.Length];
        for (int i = 0; i < foundPatrolPoints.Length; i++)
        {
            patrolPoints[i] = foundPatrolPoints[i].transform;
        }

        Debug.Log($"✅ Assigned {patrolPoints.Length} patrol points for {team} team.");
    }

    public void NotifyHit()
    {
        lastHitTime = Time.time;
        isUnderAttack = true;
    }

    private Transform FindFlagCarrier()
    {
        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject playerObj in allPlayers)
        {
            FlagHandler flagHandler = playerObj.GetComponent<FlagHandler>();
            string playerTeam = GetPlayerTeam(playerObj); // Get correct team

            // 🛑 Ignore friendly flag carriers (AI or human)
            if (playerTeam == team)
                continue;

            if (flagHandler != null && flagHandler.heldFlag != null)
            {
                if (flagHandler.heldFlag.CompareTag(team == "Red" ? "RedFlag" : "BlueFlag"))
                {
                    Debug.Log($"🚩 Chasing Flag Carrier: {playerObj.name}");
                    FlagCarrier = playerObj.transform; // Assign any enemy flag carrier
                    return FlagCarrier;
                }
            }
        }

        Debug.Log("⚠️ No enemy flag carrier found.");
        return null;
    }

    IEnumerator CheckIfReachedCover()
    {
        float timeout = 5f;
        float elapsedTime = 0f;

        while (elapsedTime < timeout)
        {
            yield return new WaitForSeconds(0.5f);
            elapsedTime += 0.5f;

            float distance = Vector3.Distance(transform.position, coverTarget.position);
            Debug.Log($"<color=#FFD700>📏 Distance to cover: {distance:F2}</color>");

            if (distance < 1.5f) // Adjust if AI stops too early
            {
                Debug.Log("<color=#32CD32>🛡️ AI reached cover successfully!</color>");
                StartCoroutine(StayInCover());
                yield break;
            }
        }

        Debug.LogError("<color=#FF0000>❌ AI failed to reach cover! Check NavMesh.</color>");
    }

    private void AssignFlagLocationsAndProperties()
    {
        redFlagBasePosition = GameObject.FindGameObjectWithTag("RL");
        blueFlagBasePosition = GameObject.FindGameObjectWithTag("BL");
        enemyFlag = GameObject.FindGameObjectWithTag(team == "Red" ? "BlueFlag" : "RedFlag");
        homeBase = GameObject.FindGameObjectWithTag(team == "Red" ? "RL" : "BL");
        basePosition = GameObject.FindGameObjectWithTag(team == "Red" ? "RedBase" : "BlueBase");
        if (redFlagBasePosition == null || blueFlagBasePosition == null || basePosition == null)
        {
            Debug.LogError("❌ ERROR: Flag base positions not found! Check your scene setup.");
        }
    }

    private IEnumerator TransitionToPatrol()
    {
        yield return new WaitForSeconds(5f); // Wait 5 seconds before switching to patrol
        if (currentState == AIState.DefendBase) // Ensure AI is still in DefendBase
        {
            Debug.Log("🔄 AI finished defending, switching to Patrol.");
            currentState = AIState.Patrol;
        }
    }

    #endregion UtilityFunctions
}
