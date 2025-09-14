using System.Collections;
using Mirror;
using UnityEngine;

public class ArrowShoot : NetworkBehaviour
{
    private Rigidbody rb;
    public int initialForce;
    public int arrowDamage;
    NetworkIdentity attackerId;
    public int lifetime;
    public GameObject sparkEffectPrefab;

    void OnCollisionEnter(Collision collision)
    {
        if (!isServer)
            return;

        if (collision.transform.CompareTag("Player") || collision.transform.CompareTag("Cover"))
        {
            // Instantiate sparks at the collision point
            Vector3 collisionPoint = collision.contacts[0].point; // Use the first contact point for better accuracy
            GameObject sparks = Instantiate(sparkEffectPrefab, collisionPoint, Quaternion.identity);
            player playerToDamage = collision.gameObject.GetComponentInChildren<player>();
            RpcSpawnSparks(collisionPoint);
            Destroy(sparks, 1f); // Adjust the time based on how long you want the sparks to last
            NetworkServer.Destroy(gameObject);

            if (playerToDamage != null)
            {
                playerToDamage.RpctakeDamageU(arrowDamage, attackerId);
            }

            // Optionally, destroy the sparks after a short time (e.g., 1 second)
        }
    }

    [ClientRpc]
    void RpcSpawnSparks(Vector3 pos)
    {
        GameObject sparks = Instantiate(sparkEffectPrefab, pos, Quaternion.identity);
        Destroy(sparks, 1f);
    }

    public override void OnStartServer()
    {
        rb = GetComponent<Rigidbody>();
        rb.AddForce(transform.forward * initialForce, ForceMode.Impulse);

        Debug.Log("Arrow Launched. Will destroy in " + lifetime + " seconds.");
        StartCoroutine(DestroyArrow());
    }

    IEnumerator DestroyArrow()
    {
        yield return new WaitForSeconds(lifetime);
        NetworkServer.Destroy(gameObject);
        Debug.LogWarning("Arrow Destroyed");
    }

    // Update is called once per frame
}
