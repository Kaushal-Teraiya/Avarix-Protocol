//using Mirror;
//using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(playerSetup))]
public class player : NetworkBehaviour
{
    [SyncVar]
    private bool _isDead = false;
    public bool isDead
    {
        get { return _isDead; }
        protected set { _isDead = value; }
    }

    [SerializeField]
    private HealthUI healthUI;
    public Rigidbody _rb;
    private weaponGraphics WG;

    [SerializeField]
    private GameObject PlayerRagdoll;

    [SerializeField]
    public int maxHealth = 100;
    private int RealHealth;

    [SyncVar(hook = nameof(OnHealthChanged))]
    public int currentHealth;

    [SerializeField]
    private Behaviour[] disableOnDeath;

    [SerializeField]
    private GameObject[] disableGameObjectsOnDeath;
    private bool[] wasEnabled;

    [SerializeField]
    private GameObject deathEffect;

    [SerializeField]
    private GameObject spawnEffect;
    private bool firstSetup = true;
    private FlagHandler fh;
    private playerShoot PS;

    [SerializeField]
    private float gunForce;

    [SerializeField]
    private float gunTorque;
    RagdollManager RM;
    public GameObject WeaponHolder;
    public GameObject gun;

    public GameObject healthUICanvas;

    //Animator anim = GetComponent<Animator>();
    [SerializeField]
    private CapsuleCollider mainCollider;

    public GameObject worldGun; // Gun A (Visible to others, not to me)
    public GameObject fpsGun; // Gun B (Visible only to me)
    public GameObject healthDropPrefab;

    private FlagHandler FHD;
    private string teamNamefromFH;
    private PlayerNameUI nameUI;

    // True for blue, false for red

    private CharacterSpawner spawner;

    public GameObject NewWeaponHolder;
    public GameObject FPShands;
    public TextMeshProUGUI HealthText;
    Camera cam;
    string killerName;

    [SerializeField]
    private GameObject PlayerNameHolder;

    [SerializeField]
    private GameObject HealthBarUIobj;

    public Slider HealthBarUI;
    private PlayerHealthBar pplayerHealthBar;

    [SyncVar]
    public string nameOfPlayer;
    private playerWeapon currentWeapon;
    public weaponManager WeaponManager;

    public Vector3 storedHitPoint;
    public Vector3 storedHitDirection;
    public string storedHitBodyPartName;

    void Start()
    {
        fh = GetComponent<FlagHandler>();
        PS = GetComponent<playerShoot>(); // Assign playerShoot reference
        RM = GetComponent<RagdollManager>();
        mainCollider = GetComponent<CapsuleCollider>();
        _rb = GetComponent<Rigidbody>();
        //  WG = GetComponent<weaponGraphics>();
        FHD = GetComponent<FlagHandler>();
        teamNamefromFH = FHD.Team;
        Animator animator = GetComponent<Animator>();
        spawner = FindAnyObjectByType<CharacterSpawner>();
        pplayerHealthBar = GetComponent<PlayerHealthBar>();
        HealthBarUI = HealthBarUIobj.GetComponent<Slider>();
        HealthText = FindAnyObjectByType<TextMeshProUGUI>();
        healthUI = GetComponentInChildren<HealthUI>();
        WeaponManager = GetComponent<weaponManager>();
        currentWeapon = WeaponManager.GetcurrentWeapon();
        // currentWeapon = GetComponentInChildren<playerWeapon>();

        NewWeaponHolder.SetActive(false);
        cam = Camera.main;
        if (spawnEffect == null)
        {
            Debug.Log("spawner is null");
        }
        else
        {
            Debug.Log("found spawner");
        }

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        if (HealthBarUI == null)
        {
            Debug.Log("UI for HealthBar is NULL");
        }

        UnityEngine.Animations.Rigging.RigBuilder rigBuilder =
            GetComponent<UnityEngine.Animations.Rigging.RigBuilder>();
        if (rigBuilder != null)
        {
            rigBuilder.enabled = false; // Force reset
            rigBuilder.enabled = true; // Enable it again
            rigBuilder.Build(); // Rebuild the rig to ensure it's applied properly
        }

        // ✅ Ensure correct gun visibility at Start (Backup fix)
        if (!isLocalPlayer)
        {
            if (FPShands != null)
            {
                FPShands.SetActive(false);
            }
            if (fpsGun != null)
                fpsGun.SetActive(false); // Hide FPS gun for non-local
            if (worldGun != null)
                worldGun.SetActive(true); // Show gun holder for non-local
            gameObject.GetComponentInChildren<Canvas>().gameObject.SetActive(false);

            if (healthUICanvas != null)
            {
                healthUICanvas.SetActive(false);
            }
        }

        if (isServer)
        {
            currentHealth = maxHealth; // ✅ Initialize health only on server
            RealHealth = currentHealth;
        }
        if (isLocalPlayer)
        {
            healthUI = GetComponentInChildren<HealthUI>();
            if (healthUI == null)
            {
                Debug.LogError("HealthUI not found in player prefab!");
            }
            else
            {
                healthUI.SetHealth(currentHealth);
            }
            healthUI.SetHealth(currentHealth);
        }

        nameUI = GetComponentInChildren<PlayerNameUI>();

        // Fetch name from PlayerPrefs
        string playerName = PlayerPrefs.GetString("PlayerName", "Player"); // Default to "Player" if not set
        nameOfPlayer = playerName;
        if (nameUI != null)
        {
            nameUI.SetPlayerName(playerName, teamNamefromFH);
        }

        if (!NetworkClient.ready)
        {
            NetworkClient.Ready();
        }
    }

    [Command]
    void CmdSetPlayerName(string _name)
    {
        nameOfPlayer = _name; // ✅ SyncVar will now update across all clients
    }

    void Awake()
    {
        // StartCoroutine(Respawn());
    }

    public void PlayerSetup()
    {
        if (isLocalPlayer)
        {
            GameManager.instance.setSceneCameraActive(false);
            GameManager.instance.sceneCamera.GetComponent<SceneCameraController>().enabled = false;
            GetComponent<playerSetup>().playerUIInstance.SetActive(true);
            PlayerNameHolder.gameObject.SetActive(false);
            HealthBarUIobj.gameObject.SetActive(false); //diabling UI on localplayer
        }

        CmdBroadCastNewPlayerSetup();
    }

    [Command(requiresAuthority = false)]
    private void CmdBroadCastNewPlayerSetup()
    {
        RpcSetupPlayerOnAllClients();
    }

    [ClientRpc]
    private void RpcSetupPlayerOnAllClients()
    {
        if (firstSetup)
        {
            wasEnabled = new bool[disableOnDeath.Length];
            for (int i = 0; i < wasEnabled.Length; i++)
            {
                wasEnabled[i] = disableOnDeath[i].enabled;
            }
            firstSetup = false;
        }
        SetDefaults();
        //StartCoroutine(TrySetDefaultsDelayed());
    }

    [Command]
    public void CmdKillSelf(int DMGamount)
    {
        RpcKillSelf(DMGamount);
    }

    [ClientRpc]
    public void RpctakeDamageP(
        int _amount,
        NetworkIdentity killerName,
        Vector3 hitpoint,
        Vector3 hitDirection,
        string hitBodyPartName
    )
    {
        if (isDead)
            return;

        if (TryGetComponent<AIController>(out AIController ai)) // If it's an AI, notify it
        {
            ai.NotifyHit();
            Debug.Log("Notify called ****************************************");
        }

        currentHealth = Mathf.Clamp(currentHealth - _amount, 0, maxHealth);
        Debug.Log(transform.name + " now has " + currentHealth + " Health");

        if (currentHealth <= 0 && !isDead)
        {
            CancelInvoke("Shoot");

            // Send attacker name to Suicide (aka Die)
            Die(killerName, hitpoint, hitDirection, hitBodyPartName);
        }
    }

    [Server]
    public void TakeDamage(
        int amount,
        NetworkIdentity killerId,
        Vector3 hitPoint,
        Vector3 hitDirection,
        string hitBodyPartName
    )
    {
        if (isDead)
            return;

        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
        Debug.Log($"{name} now has {currentHealth} Health");

        // Notify AI if needed
        if (TryGetComponent<AIController>(out AIController ai))
        {
            ai.NotifyHit();
        }

        // Update health UI for local player
        RpcUpdateHealth(currentHealth);

        if (currentHealth <= 0 && !isDead)
        {
            Die(killerId, hitPoint, hitDirection, hitBodyPartName);
        }
    }

    [ClientRpc]
    private void RpcUpdateHealth(int newHealth)
    {
        currentHealth = newHealth;
        UpdateHealth(newHealth);
    }

    [ClientRpc]
    public void RpctakeDamageU(int _amount, NetworkIdentity killerName)
    {
        if (isDead)
            return;

        if (TryGetComponent<AIController>(out AIController ai)) // If it's an AI, notify it
        {
            ai.NotifyHit();
            Debug.Log("Notify called ****************************************");
        }

        currentHealth = Mathf.Clamp(currentHealth - _amount, 0, maxHealth);
        Debug.Log(transform.name + " now has " + currentHealth + " Health");

        if (currentHealth <= 0 && !isDead)
        {
            CancelInvoke("Shoot");

            // Send attacker name to Suicide (aka Die)
            DieU(killerName);
        }
    }

    [ClientRpc]
    private void RpcKillSelf(int _amount)
    {
        if (isDead)
        {
            return;
        }
        currentHealth = Mathf.Clamp(currentHealth - _amount, 0, maxHealth);
        //PlayerHealthBar.RpcUpdateHealthBar(currentHealth);
        Debug.Log(transform.name + "now has " + currentHealth + "Health");
        // Force SyncVar to trigger its hook by setting it to a different value
        int temp = currentHealth;
        currentHealth = -1; // Temporary invalid value
        currentHealth = temp; // Set back to correct value

        if (currentHealth <= 0 && !isDead)
        {
            Suicide();
        }
    }

    private void Suicide()
    {
        isDead = true;
        // PS.enabled = false;
        _rb.isKinematic = true;

        Animator animator = GetComponent<Animator>();
        animator.enabled = false;
        GetComponent<UnityEngine.Animations.Rigging.RigBuilder>().enabled = false;

        if (fh.heldFlag != null)
        {
            Debug.Log($"Player died while holding flag: {fh.heldFlag.name}");
            fh.heldFlag.transform.SetParent(null);
            fh.CmdDropFlag(transform.position);
        }

        if (PlayerRagdoll != null)
        {
            PlayerRagdoll.transform.parent = null;
        }

        for (int i = 0; i < disableOnDeath.Length; i++)
        {
            disableOnDeath[i].enabled = false;
        }
        for (int i = 0; i < disableGameObjectsOnDeath.Length; i++)
        {
            disableGameObjectsOnDeath[i].SetActive(false);
        }

        if (mainCollider != null && _rb != null)
        {
            Debug.Log("Disabling Collider: " + mainCollider.name);
            mainCollider.enabled = false;
            _rb.isKinematic = true;
        }
        Debug.Log(transform.name + " is dead");

        GameObject _gfxInstance = Instantiate(deathEffect, transform.position, Quaternion.identity);
        Destroy(_gfxInstance, 3f);

        if (isLocalPlayer)
        {
            GameManager.instance.setSceneCameraActive(true);
            GameManager.instance.SetSceneCameraAbovePlayer(transform.position);
            GetComponent<playerSetup>().playerUIInstance.SetActive(false);
        }

        //RM.CmdEnableRagdoll();
        // RM.CmdEnableRagdollNoForce();
        //  RM.CmdSetRagdoll();
        PlayerNameHolder.SetActive(false);
        HealthBarUIobj.SetActive(false);

        // ✅ Ensure UI updates health text when dying
        UpdateHealth(0);
        //currentWeapon.currentAmmo = 0;
        if (PS != null)
        {
            PS.CancelInvoke("Shoot");
        }

        if (isServer)
            CmdSpawnHealthDrop();

        transform.rotation = Quaternion.Euler(
            90f,
            transform.rotation.eulerAngles.y,
            transform.rotation.eulerAngles.z
        );

        StartCoroutine(Respawn());
    }

    [ClientRpc]
    void RpcAddKillFeed(string killer, string victim)
    {
        Debug.Log($"[ClientRpc] Feed: {killer} 🔫 {victim}");
        KillFeedManager.Instance.AddKillFeedEntry(killer, victim);
    }

    private void Die(
        NetworkIdentity killerId,
        Vector3 hitPoint,
        Vector3 hitDirection,
        string hitBodyPartName
    )
    {
        isDead = true;
        // PS.enabled = false;
        _rb.isKinematic = true;

        Debug.Log(
            $"<color=yellow>[Die()]</color> <color=green>killerId:</color> <color=cyan>{killerId?.netId} ({killerId?.name})</color> | <color=red>This Player:</color> <color=cyan>{GetComponent<NetworkIdentity>()?.netId} ({nameOfPlayer})</color>"
        );

        if (killerId != null && killerId != GetComponent<NetworkIdentity>())
        {
            var killerPlayer = killerId.GetComponent<player>();
            if (killerPlayer != null)
            {
                killerName = killerPlayer.nameOfPlayer; // ✅ SyncVar will now give correct name
            }
        }

        if (isServer && killerId != null && killerId != GetComponent<NetworkIdentity>())
        {
            PlayerStats killerStats = killerId.GetComponent<PlayerStats>();
            PlayerStats victimStats = GetComponent<PlayerStats>();

            if (killerStats != null)
                killerStats.CmdAddKill();

            if (victimStats != null)
                victimStats.CmdAddDeath();

            NetworkScoreBoard scoreboard = FindObjectsByType<NetworkScoreBoard>(
                FindObjectsSortMode.None
            )[0];
            if (scoreboard != null)
            {
                scoreboard.CmdUpdateScoreBoard(); // triggers Rpc
            }
        }

        // Get victim color from this player's FlagHandler
        string victimColor = "white";
        FlagHandler myFH = GetComponent<FlagHandler>();
        if (myFH != null)
        {
            victimColor = myFH.Team == "Blue" ? "#00BFFF" : "#FF3E3E"; // Bright Blue / Bright Red
        }

        // Get killer color from killer's FlagHandler
        string killerColor = "white";
        if (killerId != null && killerId != GetComponent<NetworkIdentity>())
        {
            var killerPlayer = killerId.GetComponent<player>();
            if (killerPlayer != null)
            {
                killerName = killerPlayer.nameOfPlayer;
                FlagHandler killerFH = killerPlayer.GetComponent<FlagHandler>();
                if (killerFH != null)
                {
                    killerColor = killerFH.Team == "Blue" ? "#00BFFF" : "#FF3E3E";
                }
            }
        }

        // Apply color tags
        string coloredKiller = $"<color={killerColor}>{killerName}</color>";
        string coloredVictim = $"<color={victimColor}>{nameOfPlayer}</color>";

        Debug.Log(
            $"<color=yellow>KillFeed</color> - Killer: {coloredKiller} | Victim: {coloredVictim}"
        );

        // Send to kill feed
        if (isServer)
        {
            RpcAddKillFeed(coloredKiller, coloredVictim);
        }

        // KillFeedManager.Instance?.AddKillFeedEntry(killerName, victimName);

        Animator animator = GetComponent<Animator>();
        animator.enabled = false;
        GetComponent<UnityEngine.Animations.Rigging.RigBuilder>().enabled = false;

        if (fh.heldFlag != null)
        {
            Debug.Log($"Player died while holding flag: {fh.heldFlag.name}");
            fh.heldFlag.transform.SetParent(null);
            fh.CmdDropFlag(transform.position);
        }

        if (PlayerRagdoll != null)
        {
            PlayerRagdoll.transform.parent = null;
        }

        for (int i = 0; i < disableOnDeath.Length; i++)
        {
            disableOnDeath[i].enabled = false;
        }
        for (int i = 0; i < disableGameObjectsOnDeath.Length; i++)
        {
            disableGameObjectsOnDeath[i].SetActive(false);
        }

        if (mainCollider != null && _rb != null)
        {
            Debug.Log("Disabling Collider: " + mainCollider.name);
            mainCollider.enabled = false;
            _rb.isKinematic = true;
        }
        Debug.Log(transform.name + " is dead");

        GameObject _gfxInstance = Instantiate(deathEffect, transform.position, Quaternion.identity);
        Destroy(_gfxInstance, 3f);

        if (isLocalPlayer)
        {
            GameManager.instance.setSceneCameraActive(true);
            GameManager.instance.SetSceneCameraAbovePlayer(transform.position);
            GetComponent<playerSetup>().playerUIInstance.SetActive(false);
        }

        RM.CmdEnableRagdoll();
        // RM.CmdEnableRagdoll(hitPoint, hitDirection, 500f);
        // RM.CmdSetRagdoll(hitPoint, hitDirection, 500f);
        GetComponent<Animator>().enabled = false;
        storedHitPoint = hitPoint;
        storedHitDirection = hitDirection;
        storedHitBodyPartName = hitBodyPartName;
        // StartCoroutine(DelayedForceToHips());
        //  StartCoroutine(ApplyRagdollForce());
        PlayerNameHolder.SetActive(false);
        HealthBarUIobj.SetActive(false);

        // ✅ Ensure UI updates health text when dying
        UpdateHealth(0);
        //  currentWeapon.currentAmmo = 0;
        if (PS != null)
        {
            PS.CancelInvoke("Shoot");
        }

        if (isServer)
            CmdSpawnHealthDrop();

        transform.rotation = Quaternion.Euler(
            90f,
            transform.rotation.eulerAngles.y,
            transform.rotation.eulerAngles.z
        );

        StartCoroutine(Respawn());
    }

    [ClientRpc]
    private void RpcHandleDeathEffects(NetworkIdentity killerId)
    {
        string victimColor = fh.Team == "Blue" ? "#00BFFF" : "#FF3E3E";
        string killerColor = "white";

        if (killerId != null && killerId != GetComponent<NetworkIdentity>())
        {
            var killerPlayer = killerId.GetComponent<player>();
            if (killerPlayer != null)
            {
                FlagHandler killerFH = killerPlayer.GetComponent<FlagHandler>();
                killerColor = killerFH.Team == "Blue" ? "#00BFFF" : "#FF3E3E";
            }
        }

        string coloredKiller = $"<color={killerColor}>{killerName}</color>";
        string coloredVictim = $"<color={victimColor}>{nameOfPlayer}</color>";

        Debug.Log(
            $"<color=yellow>KillFeed</color> - Killer: {coloredKiller} | Victim: {coloredVictim}"
        );
        RpcAddKillFeed(coloredKiller, coloredVictim);

        // Handle ragdoll visuals
        if (PlayerRagdoll != null)
            PlayerRagdoll.transform.parent = null;

        Animator animator = GetComponent<Animator>();
        animator.enabled = false;
        GetComponent<UnityEngine.Animations.Rigging.RigBuilder>().enabled = false;
    }

    // IEnumerator DelayedForceToHips()
    // {
    //     yield return new WaitForSeconds(0.1f);
    //     RM.ApplyForceToRagdoll(storedHitPoint, storedHitDirection);
    // }

    private void DieU(NetworkIdentity killer)
    {
        isDead = true;
        // PS.enabled = false;
        _rb.isKinematic = true;
        Animator animator = GetComponent<Animator>();
        animator.enabled = false;
        GetComponent<UnityEngine.Animations.Rigging.RigBuilder>().enabled = false;

        if (fh.heldFlag != null)
        {
            Debug.Log($"Player died while holding flag: {fh.heldFlag.name}");
            fh.heldFlag.transform.SetParent(null); // Force detach
            fh.CmdDropFlag(transform.position);
        }

        // Ignore collisions between player collider and ragdoll colliders
        CapsuleCollider playerCollider = GetComponent<CapsuleCollider>();
        Collider[] ragdollColliders = GetComponentsInChildren<Collider>();
        foreach (Collider ragdollCollider in ragdollColliders)
        {
            Physics.IgnoreCollision(playerCollider, ragdollCollider, true);
        }

        if (PlayerRagdoll != null)
        {
            PlayerRagdoll.transform.parent = null;
        }

        for (int i = 0; i < disableOnDeath.Length; i++)
        {
            disableOnDeath[i].enabled = false;
        }
        for (int i = 0; i < disableGameObjectsOnDeath.Length; i++)
        {
            disableGameObjectsOnDeath[i].SetActive(false);
        }

        // Ensure main collider is disabled
        if (mainCollider == null)
        {
            Debug.LogError("mainCollider was null, assigning now.");
            mainCollider = GetComponent<CapsuleCollider>();
        }
        if (mainCollider != null)
        {
            Debug.Log("Disabling Collider: " + mainCollider.name);
            mainCollider.enabled = false;
        }
        else
        {
            Debug.LogError("mainCollider is NULL! Assign it in Inspector or via script.");
        }

        Debug.Log(transform.name + " is dead");

        GameObject _gfxInstance = Instantiate(deathEffect, transform.position, Quaternion.identity);
        Destroy(_gfxInstance, 3f);

        if (isLocalPlayer)
        {
            GameManager.instance.setSceneCameraActive(true);
            GetComponent<playerSetup>().playerUIInstance.SetActive(false);
        }

        // Enable Ragdoll
        if (RM != null)
        {
            // RM.CmdEnableRagdoll();
            //  RM.cmd();
        }
        else
        {
            Debug.LogError("RagdollManager (RM) is NULL! Ensure it's assigned.");
        }

        PlayerNameHolder.SetActive(false);
        HealthBarUIobj.SetActive(false);

        if (killer != null && killer.TryGetComponent(out NetworkIdentity killerIdentity))
        {
            // CmdSpawnHealthDrop(killerIdentity.connectionToClient);
        }

        // Apply force to ragdoll after enabling it
        // RM.CmdEnableRagdoll();
        //  RM.CmdEnableRagdollNoForce();
        StartCoroutine(Respawn());
    }

    // private IEnumerator ApplyRagdollForce()
    // {
    //     if (RM == null)
    //     {
    //         Debug.LogError("RagdollManager is NULL! Cannot apply force.");
    //         yield break;
    //     }

    //     yield return new WaitForSeconds(0.1f); // Let ragdoll activate

    //     // Find the closest Rigidbody to the hitPoint
    //     Rigidbody closestRb = null;
    //     float closestDistance = float.MaxValue;

    //     foreach (Rigidbody rb in RM.GetAllRigidbodies())
    //     {
    //         float dist = Vector3.Distance(rb.worldCenterOfMass, storedHitPoint);
    //         if (dist < closestDistance)
    //         {
    //             closestDistance = dist;
    //             closestRb = rb;
    //         }
    //     }

    //     if (closestRb != null)
    //     {
    //         closestRb.isKinematic = false;

    //         Vector3 forceDir = storedHitDirection.normalized; // Could also use bullet direction
    //         float forceMag = Random.Range(300f, 500f);
    //         Vector3 force = forceDir * forceMag;

    //         closestRb.AddForceAtPosition(force, storedHitPoint, ForceMode.Impulse);
    //         Debug.Log($"💥 Applied force to {closestRb.name} at {storedHitPoint} with {force}");
    //     }
    //     else
    //     {
    //         Debug.LogWarning("No suitable Rigidbody found to apply ragdoll force.");
    //     }
    // }

    IEnumerator Respawn()
    {
        yield return new WaitForSeconds(4f);

        if (PlayerRagdoll != null)
        {
            PlayerRagdoll.transform.parent = transform;
        }

        transform.rotation = Quaternion.Euler(
            0f,
            transform.rotation.eulerAngles.y,
            transform.rotation.eulerAngles.z
        );
        // RM.CmdDisableRagdoll();
        // RM.CmdClearRagdoll();
        RM.ResetRagdollPose();
        PlayerNameHolder.SetActive(true);
        HealthBarUIobj.SetActive(true);

        yield return new WaitForSeconds(GameManager.instance.matchSettings.respawnTime - 4f);

        Transform _spawnPoint = null;
        string playerTeam = fh.Team;

        if (playerTeam == "Blue" && spawner.availableTeamASpawns.Count >= 0)
        {
            if (spawner.availableTeamASpawns.Count == 0)
            {
                Debug.Log("🔄 Resetting available spawn points for Team Blue.");
                spawner.availableTeamASpawns = new List<Transform>(spawner.teamASpawnPoints);
            }
            int index = Random.Range(0, spawner.availableTeamASpawns.Count);
            _spawnPoint = spawner.availableTeamASpawns[index];
            spawner.availableTeamASpawns.RemoveAt(index);
        }
        else if (playerTeam == "Red" && spawner.availableTeamBSpawns.Count >= 0)
        {
            if (spawner.availableTeamBSpawns.Count == 0)
            {
                Debug.Log("🔄 Resetting available spawn points for Team Red.");
                spawner.availableTeamBSpawns = new List<Transform>(spawner.teamBSpawnPoints);
            }
            int index = Random.Range(0, spawner.availableTeamBSpawns.Count);
            _spawnPoint = spawner.availableTeamBSpawns[index];
            spawner.availableTeamBSpawns.RemoveAt(index);
        }

        if (_spawnPoint == null)
        {
            Debug.LogError($"[ERROR] No available spawn point for team {playerTeam}!");
        }

        transform.position = _spawnPoint.position;
        transform.rotation = _spawnPoint.rotation;

        yield return new WaitForSeconds(0.1f);

        Animator animator = GetComponent<Animator>();
        animator.enabled = true;
        GetComponent<UnityEngine.Animations.Rigging.RigBuilder>().enabled = true;

        currentHealth = maxHealth;
        Debug.Log($"[SERVER] {gameObject.name} respawned with {currentHealth} HP");

        UpdateHealth(maxHealth);
        if (isServer)
        {
            PlayerHealthBar playerHealthBar = GetComponentInChildren<PlayerHealthBar>();
            playerHealthBar.RpcUpdateHealthBar(currentHealth);
        }

        PlayerSetup();
    }

    [Command(requiresAuthority = false)]
    public void UpdateHealth(int newHealth)
    {
        currentHealth = newHealth;
        RpcUpdateHealthUI(currentHealth); // Make sure only the server calls this!
    }

    public void SetDefaults()
    {
        Debug.Log($"[SetDefaults] IsLocalPlayer: {isLocalPlayer}, HasAuthority: {isOwned}");

        Debug.Log($"[SetDefaults] IsLocalPlayer: {isLocalPlayer}, HasAuthority: {isOwned}");

        WeaponManager = GetComponent<weaponManager>();
        if (WeaponManager == null)
        {
            Debug.LogError("[SetDefaults] WeaponManager is NULL");
            return;
        }

        currentWeapon = WeaponManager.GetcurrentWeapon();
        if (currentWeapon == null)
        {
            Debug.LogError("[SetDefaults] currentWeapon is NULL");
            return;
        }

        PS = GetComponent<playerShoot>();
        // if (PS == null)
        // {
        //     Debug.LogError("[SetDefaults] PS (PlayerShoot) is NULL");
        //     return;
        // }

        // Check if currentWeapon is ready
        if (currentWeapon == null)
        {
            Debug.LogWarning("[SetDefaults] currentWeapon is NULL");
            currentWeapon = WeaponManager.GetcurrentWeapon();
        }
        else
        {
            Debug.Log("[SetDefaults] currentWeapon is initialized");
        }

        // Check if PS.currentWeapon is ready
        if (PS != null && PS.currentWeapon == null)
        {
            Debug.LogWarning("[SetDefaults] PS.currentWeapon is NULL");
        }
        else if (PS != null)
        {
            Debug.Log("[SetDefaults] PS.currentWeapon is initialized");
        }

        // Check if PS.ammoText is ready
        if (PS != null && PS.ammoText == null)
        {
            Debug.LogWarning("[SetDefaults] PS.ammoText is NULL");
        }
        else if (PS != null)
        {
            Debug.Log("[SetDefaults] PS.ammoText is initialized");
        }

        // Check if currentWeapon.maxAmmo exists
        if (currentWeapon != null && currentWeapon.maxAmmo == 0)
        {
            Debug.LogWarning("[SetDefaults] currentWeapon.maxAmmo is 0");
        }
        else if (currentWeapon != null)
        {
            Debug.Log("[SetDefaults] currentWeapon.maxAmmo is valid");
        }

        // Normal SetDefaults behavior if everything is ready
        //if (PS != null && currentWeapon != null && PS.currentWeapon != null && PS.ammoText != null)
        if (currentWeapon != null)
        {
            // Proceed with SetDefaults
            isDead = false;
            currentHealth = maxHealth;
            for (int i = 0; i < disableOnDeath.Length; i++)
            {
                disableOnDeath[i].enabled = wasEnabled[i];
            }

            for (int i = 0; i < disableGameObjectsOnDeath.Length; i++)
            {
                disableGameObjectsOnDeath[i].SetActive(true);
            }

            CapsuleCollider _col = GetComponent<CapsuleCollider>();
            if (_col != null)
                _col.enabled = true;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            Transform rootBone = transform.Find("Hips");
            if (rootBone)
            {
                transform.position = rootBone.position;
            }
            GameManager.instance.sceneCamera.GetComponent<SceneCameraController>().enabled = false;

            GameObject _gfxInstance = Instantiate(
                spawnEffect,
                transform.position,
                Quaternion.identity
            );
            Destroy(_gfxInstance, 3f);

            foreach (Transform child in transform.root)
            {
                if (child.name.Contains("Ragdoll_Pumpkin"))
                {
                    return;
                }
            }

            if (currentWeapon != null)
            {
                Debug.LogWarning("current weapon assigned !");
            }
            else
            {
                currentWeapon = WeaponManager.GetcurrentWeapon();
            }

            if (PS == null)
            {
                PS = GetComponent<playerShoot>();
                Debug.LogWarning("player shoot script is null, tried to assign it");
            }

            if (PS != null)
            {
                PS.currentWeapon.currentAmmo = currentWeapon.maxAmmo;
                PS.ammoText.text = currentWeapon.currentAmmo.ToString();
                PS.CancelInvoke("Shoot");
                PS.CancelInvoke("Recoil");
            }
            else
            {
                Debug.LogWarning("STILL NOT FOUND PLAYER SHOOT SCRIPT");
            }
        }
        else
        {
            Debug.LogWarning("[SetDefaults] Some components are not ready. Aborting SetDefaults.");
        }
    }

    private bool IsPlayerSetupComplete()
    {
        return PS != null && PS.ammoText != null && currentWeapon != null;
    }

    IEnumerator TrySetDefaultsDelayed()
    {
        float timeout = 5f;
        float t = 0f;

        while (!IsPlayerSetupComplete() && t < timeout)
        {
            Debug.Log("Waiting for player setup...");
            t += Time.deltaTime;
            yield return null;
        }

        if (IsPlayerSetupComplete())
        {
            SetDefaults();
        }
        else
        {
            Debug.LogError("SetDefaults() failed: Player components not ready after timeout.");
        }
    }

    void Update()
    {
        if (!isLocalPlayer)
        {
            return;
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            CmdKillSelf(99999);
        }
        if (currentWeapon == null)
        {
            currentWeapon = WeaponManager.GetcurrentWeapon();
        }
    }

    public override void OnStartLocalPlayer()
    {
        if (fpsGun != null)
        {
            fpsGun.SetActive(true); // Enable FPS gun only for local player
        }

        // Transform gunHolder = transform.Find("RightHand/GunHolder");
        if (worldGun != null)
        {
            worldGun.gameObject.SetActive(false); // Disable world gun for local player
        }

        if (FPShands != null)
        {
            FPShands.SetActive(true);
        }

        if (healthUICanvas != null)
        {
            healthUICanvas.SetActive(true); // Enable only for the local player
        }

        string playerName = PlayerPrefs.GetString("PlayerName", "Player");
        CmdSetPlayerName(playerName); // ✅ Sends it to the server

        // healthUI = GetComponentInChildren<HealthUI>();
        // if (healthUI == null)
        // {
        //     Debug.LogError("HealthUI not found in player prefab!");
        // }
        // else
        // {
        //     healthUI.SetHealth(currentHealth);
        // }
        // // Update UI initially
        // healthUI.SetHealth(currentHealth);
    }

    // public override void OnStartClient()
    // {
    //     base.OnStartClient();

    //     if (isLocalPlayer)
    //     {
    //         currentHealth = maxHealth;  // ✅ Ensure health is max on start
    //         RpcUpdateHealthUI(currentHealth); // ✅ Update health UI immediately
    //         Animator animator = GetComponent<Animator>();
    //         animator.enabled = true;
    //         GetComponent<UnityEngine.Animations.Rigging.RigBuilder>().enabled = true;
    //     }
    // }

    public override void OnStartServer()
    {
        base.OnStartServer();

        currentHealth = maxHealth; // ✅ Ensure server initializes health
    }

    // void UpdateHealthUI(int newHealth)
    // {
    //     if (!isLocalPlayer) return; // Ensure only the local player's UI updates

    //     if (HealthText != null)
    //     {
    //         HealthText.text = "Health: " + newHealth.ToString();
    //     }
    //     else
    //     {
    //         Debug.LogWarning("HealthText is not assigned!");
    //     }
    // }

    void OnHealthChanged(int oldHealth, int newHealth)
    {
        Debug.Log($"[CLIENT] {gameObject.name} HP updated: {newHealth}");

        if (healthUI == null)
        {
            healthUI = GetComponentInChildren<HealthUI>(); // Ensure UI is assigned
            if (healthUI == null)
                return;
        }

        healthUI.SetHealth(newHealth); // ✅ Update the UI
    }

    [ClientRpc]
    public void RpcUpdateHealthUI(int newHealth)
    {
        Debug.Log(
            $"[RPC] Update Health Bar called on {gameObject.name}, isServer: {isServer}, isClient: {isClient}, isLocalPlayer: {isLocalPlayer}"
        );
        if (!isClient)
            return; // ✅ Prevents execution if not a client
        if (!isLocalPlayer)
            return; // Ensure only local player updates UI

        Debug.Log(
            $"[CLIENT] {gameObject.name} updating health text to {newHealth} HP after respawn"
        );

        HealthUI _healthUI = GetComponentInChildren<HealthUI>();
        if (_healthUI != null)
        {
            _healthUI.SetHealth(newHealth); // ✅ Update health text
            Debug.Log($"[CLIENT] Health text updated to {newHealth}");
        }
        else
        {
            Debug.LogError("[CLIENT] HealthUI reference missing on respawn!");
        }
    }

    [Server]
    void CmdSpawnHealthDrop()
    {
        if (healthDropPrefab == null)
            return;

        Vector3 dropPosition = transform.position + Vector3.up * 5f;
        GameObject healthDrop = Instantiate(healthDropPrefab, dropPosition, Quaternion.identity);

        // Ensure Rigidbody starts without random movement
        if (healthDrop.TryGetComponent(out Rigidbody rb))
        {
            rb.AddForce(Vector3.up * 2f, ForceMode.Impulse); // Add upward force
            rb.AddTorque(Vector3.up * 100f, ForceMode.Impulse); // Add random spin
        }

        // ✅ Corrected: Now spawning it on the server properly!
        NetworkServer.Spawn(healthDrop);
        Debug.Log("Health drop spawned at " + dropPosition);
        // // Prevent the dead player from picking it up
        // if (healthDrop.TryGetComponent(out HealthDrop dropScript))
        // {
        //     dropScript.SetKillerOnly(killerConnection.identity); // Ensure only the killer can pick it up
        // }
    }

    void OnDestroy()
    {
        Debug.Log(gameObject.name + " was destroyed!");
    }

    public void Heal(int healAmount)
    {
        if (!isServer)
            return; // Ensure this runs only on the server

        Debug.Log(
            $"[SERVER] {gameObject.name} Healing by {healAmount}. Old Health: {currentHealth}"
        );

        currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth); // Ensure health doesn't exceed max

        Debug.Log($"[SERVER] {gameObject.name} New Health: {currentHealth}/{maxHealth}");

        //RpcUpdateHealthUI(currentHealth);
        UpdateHealth(currentHealth);
        PlayerHealthBar playerHealthBar = GetComponentInChildren<PlayerHealthBar>();
        playerHealthBar.RpcUpdateHealthBar(currentHealth);
    }

    IEnumerator DelayedHealthUpdate(int newHealth)
    {
        yield return new WaitUntil(() => NetworkClient.active);
        // RpcUpdateHealthUI(newHealth);
    }
}


// [ClientRpc]
// void RpcSetInitialHealth(int health)
// {
//     Debug.Log($"[ClientRpc] Setting initial health: {health} for {gameObject.name}");
//     healthUI.SetHealth();
// }



// private void DropGun()
// {
//     //  weaponGraphics.Instance.gunBody.isKinematic = false;
//     // weaponGraphics.Instance.gunBody.useGravity = true;
//     //  Vector3 dropDirection = -transform.forward + Vector3.up * 0.3f;
//     // weaponGraphics.Instance.gunBody.AddForce(dropDirection * gunForce, ForceMode.Impulse);

//     // Apply torque to make it spin naturally
//     Vector3 randomTorque = new Vector3(
//         Random.Range(-1f, 1f),
//         Random.Range(-1f, 1f),
//         Random.Range(-1f, 1f)
//     );
//     weaponGraphics.Instance.gunBody.AddTorque(randomTorque * gunTorque, ForceMode.Impulse);
//     gun.transform.SetParent(null);
// }

// private void ResetGun()
// {
//      //gun.transform.SetParent(WeaponHolder.transform);
//     weaponGraphics.Instance.GunTransform.localPosition = new Vector3(0,0,0);
//     weaponGraphics.Instance.GunTransform.localRotation = Quaternion.identity;
//     weaponGraphics.Instance.gunBody.isKinematic = true;
//     weaponGraphics.Instance.gunBody.useGravity = false;
//     Debug.Log(transform.name + "respawned");
// }
