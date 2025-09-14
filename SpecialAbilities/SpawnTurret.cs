using Mirror;
using UnityEngine;

public class SpawnTurret : NetworkBehaviour
{
    public GameObject Turret;

    [SerializeField]
    private Vector3 Offset = new Vector3(0f, 0f, 1f);
    private FlagHandler player;

    void Start()
    {
        player = GetComponent<FlagHandler>();
    }

    void Update()
    {
        if (!isLocalPlayer)
            return;

        if (Input.GetKeyDown(KeyCode.J) && Turret != null)
        {
            CmdSpawnTurret();
        }
    }

    [Command] // Runs on the server
    private void CmdSpawnTurret()
    {
        GameObject _turret = Instantiate(
            Turret,
            transform.position + transform.forward * 5,
            transform.rotation
        );
        Turret turret = _turret.GetComponent<Turret>();
        if (turret != null)
        {
            turret.TurretTeam = player.Team;
        }

        NetworkServer.Spawn(_turret);
    }
}
