using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.Pool;

public class ReflectBulletShield : NetworkBehaviour
{
    public float reflectionChance = 1f; // Always reflect
    public float missPlayerChance = 0.4f; // 40% chance to miss the player
    public int maxBounces = 5;
    public float bounceForce = 10f;
    public int bulletDamage = 10;
    public TrailConfigScriptableObject TrailConfig;

    private ObjectPool<TrailRenderer> TrailPool;

    private void Start()
    {
        TrailPool = new ObjectPool<TrailRenderer>(
            CreateTrail,
            OnTakeTrail,
            OnReturnTrail,
            OnDestroyTrail,
            false,
            10,
            100
        );
    }

    [Command(requiresAuthority = false)]
    public void CmdReflectBullet(
        Vector3 hitPoint,
        Vector3 bulletDirection,
        GameObject Shooter,
        NetworkIdentity attacker
    )
    {
        if (Random.value > reflectionChance)
        {
            Debug.Log("<color=yellow>[REFLECT]</color> Bullet was NOT reflected.");
            return;
        }

        Debug.Log("<color=green>[REFLECT]</color> Bullet reflected! Starting bounce process.");
        BounceBullet(hitPoint, bulletDirection, maxBounces, Shooter, attacker);
    }

    private void BounceBullet(
        Vector3 startPoint,
        Vector3 direction,
        int bouncesRemaining,
        GameObject shooter,
        NetworkIdentity attacker
    )
    {
        if (bouncesRemaining <= 0)
            return; // Stop recursion if max bounces are reached

        RaycastHit hit;
        if (Physics.Raycast(startPoint, direction, out hit))
        {
            Debug.Log(
                $"<color=blue>[BOUNCE]</color> Bullet hit: {hit.collider.name} at {hit.point}"
            );

            RpcPlayReflectionEffect(startPoint, hit.point); // Ensure trail is visible at every bounce

            if (hit.collider.CompareTag("ReflectBulletShield"))
            {
                // Get the shield holder (player who owns the shield)
                GameObject shieldHolder = hit.collider.transform.parent?.gameObject;

                // Find the best target, ignoring the shield holder (unless it's the shooter)
                Transform target = FindBestTarget(hit.point, shooter, shieldHolder);

                if (target != null && Random.value > missPlayerChance)
                {
                    // Get a random height offset to avoid always aiming at the same spot
                    float randomHeightOffset = Random.Range(0.5f, 1.8f); // Adjust to match player height range
                    Vector3 targetPoint = target.position + Vector3.up * randomHeightOffset;

                    // Calculate new direction to this randomized point
                    Vector3 newDirection = (targetPoint - hit.point).normalized;

                    BounceBullet(hit.point, newDirection, bouncesRemaining - 1, shooter, attacker);
                    return;
                }
            }

            if (hit.collider.CompareTag("Player"))
            {
                player enemy = hit.collider.GetComponent<player>();
                if (enemy != null)
                {
                    ApplyDamage(enemy.gameObject, bulletDamage, attacker);
                    Debug.Log(
                        $"<color=red>[HIT]</color> Bullet hit player {enemy.name}. Dealing {bulletDamage} damage."
                    );
                }
                return; // Stop bouncing when hitting a player
            }

            // 🔥 **NEW: Always Reflect, No Matter What** 🔥
            Vector3 reflectedDirection = Vector3.Reflect(direction, hit.normal);

            // Add slight randomness to make reflections look more dynamic
            reflectedDirection += new Vector3(
                Random.Range(-0.1f, 0.1f),
                Random.Range(-0.1f, 0.1f),
                Random.Range(-0.1f, 0.1f)
            );

            // Ensure the new direction is normalized before bouncing again
            BounceBullet(
                hit.point,
                reflectedDirection.normalized,
                bouncesRemaining - 1,
                shooter,
                attacker
            );
        }
    }

    /// <summary>
    /// Finds the best target near the impact point (either the shooter or their nearby teammates).
    /// </summary>
    private Transform FindBestTarget(Vector3 hitPoint, GameObject shooter, GameObject shieldHolder)
    {
        Collider[] potentialTargets = Physics.OverlapSphere(hitPoint, 20f); // Get all colliders in a 20m radius

        Transform bestTarget = null;
        float bestDist = float.MaxValue;

        foreach (Collider col in potentialTargets)
        {
            if (!col.CompareTag("Player"))
                continue; // Ignore everything except players

            GameObject playerObj = col.gameObject;

            // Ignore the shield holder unless they are the shooter
            if (playerObj == shieldHolder && playerObj != shooter)
            {
                continue;
            }

            // Prioritize hitting the shooter if they are nearby
            if (playerObj == shooter)
            {
                return playerObj.transform;
            }

            // Find the closest valid player
            float dist = Vector3.Distance(hitPoint, playerObj.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestTarget = playerObj.transform;
            }
        }

        return bestTarget; // Returns shooter if found, otherwise the closest valid player
    }

    private void ApplyDamage(GameObject enemy, int damage, NetworkIdentity attacker)
    {
        if (enemy != null)
        {
            enemy.GetComponent<player>().RpctakeDamageU(damage, attacker);
            Debug.Log($"<color=red>[DAMAGE]</color> Applied {damage} damage to {enemy.name}");
        }
    }

    [ClientRpc]
    private void RpcPlayReflectionEffect(Vector3 startPoint, Vector3 endPoint)
    {
        Debug.Log($"<color=yellow>[TRAIL]</color> Spawning trail from {startPoint} to {endPoint}");
        StartCoroutine(PlayTrail(startPoint, endPoint));
    }

    private IEnumerator PlayTrail(Vector3 startPoint, Vector3 endPoint)
    {
        TrailRenderer instance = TrailPool.Get();
        instance.gameObject.SetActive(true);
        instance.transform.position = startPoint;
        instance.emitting = true;

        float distance = Vector3.Distance(startPoint, endPoint);
        float elapsedTime = 0f;

        Debug.Log($"<color=yellow>[TRAIL]</color> Moving trail over {distance} units");

        while (elapsedTime < TrailConfig.Duration)
        {
            instance.transform.position = Vector3.Lerp(
                startPoint,
                endPoint,
                elapsedTime / TrailConfig.Duration
            );
            elapsedTime += Time.deltaTime * TrailConfig.SimulationSpeed;
            yield return null;
        }

        instance.transform.position = endPoint;
        yield return new WaitForSeconds(TrailConfig.Duration);
        instance.emitting = false;
        instance.gameObject.SetActive(false);
        TrailPool.Release(instance);

        Debug.Log("<color=green>[TRAIL]</color> Trail completed and returned to pool.");
    }

    private TrailRenderer CreateTrail()
    {
        GameObject instance = new GameObject("Bullet Trail");
        TrailRenderer trail = instance.AddComponent<TrailRenderer>();

        trail.colorGradient = TrailConfig.Color;
        trail.material = TrailConfig.material;

        // Adjusted width curve (thinner than before)
        AnimationCurve widthCurve = new AnimationCurve();
        widthCurve.AddKey(0f, 0.2f); // Start at 0.6 (was 1.2)
        widthCurve.AddKey(0.5f, 0.1f); // Midway, slightly thinner
        widthCurve.AddKey(1f, 0f); // End fades out

        trail.widthCurve = widthCurve;
        trail.time = TrailConfig.Duration;
        trail.minVertexDistance = TrailConfig.MinVertexDistance;
        trail.emitting = false;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        return trail;
    }

    void SetTrailRenderingMode(Material material, int mode)
    {
        switch (mode)
        {
            case 0: // Opaque
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
                break;

            case 1: // Transparent
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                break;

            case 2: // Additive (Neon glow)
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_ZWrite", 0);
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                break;
        }
    }

    private void OnTakeTrail(TrailRenderer trail) => trail.gameObject.SetActive(true);

    private void OnReturnTrail(TrailRenderer trail) => trail.gameObject.SetActive(false);

    private void OnDestroyTrail(TrailRenderer trail) => Destroy(trail.gameObject);
}
