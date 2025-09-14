using System.Collections;
using Mirror;
using UnityEngine;

public class DestroyEffect : NetworkBehaviour
{
    public AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (isServer) // Only the server can trigger this
        {
            RpcPlayExplosionSound();
        }

        StartCoroutine(DestroyExplosionClone());
    }

    private IEnumerator DestroyExplosionClone()
    {
        yield return new WaitForSeconds(3f);
        NetworkServer.Destroy(gameObject);
    }

    [ClientRpc]
    void RpcPlayExplosionSound()
    {
        // This should only be executed on clients
        if (audioSource != null)
        {
            audioSource.Play();
        }
    }
}
