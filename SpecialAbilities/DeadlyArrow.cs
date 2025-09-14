using Mirror;
using UnityEngine;

public class DeadlyArrow : NetworkBehaviour
{
    public GameObject ArrowVFXprefab;
    public Vector3 heightOffset = new Vector3(0f, 1f, 0f);
    public int numberOfArrows;
    public float offsetX,
        offsetY,
        offsetZ;

    void Update()
    {
        if (!isLocalPlayer)
            return;

        if (Input.GetKeyDown(KeyCode.J) && numberOfArrows > 0)
        {
            CmdSpawnArrow(transform.position, transform.rotation); // tell server
            numberOfArrows--;
        }
    }

    [Command]
    void CmdSpawnArrow(Vector3 position, Quaternion rotation)
    {
        Vector3 spawnPosition =
            position + transform.forward * offsetZ + Vector3.up * offsetY + Vector3.left * offsetX;

        GameObject arrow = Instantiate(ArrowVFXprefab, spawnPosition, rotation);
        NetworkServer.Spawn(arrow);
        Debug.Log("Arrow spawned on server");
    }
}
