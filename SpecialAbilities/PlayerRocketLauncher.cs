using System.Collections;
using Mirror;
using UnityEngine;

public class PlayerRocketLauncher : NetworkBehaviour
{
    public GameObject rocketPrefab;
    private Transform firePoint;
    private Transform rocketLauncher;
    private float nextFireTime;
    public float fireRate = 3f;
    private Animator LauncherRecoil = null;

    void Start()
    {
        // Try to find the rocket launcher and firePoint early
        FindRocketLauncher();
        Animator[] anims = GetComponentsInChildren<Animator>(true);
        foreach (Animator anim in anims)
        {
            if (anim.gameObject.name.Contains("Rocket_Launcher"))
            {
                if (anim.gameObject.activeInHierarchy)
                {
                    LauncherRecoil = anim;
                    Debug.Log("✅ Found and active: " + anim.gameObject.name);
                    break;
                }
                else
                {
                    Debug.LogWarning("Found launcher but it's inactive!");
                }
            }
        }
        TryAssignLauncherAnimator();
        // Also setup the animator for launcher recoil
    }

    void Update()
    {
        if (!isLocalPlayer)
            return;

        // Retry finding launcher if needed (safe fallback)
        if (rocketLauncher == null || firePoint == null)
        {
            FindRocketLauncher();
        }

        if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            StartCoroutine(WaitAndFire()); // Use coroutine to ensure firePoint is ready
        }
    }

    IEnumerator WaitAndFire()
    {
        int waitFrames = 0;

        // Wait max 1 second (60 frames) to avoid infinite loop
        while (firePoint == null && waitFrames < 60)
        {
            FindRocketLauncher(); // Retry assignment
            yield return null;
            waitFrames++;
        }

        if (firePoint != null)
        {
            CmdFireRocket();
        }
        else
        {
            Debug.LogError("🚫 FirePoint is still null after waiting. Aborting rocket fire.");
        }
    }

    [Command]
    void CmdFireRocket()
    {
        if (firePoint == null)
        {
            Debug.LogError("❌ Cannot fire rocket: FirePoint is missing!");
            return;
        }

        if (LauncherRecoil != null)
        {
            RpcLauncherRecoilAnimation();
        }
        else
        {
            Debug.LogWarning("🚫 Launcher animator not found or not active!");
        }

        GameObject rocketInstance = Instantiate(
            rocketPrefab,
            firePoint.position,
            firePoint.rotation
        );
        HomingMissile homingMissile = rocketInstance.GetComponent<HomingMissile>();

        if (homingMissile != null)
        {
            homingMissile.Initialize(transform);
        }
        else
        {
            Debug.LogError(
                "<color=red>ERROR: HomingMissile script is missing on the rocket prefab!</color>"
            );
        }

        NetworkServer.Spawn(rocketInstance);
        Debug.Log("<color=cyan>🚀 Rocket Spawned at: </color>" + firePoint.position);
    }

    [ClientRpc]
    private void RpcLauncherRecoilAnimation()
    {
        if (LauncherRecoil == null)
            TryAssignLauncherAnimator(); // Try again if it's missing

        if (LauncherRecoil != null)
        {
            LauncherRecoil.Play("RocketLauncherRecoil", 0, 0f);
            Debug.Log("🎯 Recoil animation triggered on client!");
        }
        else
        {
            Debug.LogError("🚫 Cannot play recoil: Animator is still null!");
        }
    }

    private void TryAssignLauncherAnimator()
    {
        if (LauncherRecoil != null)
            return;

        if (rocketLauncher == null)
        {
            Debug.LogWarning("🚫 RocketLauncher not found yet for assigning animator.");
            return;
        }

        Animator anim = rocketLauncher.GetComponent<Animator>();
        if (anim != null)
        {
            LauncherRecoil = anim;
            Debug.Log("✅ Launcher animator assigned directly from RocketLauncher object.");
        }
        else
        {
            // Fallback if not directly on launcher
            Animator[] anims = rocketLauncher.GetComponentsInChildren<Animator>(true);
            foreach (Animator a in anims)
            {
                if (a.gameObject.name.Contains("Rocket_Launcher"))
                {
                    LauncherRecoil = a;
                    Debug.Log("✅ Launcher animator found in children: " + a.gameObject.name);
                    break;
                }
            }
        }

        if (LauncherRecoil == null)
            Debug.LogWarning("❌ Still couldn't find launcher animator.");
    }

    private void FindRocketLauncher()
    {
        if (rocketLauncher != null && firePoint != null)
            return;

        Transform cameraTransform = transform.Find("Camera");
        if (cameraTransform == null)
        {
            Debug.LogWarning("<color=yellow>Camera not found on player!</color>");
            return;
        }

        Transform weaponHolder = cameraTransform.Find("WeaponHolder");
        if (weaponHolder == null)
        {
            Debug.LogWarning("<color=yellow>WeaponHolder not found inside Camera!</color>");
            return;
        }

        foreach (Transform child in weaponHolder)
        {
            if (child.name.StartsWith("Rocket_Launcher"))
            {
                rocketLauncher = child;
                rocketLauncher.localScale = new Vector3(600, 600, 600);
                firePoint = rocketLauncher.Find("FirePoint");

                if (firePoint == null)
                {
                    Debug.LogError("<color=red>FirePoint not found inside RocketLauncher!</color>");
                }
                else
                {
                    Debug.Log(
                        "<color=green>🎯 Rocket Launcher assigned: </color>" + rocketLauncher.name
                    );
                }
                return;
            }
        }

        Debug.LogWarning("<color=yellow>🚀 Rocket Launcher not found inside WeaponHolder!</color>");
    }
}
