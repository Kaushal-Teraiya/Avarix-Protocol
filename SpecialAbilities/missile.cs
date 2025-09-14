using System.Collections;
using Mirror;
using UnityEngine;

public enum MissileSoundType
{
    Launch,
    Travel,
    Explosion,
}

public class HomingMissile : NetworkBehaviour
{
    [Header("Missile Settings")]
    public float speed = 15f;
    public float rotateSpeed = 130f;
    public float detectionRadius = 15f;
    public float explosionRadius = 100f;
    public float lifetime = 6f;
    public float initialForce = 500f;
    public GameObject explosionEffect;

    [Header("Prediction Settings")]
    public float maxDistancePredict = 100f;
    public float minDistancePredict = 5f;
    public float maxTimePrediction = 2.5f;

    [Header("Deviation Settings")]
    public float deviationAmount = 30f;
    public float deviationSpeed = 3.5f;

    private Rigidbody rb;
    private Transform target;
    private Transform shooter;
    private AudioSource audioSource;
    private Vector3 standardPrediction,
        deviatedPrediction;

    public AudioClip launchSound;
    public AudioClip travelSound;
    public AudioClip explosionSound;
    NetworkIdentity attackerId;
    public GameObject sparkEffectPrefab; // Assign the spark effect prefab in the inspector
    private FlagHandler playerTeamChecker;

    public void Initialize(Transform shooterTransform)
    {
        shooter = shooterTransform;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.5f); // Orangey explosion color
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();

        if (isServer)
        {
            //FixInitialRotation();
            rb.AddForce(transform.forward * initialForce, ForceMode.Impulse);
            Invoke(nameof(DestroyMissile), lifetime);

            RpcPlaySound(MissileSoundType.Launch);
            RpcStartTravelSound();
        }
    }

    void FixedUpdate()
    {
        if (!isServer)
            return;

        FindTarget();

        if (target != null)
        { //LeadTimePercentage gives a value between min and max predict based on distance between missile and target
            float leadTimePercentage = Mathf.InverseLerp(
                minDistancePredict,
                maxDistancePredict,
                Vector3.Distance(transform.position, target.position)
            );
            PredictMovement(leadTimePercentage); // predicts where the target will be
            AddDeviation(leadTimePercentage); // Adds Wobble
            RotateRocket();
            rb.linearVelocity = transform.forward * speed; // Moves forward while adjusting direction
        }
        else
        {
            rb.linearVelocity = transform.forward * speed;
        }
    }

    void PredictMovement(float leadTimePercentage)
    {
        float predictionTime = Mathf.Lerp(0, maxTimePrediction, leadTimePercentage); //jetlu leadtimepercentage ochu hase etlu jaldi predict karse
        Rigidbody targetRb = target.GetComponent<Rigidbody>();

        if (targetRb)
        {
            standardPrediction = target.position + targetRb.linearVelocity * predictionTime; // standard prediction is a Vector3
        }
        else
        {
            standardPrediction = target.position;
        }
    }

    void AddDeviation(float leadTimePercentage) // Makes missile wobble in X-Z plane giving circular motion
    {
        Vector3 deviation = new Vector3(
            Mathf.Cos(Time.time * deviationSpeed),
            0,
            Mathf.Sin(Time.time * deviationSpeed)
        );
        Vector3 predictionOffset =
            transform.TransformDirection(deviation) * deviationAmount * leadTimePercentage;
        deviatedPrediction = standardPrediction + predictionOffset;
    }

    void RotateRocket()
    {
        Vector3 heading = deviatedPrediction - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(heading);
        rb.MoveRotation(
            Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotateSpeed * Time.deltaTime
            )
        );
    }

    void FindTarget()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius);
        float closestDistance = detectionRadius;
        Transform closestEnemy = null;

        foreach (Collider col in colliders)
        {
            if (
                col.CompareTag("Player")
                && col.transform != shooter
                && col.GetComponent<FlagHandler>().Team != shooter.GetComponent<FlagHandler>().Team
            )
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = col.transform;
                }
            }
        }

        if (closestEnemy != null)
        {
            target = closestEnemy;
            Debug.Log("<color=yellow>Missile locked onto target: " + target.name + "</color>");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isServer)
            return;
        if (other.CompareTag("Player") && other.transform != shooter)
        {
            Explode(attackerId);
        }
    }

    private IEnumerator DelayExplode()
    {
        yield return new WaitForSeconds(2f);
        Explode(attackerId);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isServer)
            return;

        if (collision.transform != shooter)
        {
            // Instantiate sparks at the collision point
            Vector3 collisionPoint = collision.contacts[0].point; // Use the first contact point for better accuracy
            GameObject sparks = Instantiate(sparkEffectPrefab, collisionPoint, Quaternion.identity);

            // Optionally, destroy the sparks after a short time (e.g., 1 second)
            Destroy(sparks, 1f); // Adjust the time based on how long you want the sparks to last
            if (collision.transform.CompareTag("Player"))
            {
                Explode(attackerId);
            }
            StartCoroutine(DelayExplode());
        }
    }

    void Explode(NetworkIdentity attackerId)
    {
        RpcPlaySound(MissileSoundType.Explosion);

        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider col in colliders)
        {
            float distance = Vector3.Distance(transform.position, col.transform.position);
            int damageDropOff = (int)CalculateDamage(distance, 0f, explosionRadius, 60f);

            if (col.CompareTag("Player") && col.transform != shooter)
            {
                player playerToDamage = col.GetComponentInChildren<player>();
                playerToDamage.RpctakeDamageU(damageDropOff, attackerId);
            }
        }

        if (explosionEffect != null)
        {
            GameObject explosion = Instantiate(
                explosionEffect,
                transform.position,
                Quaternion.identity
            );
            NetworkServer.Spawn(explosion);
            // StartCoroutine(DestroyExplosionClone(explosion, 2f)); // Use coroutine
        }

        DestroyMissile();
    }

    float CalculateDamage(float distance, float minRange, float maxRange, float maxDamage)
    {
        if (distance <= minRange)
            return maxDamage;

        if (distance >= maxRange)
            return 0f;

        float t = (distance - minRange) / (maxRange - minRange); // 0 to 1
        return Mathf.Lerp(maxDamage, 0f, t); // linear dropoff
    }

    void DestroyMissile()
    {
        RpcStopTravelSound();
        ParticleSystem smokeTrail = GetComponentInChildren<ParticleSystem>();

        if (smokeTrail != null)
        {
            smokeTrail.transform.SetParent(null);
            smokeTrail.transform.localScale = Vector3.one;
            var main = smokeTrail.main;
            main.stopAction = ParticleSystemStopAction.Destroy;
            smokeTrail.Stop();
            StartCoroutine(DestroySmokeAfterDelay(smokeTrail.gameObject, 1.5f));
        }

        NetworkServer.Destroy(gameObject);
    }

    IEnumerator DestroySmokeAfterDelay(GameObject smokeObject, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (smokeObject != null)
            Destroy(smokeObject);
    }

    [ClientRpc]
    void RpcPlaySound(MissileSoundType soundType)
    {
        if (audioSource == null)
            return;

        switch (soundType)
        {
            case MissileSoundType.Launch:
                if (launchSound != null)
                    audioSource.PlayOneShot(launchSound);
                break;
            case MissileSoundType.Explosion:
                if (explosionSound != null)
                    audioSource.PlayOneShot(explosionSound);
                break;
        }
    }

    [ClientRpc]
    void RpcStartTravelSound()
    {
        if (audioSource != null && travelSound != null)
        {
            audioSource.clip = travelSound;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    [ClientRpc]
    void RpcStopTravelSound()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}
