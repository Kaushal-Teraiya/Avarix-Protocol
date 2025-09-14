using System;
using System.Collections;
using System.Linq;
using Mirror;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(weaponManager))]
public class playerShoot : NetworkBehaviour
{
    private const string PLAYER_TAG = "Player";
    public playerWeapon currentWeapon;

    [SerializeField]
    private Camera cam;

    [SerializeField]
    private LayerMask mask;
    public weaponManager WeaponManager;
    public GunSoundManager gunSoundManager;
    private float nextFireTime = 0f; // Keeps track of when the next shot is allowed

    //[SerializeField] private float RagdollForce;
    [SerializeField]
    private PlayerGunSelector gunSelector;
    RagdollManager RM;
    public Vector3 ActualBulletSpread = new Vector3(0.1f, 0.1f, 0.1f);
    public Rigidbody testrb;
    private player _player;
    private NetworkIdentity attackerId;
    private RaycastHit hitInfo;
    GunRecoil recoil;
    public bool canShoot;
    private Animator ShotgunRecoil = null;
    private Animator SniperRecoil = null;
    private Animator PistolRecoil = null;
    Transform gunHolder;
    public string ShotgunName;
    public TextMeshProUGUI ammoText;
    private int reloadDelay;

    [SyncVar(hook = nameof(OnAmmoChanged))]
    public int syncedAmmo;

    public bool isReloading = false;

    // ======= New interface and wrapper (non-breaking): =======
    // We add IGun so different gun types can implement behaviour.
    // The wrapper adapts your existing gun class (gunSelector.ActiveGun) to the interface.
    interface IGun
    {
        // Called on client to request a shot. Should call playerShoot.CmdRequestShoot(...) ultimately.
        void ShootClientRequest(playerShoot owner, Vector3 origin, Vector3 forward);

        // For server-side direct invocation (if needed).
        void ShootServer(playerShoot owner, Vector3 origin, Vector3 forward);

        // Special shotgun server shoot
        void ShootShotgunServer(playerShoot owner, Vector3 origin, Vector3 forward);
    }

    // Wrapper that adapts existing gun object (whatever ActiveGun is) to IGun
    // This prevents needing to change other gun classes immediately.
    class GunWrapper : IGun
    {
        public GunWrapper(PlayerGunSelector selector)
        {
            selectorRef = selector;
        }

        private PlayerGunSelector selectorRef;

        public void ShootClientRequest(playerShoot owner, Vector3 origin, Vector3 forward)
        {
            // Keep minimal: forwards request to server command on owner
            // We infer whether it's shotgun from owner's currentWeapon/guns effect
            bool isShotgun = false;
            try
            {
                if (
                    owner != null
                    && owner.currentWeapon != null
                    && owner.currentWeapon.GunName == "ShotGun"
                )
                    isShotgun = true;
            }
            catch { }

            owner.CmdRequestShoot(origin, forward, isShotgun);
        }

        public void ShootServer(playerShoot owner, Vector3 origin, Vector3 forward)
        {
            // server-side direct invocation (not used by client code normally)
            owner.ServerProcessHits(origin, forward);
        }

        public void ShootShotgunServer(playerShoot owner, Vector3 origin, Vector3 forward)
        {
            owner.ServerProcessShotgun(origin, forward);
        }
    }

    // =========================================================

    private void OnAmmoChanged(int oldAmmo, int newAmmo)
    {
        // Only update the UI if we are NOT currently reloading
        if (isLocalPlayer && !isReloading && ammoText != null)
        {
            ammoText.text = "Ammo:-" + newAmmo.ToString();
        }
    }

    // public string holderName;

    void Start()
    {
        RM = GetComponent<RagdollManager>();
        gunSelector = GetComponent<PlayerGunSelector>();
        gunSoundManager = FindAnyObjectByType<GunSoundManager>();
        WeaponManager = GetComponent<weaponManager>();
        currentWeapon = WeaponManager.GetcurrentWeapon();
        _player = GetComponent<player>();
        recoil = GetComponentInChildren<GunRecoil>();
        gunHolder = transform.Find("WeaponHolder");
        //ShotgunRecoil = GetComponentInChildren<Animator>();
        AssignAnimatorsForGuns();
        NullChecker();
        //CmdDieOnStart();
        // RM.CmdClearRagdoll();
        RM.CmdDisableRagdoll();
        StartCoroutine(WaitForNetworkReady());
        currentWeapon.currentAmmo = currentWeapon.maxAmmo;
        syncedAmmo = currentWeapon.maxAmmo;
        reloadDelay = currentWeapon.reloadDelay;
    }

    private void AssignAnimatorsForGuns()
    {
        Animator[] anims = GetComponentsInChildren<Animator>(true); // true = include inactive too
        foreach (Animator anim in anims)
        {
            if (anim.gameObject.name.Contains("Rustic_Shotgun (1)")) // case-sensitive, so be cautious
            {
                if (anim.gameObject.activeInHierarchy)
                {
                    ShotgunRecoil = anim;
                    Debug.Log("✅ Found and active: " + anim.gameObject.name);
                    break;
                }
                else
                {
                    Debug.LogWarning("Found Rustic_Shotgun but it's inactive!");
                }
            }

            if (anim.gameObject.name.Contains("L96_Sniper_Rifle")) // case-sensitive, so be cautious
            {
                if (anim.gameObject.activeInHierarchy)
                {
                    SniperRecoil = anim;
                    Debug.Log("✅ Found and active: " + anim.gameObject.name);
                    break;
                }
                else
                {
                    Debug.LogWarning("Found SniperRifle but it's inactive!");
                }
            }

            if (anim.gameObject.name.Contains("PISTOL")) // case-sensitive, so be cautious
            {
                if (anim.gameObject.activeInHierarchy)
                {
                    PistolRecoil = anim;
                    Debug.Log("✅ Found and active: " + anim.gameObject.name);
                    break;
                }
                else
                {
                    Debug.LogWarning("Found Pistol but it's inactive!");
                }
            }
        }
    }

    private void NullChecker()
    {
        if (gunSelector == null)
        {
            Debug.LogError("PlayerGunSelector is missing!");
        }
        else if (gunSelector.ActiveGun == null)
        {
            Debug.LogWarning("gunSelector found, but ActiveGun is NULL!");
        }
        if (cam == null)
        {
            Debug.LogError("playerShoot: no camera referenced");
            this.enabled = false;
        }
        if (WeaponManager == null)
        {
            Debug.LogError("WeaponManager is NULL!");
        }

        if (gunSoundManager == null)
        {
            Debug.Log("Gun sound Manager is null");
        }

        if (ammoText == null)
        {
            Debug.LogError("AmmoText is null!!");
        }
    }

    void Awake()
    {
        gunSelector = GetComponent<PlayerGunSelector>();
    }

    void Update()
    {
        if (_player == null)
        {
            Debug.LogError("player is null");
        }

        if (_player != null && _player.isDead)
        {
            Debug.LogWarning("Cannot shoot, player is dead.");
            currentWeapon.currentAmmo = 0;
            syncedAmmo = 0;
        }

        currentWeapon = WeaponManager.GetcurrentWeapon();

        if (currentWeapon == null)
        {
            Debug.LogWarning("No weapon found in WeaponManager!");
            return;
        }

        if (syncedAmmo <= 0 && !isReloading)
        {
            StartCoroutine(ReloadAmmo());
        }

        if (MatchTimer.Instance.hasMatchEnded)
        {
            return;
        }

        // Create wrapper on demand; keeps compatibility with existing ActiveGun
        IGun gun = new GunWrapper(gunSelector);

        if (Input.GetButtonDown("Fire1") && !isReloading)
        {
            Debug.Log("Fire1 input detected!");

            if (currentWeapon.FireRate <= 1f) // Single shot (e.g., sniper)
            {
                Debug.Log("Weapon is single-shot.");
                if (syncedAmmo > 0)
                {
                    if (Time.time >= nextFireTime)
                    {
                        Debug.Log("Cooldown passed. Attempting to shoot...");
                        // Client requests authoritative server shot through IGun wrapper
                        gun.ShootClientRequest(this, cam.transform.position, cam.transform.forward);

                        if (currentWeapon.GunName == "Sniper")
                        {
                            nextFireTime = Time.time + 2f;
                        }
                        if (currentWeapon.GunName == "Pistol")
                        {
                            nextFireTime = Time.time + 0.5f;
                        }
                        if (currentWeapon.GunName == "ShotGun")
                        {
                            nextFireTime = Time.time + 1.5f;
                        }
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"Still on cooldown. Next fire at: {nextFireTime}, current time: {Time.time}"
                        );
                    }
                }
                else
                {
                    Debug.LogWarning($"Ammo low. Reloading...");
                }
            }
            else // Auto fire
            {
                Debug.Log("Weapon is automatic. Starting continuous fire.");
                if (syncedAmmo > 0)
                {
                    if (!IsInvoking("Shoot"))
                    {
                        InvokeRepeating("Shoot", 0f, 1f / currentWeapon.FireRate);
                        InvokeRepeating("Recoil", 0f, 1f / currentWeapon.FireRate);
                        // handRecoil.SetTrigger("Fire");
                    }
                }
                else
                {
                    Debug.LogWarning("Ammo low Reloading....");
                    CancelInvoke("Recoil");
                }
            }
        }
        else if (Input.GetButtonUp("Fire1"))
        {
            Debug.Log("Fire1 input released. Stopping fire.");
            CancelInvoke("Shoot");
            CancelInvoke("Recoil");
            // ReloadAmmo();
        }
    }

    private IEnumerator ReloadAmmo()
    {
        isReloading = true;
        canShoot = false;

        yield return new WaitForSeconds(reloadDelay);
        // Server should be authoritative for syncedAmmo; but for singleplayer or host this will set it locally too.
        if (isServer)
        {
            syncedAmmo = currentWeapon.maxAmmo;
        }
        else
        {
            // Ask server to reload for authoritative update
            CmdReloadAmmo();
        }
        ammoText.text = "Ammo: " + syncedAmmo.ToString();
        isReloading = false;
        canShoot = true;
    }

    [Command]
    void CmdStartReload()
    {
        // Optional: mark server state so it stops sending down bad ammo updates
        isReloading = true;
    }

    [Command]
    void CmdFinishReload()
    {
        syncedAmmo = currentWeapon.maxAmmo;
        isReloading = false;
    }

    [Command]
    void CmdReloadAmmo()
    {
        syncedAmmo = currentWeapon.maxAmmo;
        // ammoText.text = syncedAmmo.ToString(); // Removed direct assignment
    }

    [Command(requiresAuthority = true)]
    private void Recoil()
    {
        RpcApplyRecoil();
    }

    [ClientRpc]
    private void RpcApplyRecoil()
    {
        if (!isOwned)
        {
            return;
        }
        recoil.ApplyRecoil(); // Call from Shoot() instead
    }

    [ClientRpc]
    void RpcDoShootEffect()
    {
        Debug.Log("RpcDoShootEffect called — playing muzzle flash.");
        WeaponManager.GetcurrentGraphics().muzzleFlash.Play();
    }

    [Command]
    public void CmdOnHit(Vector3 _pos, Vector3 _normal)
    {
        RpcDoHitEffect(_pos, _normal);
    }

    [ClientRpc]
    void RpcDoHitEffect(Vector3 _pos, Vector3 _normal)
    {
        GameObject _hitEffect = Instantiate(
            WeaponManager.GetcurrentGraphics().hitEffectPrefab,
            _pos,
            Quaternion.LookRotation(_normal)
        );
        GameObject _hitSparks = Instantiate(
            WeaponManager.GetcurrentGraphics().HitSparks,
            _pos,
            Quaternion.LookRotation(_normal)
        );

        Destroy(_hitEffect, 2f);
        Destroy(_hitSparks, 1f);
        // _hitSparks.Stop();
    }

    [Command]
    public void CmdOnShootTrailEffect()
    {
        Debug.Log("CmdOnShoot called on server!");
        RpcDoShootEffect();
        gunSoundManager.CmdPlayGunSound(WeaponManager.GunNameForSoundFX());
        //  syncedAmmo -= 1;
        // ammoText.text = syncedAmmo.ToString(); // Removed direct assignment
        gunSelector = GetComponent<PlayerGunSelector>();
        if (gunSelector == null || gunSelector.ActiveGun == null)
        {
            Debug.LogError("CmdOnShoot: gunSelector or ActiveGun is NULL!");
            return;
        }
        gunSelector.ActiveGun.CmdShootTrailEffect();
    }

    [Command]
    public void CmdOnShootShotgunTrailEffect()
    {
        Debug.Log("CmdOnShootShotgun called on server!");

        // Play Recoil Animation on all clients
        RpcShotGunRecoilAnimation();
        // currentWeapon.currentAmmo -= 1;
        // ammoText.text = currentWeapon.currentAmmo.ToString();
        // Trigger other effects, such as gun sound
        gunSoundManager.CmdPlayGunSound(WeaponManager.GunNameForSoundFX());

        // Check for any issues with gunSelector
        gunSelector = GetComponent<PlayerGunSelector>();
        if (gunSelector == null || gunSelector.ActiveGun == null)
        {
            Debug.LogError("CmdOnShootShotgun: gunSelector or ActiveGun is NULL!");
            return;
        }

        // Call the method to handle actual shooting
        gunSelector.ActiveGun.CmdShootShotgunTrailEffect();
    }

    // ===========================
    // New authoritative entrypoint:
    // Client calls this command and server resolves hits.
    // ===========================
    [Command]
    public void CmdRequestShoot(Vector3 origin, Vector3 forward, bool isShotgun)
    {
        // This runs on the server. We perform raycasts and process hits here.
        if (!isServer)
        {
            Debug.LogError("CmdRequestShoot should run on server.");
            return;
        }

        // Basic server-side cooldown / ammo check (more robust checks can be added)
        if (syncedAmmo <= 0)
        {
            // Optionally instruct the client to reload
            return;
        }

        // Update ammo server-side
        syncedAmmo -= 1;

        // Play effects on clients
        RpcDoShootEffect();
        gunSoundManager.CmdPlayGunSound(WeaponManager.GunNameForSoundFX());

        // Recoil animation RPC - pick by weapon name
        PlayRecoilAnimationServerRpc();

        // If shotgun, run shotgun logic
        if (isShotgun)
        {
            ServerProcessShotgun(origin, forward);
            CmdOnShootShotgunTrailEffect();
            return;
        }

        // Non-shotgun server raycast
        ServerProcessHits(origin, forward);
        CmdOnShootTrailEffect();
    }

    // helper rpc to centralize recoil picking (keeps old RPC names intact)
    private void PlayRecoilAnimationServerRpc()
    {
        if (currentWeapon == null)
            return;

        if (currentWeapon.GunName == "Sniper")
        {
            RpcSniperRecoilAnimation();
        }
        else if (currentWeapon.GunName == "Pistol")
        {
            RpcPistolRecoilAnimation();
        }
    }

    // Server-side processing of a single-shot / hitscan weapon
    public void ServerProcessHits(Vector3 origin, Vector3 forward)
    {
        // Apply spread on server using currentWeapon or ActualBulletSpread
        Vector3 spread = ActualBulletSpread;
        Vector3 shootDirection =
            forward
            + new Vector3(
                UnityEngine.Random.Range(-spread.x, spread.x),
                UnityEngine.Random.Range(-spread.y, spread.y),
                UnityEngine.Random.Range(-spread.z, spread.z)
            );
        shootDirection.Normalize();

        int layerMask = ~LayerMask.GetMask("Ignore Raycast"); // Adjust if needed
        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            shootDirection,
            currentWeapon.range,
            layerMask
        );
        hits = hits.OrderBy(h => h.distance).ToArray();

        foreach (RaycastHit hit in hits)
        {
            // Keep hitInfo set server-side so other server-side logic can use it if needed
            hitInfo = hit;

            if (hit.collider.CompareTag("Shield"))
            {
                SpawnShieldRipples ripples = hit.collider.GetComponent<SpawnShieldRipples>();
                ripples?.TriggerRippleEffect(hit.point, hit.normal);
                RpcDoHitEffect(hit.point, hit.normal);
                // stop processing further hits for this shot
                return;
            }

            player hitPlayer = hit.collider.GetComponentInParent<player>();
            if (
                hitPlayer != null
                && hitPlayer != this
                && (hit.collider.CompareTag(PLAYER_TAG) || hit.collider.CompareTag("Ragdoll"))
            )
            {
                // call the server-side internal processor (previously inside CmdPlayerShot)
                ServerPlayerShotInternal(
                    hit.collider.name,
                    currentWeapon.damage,
                    GetComponent<NetworkIdentity>(),
                    hit.point,
                    hit.normal,
                    hit.collider.name
                );
            }

            if (hit.collider.CompareTag("Turret"))
            {
                Turret turret = hit.collider.GetComponent<Turret>();
                turret?.CmdTakeDamage(1f);
            }

            if (hit.collider.CompareTag("ReflectBulletShield"))
            {
                ReflectBulletShield reflectBullet =
                    hit.collider.GetComponent<ReflectBulletShield>();
                reflectBullet?.CmdReflectBullet(
                    hit.point,
                    shootDirection,
                    gameObject,
                    GetComponent<NetworkIdentity>()
                );
                return;
            }

            RpcDoHitEffect(hit.point, hit.normal);
        }
    }

    // Server-side shotgun processing (preserves pellets/spread/turret/reflect behavior)
    public void ServerProcessShotgun(Vector3 origin, Vector3 forward)
    {
        // Play shotgun recoil on clients
        RpcShotGunRecoilAnimation();

        int pellets = 8;
        float spreadAmount = 0.08f;

        for (int i = 0; i < pellets; i++)
        {
            Vector3 spreadDirection =
                forward
                + new Vector3(
                    UnityEngine.Random.Range(-spreadAmount, spreadAmount),
                    UnityEngine.Random.Range(-spreadAmount, spreadAmount),
                    UnityEngine.Random.Range(-spreadAmount, spreadAmount)
                );
            spreadDirection.Normalize();

            int layerMask = ~LayerMask.GetMask("Ignore Raycast");
            RaycastHit[] hits = Physics.RaycastAll(
                origin,
                spreadDirection,
                currentWeapon.range,
                layerMask
            );
            hits = hits.OrderBy(h => h.distance).ToArray();

            foreach (RaycastHit hit in hits)
            {
                // set server-side hitinfo
                hitInfo = hit;

                if (hit.collider.CompareTag("ReflectBulletShield"))
                {
                    var reflectBullet = hit.collider.GetComponent<ReflectBulletShield>();
                    reflectBullet?.CmdReflectBullet(
                        hit.point,
                        spreadDirection,
                        gameObject,
                        GetComponent<NetworkIdentity>()
                    );
                    break; // pellet done
                }

                if (hit.collider.CompareTag("Shield"))
                {
                    var ripples = hit.collider.GetComponent<SpawnShieldRipples>();
                    ripples?.TriggerRippleEffect(hit.point, hit.normal);
                    RpcDoHitEffect(hit.point, hit.normal);
                    break;
                }

                var hitPlayer = hit.collider.GetComponentInParent<player>();
                if (hitPlayer != null && hitPlayer != this)
                {
                    ServerPlayerShotInternal(
                        hit.collider.name,
                        currentWeapon.damage,
                        GetComponent<NetworkIdentity>(),
                        hit.point,
                        hit.normal,
                        hit.collider.name
                    );
                    RpcDoHitEffect(hit.point, hit.normal);
                    break;
                }

                if (hit.collider.CompareTag("Turret"))
                {
                    var turret = hit.collider.GetComponent<Turret>();
                    turret?.CmdTakeDamage(30f);
                    break;
                }

                RpcDoHitEffect(hit.point, hit.normal);
            }
        }
    }

    // Preserve the original CmdPlayerShot Command signature for compatibility,
    // but internally delegate to a server-side method that both the Command and the new path can use.
    [Command]
    public void CmdPlayerShot(
        string _playerID,
        int _damage,
        NetworkIdentity attackerIdentity,
        Vector3 hitPoint,
        Vector3 hitDirection,
        string hitBodyPartName
    )
    {
        // The old behavior expected this to run on server (and it will when client calls it),
        // so we route it to the internal server handler to avoid duplicated logic.
        if (!isServer)
        {
            Debug.LogError("CmdTakeDamage called on client! This should be on server.");
            return;
        }

        ServerPlayerShotInternal(
            _playerID,
            _damage,
            attackerIdentity,
            hitPoint,
            hitDirection,
            hitBodyPartName
        );
    }

    // This method contains the server-side logic previously in CmdPlayerShot.
    private void ServerPlayerShotInternal(
        string _playerID,
        int _damage,
        NetworkIdentity attackerIdentity,
        Vector3 hitPoint,
        Vector3 hitDirection,
        string hitBodyPartName
    )
    {
        if (!isServer)
        {
            Debug.LogError("ServerPlayerShotInternal must be executed on server.");
            return;
        }

        Debug.Log(_playerID + " has been shot (server processed)");

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

        // Apply damage (server authoritative)
        _player.RpctakeDamageP(_damage, attackerIdentity, hitPoint, hitDirection, hitBodyPartName);

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

    IEnumerator WaitForNetworkReady()
    {
        while (!NetworkClient.ready)
        {
            Debug.Log("Waiting for network to be ready...");
            yield return null;
        }
    }

    public void Shoot()
    {
        Debug.Log("Entered Shoot");
        if (!isOwned)
            return;

        currentWeapon = WeaponManager.GetcurrentWeapon();
        NetworkIdentity myID = GetComponent<NetworkIdentity>();

        if (currentWeapon == null)
        {
            Debug.LogError("Shoot: No weapon selected!");
            return;
        }

        if (!NetworkClient.ready)
        {
            Debug.LogWarning("Client is not ready. Cannot shoot yet!");
            return;
        }

        if (WeaponManager == null)
        {
            WeaponManager = GetComponent<weaponManager>();
            Debug.LogError("WeaponManager is null in playerShoot script!");
            return;
        }

        GunsEffect gunEffect = GetComponent<GunsEffect>();
        if (gunEffect != null && gunEffect.type == guns.Shotgun)
        {
            // Keep public method name intact but use the new authoritative flow:
            ShootShotgun(); // This will call server via CmdRequestShoot
            return;
        }
        if (syncedAmmo <= 0)
        {
            CancelInvoke("Recoil");
            return;
        }

        if (isServer)
        {
            PlayRecoilAnimation();
        }

        if (CameraShake.instance != null && isLocalPlayer)
        {
            CameraShake.instance.ShakeOnShoot();
        }

        StartCoroutine(WaitForNetworkReady());

        if (isLocalPlayer && ammoText != null)
            ammoText.text = (syncedAmmo - 1).ToString(); // show predicted ammo locally

        // Previously we called CmdOnShoot() directly here from client.
        // Now we route to the server authoritative request that resolves hits.
        // Keep old CmdOnShoot() available for compatibility; it remains callable.
        // We'll call our new command which performs the raycast and damage on server.
        CmdRequestShoot(cam.transform.position, cam.transform.forward, false);
    }

    private void PlayRecoilAnimation()
    {
        if (currentWeapon.GunName == "Sniper" && isOwned)
        {
            RpcSniperRecoilAnimation();
        }

        if (currentWeapon.GunName == "Pistol" && isOwned)
        {
            RpcPistolRecoilAnimation();
        }
    }

    private void ShootShotgun()
    {
        Debug.Log("Entered ShootShotgun");

        if (!canShoot)
            return; // Prevent shooting if on cooldown

        // Keep behavior: start cooldown locally
        canShoot = false;
        StartCoroutine(ShotgunCooldown(0.85f));

        // trigger the server authoritative shotgun path
        // This will decrement server ammo and process pellets there.
        CmdRequestShoot(cam.transform.position, cam.transform.forward, true);

        // Note: We still call CmdOnShootShotgun for compatibility if other scripts expect it.
        // It's safe to call, but authoritative ammo/damage is done in CmdRequestShoot above.
        // Keep call commented or uncomment depending on whether you want the old behavior executed as well.
        // CmdOnShootShotgun();
    }

    [ClientRpc]
    private void RpcShotGunRecoilAnimation()
    {
        if (ShotgunRecoil != null)
        {
            ShotgunRecoil.Play("NewShotGunRecoil", 0, 0f); // Ensure this is the correct animation state
            Debug.Log("🎯 Recoil animation triggered on client!");
        }
        else
        {
            Debug.LogWarning("🚫 ShotgunRecoil animator not found on client!");
        }
    }

    [ClientRpc]
    private void RpcSniperRecoilAnimation()
    {
        if (SniperRecoil != null)
        {
            SniperRecoil.Play("SniperRifleAnimation", 0, 0f); // Ensure this is the correct animation state
            Debug.Log("🎯 Recoil animation triggered on client!");
        }
        else
        {
            Debug.LogWarning("🚫 ShotgunRecoil animator not found on client!");
        }
    }

    [ClientRpc]
    private void RpcPistolRecoilAnimation()
    {
        if (PistolRecoil != null)
        {
            PistolRecoil.Play("pistolAnimation", 0, 0f); // Ensure this is the correct animation state
            Debug.Log("🎯 Recoil animation triggered on client!");
        }
        else
        {
            Debug.LogWarning("🚫 pistolGun animator not found on client!");
        }
    }

    private IEnumerator ShotgunCooldown(float delay)
    {
        yield return new WaitForSeconds(delay);
        //ShotgunRecoil.SetBool("Shoot 0", false);
        canShoot = true;
    }
}
