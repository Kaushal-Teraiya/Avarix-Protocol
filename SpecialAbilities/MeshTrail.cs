using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MeshTrail : NetworkBehaviour
{
    public float activeTime = 2f;

    [Header("Mesh Related")]
    public float spawnDistance = 0.5f;
    public float meshDestroyDelay = 3f;
    public Transform positionToSpawn; // Spawn position of the mesh

    [Header("Shader Related")]
    public Material material;
    private bool isTrailActive;
    public string shaderVarRef;
    public float shaderVarRate = 0.1f;
    public float shaderVarRefreshRate = 0.05f;
    private SkinnedMeshRenderer[] skinnedMeshRenderers;

    private Vector3 lastSpawnPosition;
    private GameObject lastTrailInstance; // Tracks the last trail

    private Queue<GameObject> trailPool = new Queue<GameObject>(); // Object pool
    public int poolSize = 10; // Preallocate trail objects

    void Start()
    {
        InitializeTrailPool();
    }

    void Update()
    {
        if (!isLocalPlayer) return;
        if (Input.GetKeyDown(KeyCode.J) && !isTrailActive)
        {
            isTrailActive = true;
            lastSpawnPosition = positionToSpawn.position;
            StartCoroutine(ActivateTrail(activeTime));
        }
    }

    void InitializeTrailPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject trail = new GameObject("MeshTrailInstance");
            trail.AddComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.AddComponent<MeshFilter>();
            trail.SetActive(false);
            trailPool.Enqueue(trail);
        }
    }

    IEnumerator ActivateTrail(float timeActive)
    {
        float timer = 0f;

        while (timer < timeActive)
        {
            timer += Time.deltaTime;

            if (Vector3.Distance(lastSpawnPosition, positionToSpawn.position) >= spawnDistance)
            {
                CmdSpawnTrailMesh(); // Request spawn from the server
                lastSpawnPosition = positionToSpawn.position;
            }

            yield return null;
        }

        isTrailActive = false;

        // Move the last trail slightly behind
        if (lastTrailInstance != null)
        {
            lastTrailInstance.transform.position -= transform.forward * 1.2f;
        }
    }

    [Command] // Runs on the server
    void CmdSpawnTrailMesh()
    {
        RpcSpawnTrailMesh(); // Tell all clients to spawn the trail
    }

    [ClientRpc] // Runs on all clients
    void RpcSpawnTrailMesh()
    {
        if (trailPool.Count == 0)
        {
            Debug.LogWarning("Trail Pool is empty!");
            return;
        }

        GameObject skin = trailPool.Dequeue();
        skin.SetActive(true);

        Vector3 spawnPos = positionToSpawn.position;

        skin.transform.SetPositionAndRotation(spawnPos, positionToSpawn.rotation);

        MeshRenderer meshRenderer = skin.GetComponent<MeshRenderer>();
        MeshFilter meshFilter = skin.GetComponent<MeshFilter>();

        if (skinnedMeshRenderers == null)
        {
            skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        }

        Mesh mesh = new Mesh();
        skinnedMeshRenderers[0].BakeMesh(mesh);
        meshFilter.mesh = mesh;

        Material newMaterial = new Material(material);
        newMaterial.color = new Color(Random.value, Random.value, Random.value, 1f);
        meshRenderer.material = newMaterial;

        StartCoroutine(AnimateMaterialFloat(meshRenderer.material, 0, shaderVarRate, shaderVarRefreshRate));

        lastTrailInstance = skin; // Store the last spawned trail

        // Return to pool after fade-out
        StartCoroutine(ReturnToPool(skin, meshDestroyDelay));
    }

    IEnumerator ReturnToPool(GameObject skin, float delay)
    {
        yield return new WaitForSeconds(delay);
        skin.SetActive(false);
        trailPool.Enqueue(skin);
    }

    IEnumerator AnimateMaterialFloat(Material material, float goal, float rate, float refreshrate)
    {
        float valueToAnimate = material.GetFloat(shaderVarRef);

        while (valueToAnimate > goal)
        {
            valueToAnimate -= rate;
            material.SetFloat(shaderVarRef, valueToAnimate);
            yield return new WaitForSeconds(refreshrate);
        }
    }
}
