using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

//using UnityEditor.Rendering;

public class Turret : NetworkBehaviour
{
    [SyncVar]
    public string TurretTeam;
    public Transform RotatableBase;

    //  public Transform RotatableBase;  // Y-axis rotation
    public Transform LookUpDownBase; // X-axis rotation
    public float range = 20f;
    public float rotationSpeed = 5f;
    public float fireRate = 1f;
    public Transform firePoint;
    public Transform muzzleFlashSpawnPoint;
    public ParticleSystem muzzleFlashEffect;
    public GameObject bulletImpactPrefab;
    public GameObject deathEffectPrefab;
    public int turretDamage = 5;

    [SyncVar(hook = nameof(OnHealthChanged))]
    public float turretHealth;
    public float aimHeightOffset;

    public Image healthBarFill;

    private Transform target;
    private float nextFireTime;

    public AudioSource rotateSound;
    public AudioSource fireSound;
    public AudioSource explosionSound;
    public AudioSource LightningSound;
    public bool isDestroyed;
    private bool isRotating;
    private NetworkIdentity attackerId;

    [SyncVar(hook = nameof(OnRotationUpdated))]
    Quaternion syncedRotation;

    // public Transform destroyedRotationPoint; // Assign this in the Inspector

    [SyncVar(hook = nameof(OnRotationUpdated))]
    Quaternion syncedRotationY;

    [SyncVar(hook = nameof(OnLookRotationUpdated))]
    Quaternion syncedRotationX;

    public float maxHealthTurret = 1000f;

    public GameObject sparksEffectPrefab;
    public GameObject lightningEffectPrefab;
    private FlagHandler playerTeam;

    void Start()
    {
        Debug.Log("Turret spawned on " + (isServer ? "SERVER" : "CLIENT"));
        if (isClient)
            UpdateHealthUI(turretHealth);
        Debug.Log("Turret Team :" + TurretTeam);
    }

    void Update()
    {
        // RotatableBase.localScale = new Vector3(5,5,5);
        if (!isServer || isDestroyed)
            return;

        FindTarget();
        HandleSounds();

        if (target != null)
        {
            RotateTurret(target.transform);

            if (Time.time >= nextFireTime && CanShootTarget())
            {
                Fire();
                nextFireTime = Time.time + 1f / fireRate;
            }
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdTakeDamage(float amount)
    {
        if (!isServer || turretHealth <= 0)
            return;

        turretHealth -= amount;
        Debug.Log("Turret took damage! Current health: " + turretHealth);

        if (turretHealth <= 0)
        {
            StartCoroutine(DestroyTurret());
        }
    }

    // 🔹 SyncVar Hook: Updates the health bar fill amount on clients
    void OnHealthChanged(float oldHealth, float newHealth)
    {
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = (float)newHealth / maxHealthTurret;
        }
    }

    [Client]
    void UpdateHealthUI(float newHealth)
    {
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = Mathf.Clamp01((float)newHealth / 100f);
        }
    }

    [Server]
    IEnumerator DestroyTurret()
    {
        Debug.Log("Turret Destroyed!");
        isDestroyed = true;

        // Stop rotation sound if turret is destroyed
        if (isRotating)
        {
            isRotating = false;
            rotateSound?.Stop();
        }

        Quaternion targetLookRotation = Quaternion.Euler(
            -30f,
            LookUpDownBase.rotation.eulerAngles.y,
            0f
        );
        float elapsedTime = 0f;
        float rotationTime = 1.5f;

        while (elapsedTime < rotationTime)
        {
            LookUpDownBase.rotation = Quaternion.Slerp(
                LookUpDownBase.rotation,
                targetLookRotation,
                elapsedTime / rotationTime
            );
            RpcSyncLookRotation(LookUpDownBase.rotation);
            syncedRotationX = LookUpDownBase.rotation; // Sync rotation
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        SpawnDestructionEffects();
        RpcPlayExplosionSound();

        yield return new WaitForSeconds(10f);
        NetworkServer.Destroy(gameObject);
    }

    [ClientRpc]
    void RpcSyncLookRotation(Quaternion newRotation)
    {
        if (!isServer)
            LookUpDownBase.rotation = newRotation;
    }

    // 🔹 Syncs destruction rotation for all clients
    [ClientRpc]
    void RpcSyncDestructionRotation(Quaternion newRotation)
    {
        if (!isServer) // Only clients should apply this
        {
            RotatableBase.rotation = newRotation;
        }
    }

    void SpawnDestructionEffects()
    {
        if (deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                deathEffectPrefab,
                transform.position,
                Quaternion.identity
            );
            NetworkServer.Spawn(effect);
            Destroy(effect, 10f);
        }

        if (sparksEffectPrefab != null)
        {
            GameObject sparks = Instantiate(
                sparksEffectPrefab,
                transform.position + Vector3.up * 2f,
                Quaternion.identity
            );
            NetworkServer.Spawn(sparks);
            Destroy(sparks, 5f);
        }

        if (lightningEffectPrefab != null)
        {
            GameObject lightning = Instantiate(
                lightningEffectPrefab,
                transform.position + Vector3.up * 2f,
                Quaternion.identity
            );
            NetworkServer.Spawn(lightning);
            Destroy(lightning, 5f);
        }
    }

    [ClientRpc]
    void RpcPlayExplosionSound()
    {
        explosionSound?.Play();
        LightningSound?.Play();
    }

    void FindTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, range);
        Transform closestTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            playerTeam = hit.GetComponent<FlagHandler>();
            if (
                hit.transform == transform
                || !hit.CompareTag("Player")
                || TurretTeam == playerTeam.Team
            )
                continue;

            float distance = Vector3.Distance(transform.position, hit.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = hit.transform;
            }
        }

        target = closestTarget;
    }

    void RotateTurret(Transform target)
    {
        FlagHandler teamVar = target.GetComponent<FlagHandler>();
        if (turretHealth <= 0 || teamVar.Team == TurretTeam)
            return;

        //**Step 1: Adjust Target Position (Aim at Upper Body)**
        Vector3 targetPosition = target.position + Vector3.up * aimHeightOffset; // Adjust height (1.5f = chest/head level)

        // **Step 2: Rotate RotatableBase (Y-Axis Only)**
        Vector3 directionY = target.position - RotatableBase.position;
        Quaternion lookRotationY = Quaternion.LookRotation(directionY);
        lookRotationY = Quaternion.Euler(0f, lookRotationY.eulerAngles.y, 0f); // Lock X & Z rotations
        RotatableBase.rotation = Quaternion.Slerp(
            RotatableBase.rotation,
            lookRotationY,
            rotationSpeed * Time.deltaTime
        );
        syncedRotationY = RotatableBase.rotation;

        // **Step 3: Rotate LookUpDownBase (X-Axis Only)**
        Vector3 directionX = targetPosition - LookUpDownBase.position;
        Quaternion lookRotationX = Quaternion.LookRotation(directionX);

        // Preserve RotatableBase's Y-axis while modifying only X-axis
        lookRotationX = Quaternion.Euler(
            lookRotationX.eulerAngles.x,
            RotatableBase.eulerAngles.y,
            0f
        );
        LookUpDownBase.rotation = Quaternion.Slerp(
            LookUpDownBase.rotation,
            lookRotationX,
            rotationSpeed * Time.deltaTime
        );
        syncedRotationX = LookUpDownBase.rotation;

        // **Handle Rotation Sound**
        if (
            Quaternion.Angle(RotatableBase.rotation, lookRotationY) > 1f
            || Quaternion.Angle(LookUpDownBase.rotation, lookRotationX) > 1f
        )
        {
            if (!isRotating)
            {
                isRotating = true;
                rotateSound?.Play();
            }
        }
        else
        {
            isRotating = false;
            rotateSound?.Stop();
        }
    }

    // Hook function - Called when 'syncedRotation' changes on clients
    void OnRotationUpdated(Quaternion oldRotation, Quaternion newRotation)
    {
        if (!isServer)
        {
            StartCoroutine(SmoothRotateTo(newRotation));
        }
    }

    //  Hook function for syncedRotationX (vertical rotation)
    void OnLookRotationUpdated(Quaternion oldRotation, Quaternion newRotation)
    {
        if (!isServer)
        {
            LookUpDownBase.rotation = newRotation;
        }
    }

    // Coroutine for smoother interpolation on clients
    IEnumerator SmoothRotateTo(Quaternion targetRotation)
    {
        float elapsedTime = 0f;
        float duration = 0.2f; // Smooth over 0.2 seconds
        Quaternion startRotation = RotatableBase.rotation;

        while (elapsedTime < duration)
        {
            RotatableBase.rotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                elapsedTime / duration
            );
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        RotatableBase.rotation = targetRotation; // Ensure final position is correct
    }

    bool CanShootTarget()
    {
        if (target == null)
            return false;

        Vector3 direction = (target.position - firePoint.position).normalized;
        float dotProduct = Vector3.Dot(RotatableBase.forward, direction);
        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        return dotProduct > 0.9f && distanceToTarget <= range;
    }

    void Fire()
    {
        FlagHandler TeamVar = target.GetComponent<FlagHandler>();
        if (firePoint == null || target == null || turretHealth <= 0 || TeamVar.Team == TurretTeam)
            return;

        RpcPlayFireSound();
        RpcPlayMuzzleFlashEffect(); // Ensure muzzle flash is visible to all clients

        if (Physics.Raycast(firePoint.position, firePoint.forward, out RaycastHit hit, range))
        {
            HandleBulletHit(hit.collider, hit.point);
        }
    }

    [ClientRpc]
    void RpcPlayFireSound()
    {
        if (!fireSound.isPlaying)
            fireSound.Play();
    }

    [ClientRpc]
    void RpcPlayMuzzleFlashEffect()
    {
        if (muzzleFlashEffect != null && muzzleFlashSpawnPoint != null)
        {
            ParticleSystem flash = Instantiate(
                muzzleFlashEffect,
                muzzleFlashSpawnPoint.position,
                muzzleFlashSpawnPoint.rotation
            );
            flash.Play();
            Destroy(flash.gameObject, 1f);
        }
    }

    public void HandleBulletHit(Collider hitCollider, Vector3 hitPoint)
    {
        if (!isServer)
            return;

        if (hitCollider.CompareTag("Shield"))
        {
            hitCollider
                .GetComponent<SpawnShieldRipples>()
                ?.TriggerRippleEffect(
                    hitCollider.transform.position,
                    hitCollider.transform.forward
                );
            return;
        }

        NetworkIdentity hitIdentity = hitCollider.GetComponentInParent<NetworkIdentity>();
        if (hitIdentity != null)
        {
            player hitPlayer = hitIdentity.GetComponent<player>();
            if (hitPlayer != null)
            {
                DreyarShield shield = hitPlayer.GetComponent<DreyarShield>();
                if (shield == null || !shield.IsShieldActive)
                {
                    hitPlayer.RpctakeDamageU(turretDamage, attackerId);
                }
            }
        }

        InstantiateBulletImpact(hitPoint);
    }

    void InstantiateBulletImpact(Vector3 hitPoint)
    {
        if (bulletImpactPrefab != null)
        {
            GameObject impact = Instantiate(bulletImpactPrefab, hitPoint, Quaternion.identity);
            Destroy(impact, 2f);
        }
    }

    void HandleSounds()
    {
        bool hasTarget = target != null;
        if (hasTarget && !rotateSound.isPlaying)
            RpcPlayRotateSound();
        if (!hasTarget)
            RpcStopRotateSound();
    }

    [ClientRpc]
    void RpcPlayRotateSound()
    {
        if (!rotateSound.isPlaying)
            rotateSound.Play();
    }

    [ClientRpc]
    void RpcStopRotateSound()
    {
        if (rotateSound.isPlaying)
            rotateSound.Stop();
    }

    void OnDrawGizmos()
    {
        if (firePoint == null || target == null)
            return;

        Vector3 aimTarget = target.position + Vector3.up * 2f; // Check where it's aiming

        Gizmos.color = Color.red;
        Gizmos.DrawLine(firePoint.position, aimTarget); // Red line shows aim direction

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(aimTarget, 0.2f); // Green sphere shows exact aim position
    }
}
