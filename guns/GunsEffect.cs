using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.Pool;

public class GunsEffect : NetworkBehaviour
{
    public guns type;
    public GameObject ModelPrefab;
    public Vector3 SpawnPoint;
    public Vector3 SpawnRotation;

    public ShootConfigScriptableObject ShootConfig;
    public TrailConfigScriptableObject TrailConfig;
    public Transform gunholderfff;
    private MonoBehaviour ActiveMonoBehaviour;

    private GameObject Model;
    private float LastShootTime;
    private ParticleSystem ShootSystem;
    private ObjectPool<TrailRenderer> TrailPool;
    public int Pellets = 100;
    public float ShotgunSpreadAngle = 0f; // tweak this to control scatter

    [Command]
    public void CmdSpawnGun(NetworkIdentity PlayerIdentity)
    {
        if (PlayerIdentity == null)
        {
            Debug.LogError(
                "CmdSpawnGun: PlayerIdentity is NULL! Make sure you are passing the correct NetworkIdentity."
            );
            return;
        }

        RpcSpawnGun(PlayerIdentity);
    }

    [ClientRpc]
    private void RpcSpawnGun(NetworkIdentity playerIdentity)
    {
        if (playerIdentity == null)
        {
            Debug.LogError("PlayerIdentity is NULL!");
            return;
        }

        // Find the GunHolder inside the player



        if (gunholderfff == null)
        {
            Debug.LogError("GunHolder Transform NOT FOUND!");
            return;
        }

        // Set the ActiveMonoBehaviour
        ActiveMonoBehaviour = playerIdentity.GetComponent<PlayerGunSelector>();

        // Spawn the gun model
        Model = Instantiate(ModelPrefab);
        Model.transform.SetParent(gunholderfff, false);
        Model.transform.localPosition = SpawnPoint;
        Model.transform.localRotation = Quaternion.Euler(SpawnRotation);

        // Assign the shooting system
        ShootSystem = Model.GetComponentInChildren<ParticleSystem>();
    }

    [Command(requiresAuthority = false)]
    public void CmdShootTrailEffect()
    {
        if (this == null)
        {
            Debug.LogError("CmdShoot() called on a null object!");
            return;
        }

        if (Time.time > ShootConfig.FireRate + LastShootTime)
        {
            LastShootTime = Time.time;
            Vector3 shootDirection =
                ShootSystem.transform.forward
                + new Vector3(
                    Random.Range(-ShootConfig.spread.x, ShootConfig.spread.x),
                    Random.Range(-ShootConfig.spread.y, ShootConfig.spread.y),
                    Random.Range(-ShootConfig.spread.z, ShootConfig.spread.z)
                );
            shootDirection.Normalize();

            if (
                Physics.Raycast(
                    ShootSystem.transform.position,
                    shootDirection,
                    out RaycastHit hit,
                    float.MaxValue,
                    ShootConfig.hitMask
                )
            )
            {
                RpcPlayShootEffect(ShootSystem.transform.position, hit.point);
            }
            else
            {
                RpcPlayShootEffect(
                    ShootSystem.transform.position,
                    ShootSystem.transform.position + (shootDirection * TrailConfig.MissDistance)
                );
            }
        }
    }

    [ClientRpc]
    private void RpcPlayShootEffect(Vector3 startPoint, Vector3 endPoint)
    {
        if (ShootSystem != null)
            ShootSystem.Play();
        StartCoroutine(PlayTrail(startPoint, endPoint));
        //  Debug.Log("🔫 AI SHOOT EFFECT TRIGGERED");
    }

    private IEnumerator PlayTrail(Vector3 startPoint, Vector3 endPoint)
    {
        if (TrailPool == null)
            TrailPool = new ObjectPool<TrailRenderer>(CreateTrail);
        TrailRenderer instance = TrailPool.Get();
        instance.gameObject.SetActive(true);
        instance.transform.position = startPoint;
        yield return null;

        instance.emitting = true;
        float distance = Vector3.Distance(startPoint, endPoint);
        float remainingDistance = distance;
        while (remainingDistance > 0)
        {
            instance.transform.position = Vector3.Lerp(
                startPoint,
                endPoint,
                Mathf.Clamp01(1 - (remainingDistance / distance))
            );
            remainingDistance -= TrailConfig.SimulationSpeed * Time.deltaTime;
            yield return null;
        }

        instance.transform.position = endPoint;
        yield return new WaitForSeconds(TrailConfig.Duration);
        instance.emitting = false;
        instance.gameObject.SetActive(false);
        TrailPool.Release(instance);
    }

    [Command(requiresAuthority = false)]
    public void CmdShootShotgunTrailEffect()
    {
        if (this == null)
        {
            Debug.LogError("CmdShootShotgun() called on a null object!");
            return;
        }

        if (Time.time > ShootConfig.FireRate + LastShootTime)
        {
            LastShootTime = Time.time;

            int pelletCount = Pellets; // Assuming you’ve defined this in your ShootConfig
            float spreadAngle = ShotgunSpreadAngle;

            for (int i = 0; i < pelletCount; i++)
            {
                Vector3 direction = ShootSystem.transform.forward;

                direction += new Vector3(
                    Random.Range(-spreadAngle, spreadAngle),
                    Random.Range(-spreadAngle, spreadAngle),
                    Random.Range(-spreadAngle, spreadAngle)
                );

                direction.Normalize();

                Vector3 origin = ShootSystem.transform.position;

                if (
                    Physics.Raycast(
                        origin,
                        direction,
                        out RaycastHit hit,
                        float.MaxValue,
                        ShootConfig.hitMask
                    )
                )
                {
                    RpcPlayShootEffect(origin, hit.point);
                }
                else
                {
                    RpcPlayShootEffect(origin, origin + (direction * TrailConfig.MissDistance));
                }
            }
        }
    }

    private TrailRenderer CreateTrail()
    {
        GameObject instance = new GameObject("Bullet Trail");
        TrailRenderer trail = instance.AddComponent<TrailRenderer>();
        trail.colorGradient = TrailConfig.Color;
        trail.material = TrailConfig.material;
        trail.widthCurve = TrailConfig.WidthCurve;
        trail.time = TrailConfig.Duration;
        trail.minVertexDistance = TrailConfig.MinVertexDistance;
        trail.emitting = false;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return trail;
    }
}
