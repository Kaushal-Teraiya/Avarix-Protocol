using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PlayerStats : NetworkBehaviour
{
    [SyncVar]
    private string playerName;

    [SyncVar]
    public string FirebaseUID; // ← store UID here
    public string PlayerName => playerName; // read-only

    [SyncVar]
    private string team;
    public string Team => team; // read-only

    [SyncVar]
    private int kills;
    public int Kills => kills; // read-only

    [SyncVar]
    private int deaths;
    public int Deaths => deaths; // read-only

    private NetworkManagerLobby playerInfo;
    public static List<PlayerStats> allPlayers = new List<PlayerStats>();
    public static List<PlayerStats> clientSidePlayers = new List<PlayerStats>();

    [Server]
    public void CmdAddKill() => kills++;

    [Server]
    public void CmdAddDeath() => deaths++;

    void Start()
    {
        playerInfo = FindObjectsByType<NetworkManagerLobby>(FindObjectsSortMode.None)[0];
    }

    public IEnumerator SetPlayerInfo()
    {
        yield return new WaitUntil(() => NetworkGamePlayerLobby.NGPLlist.Count > 0);

        foreach (var ngpl in NetworkGamePlayerLobby.NGPLlist)
        {
            Debug.Log("ngpl:" + ngpl.connectionToClient + " " + "this player" + connectionToClient);
            // Assuming connectionToClient is used to pair them (or some other matching key)
            if (ngpl.connectionToClient == connectionToClient)
            {
                playerName = ngpl.DisplayName;
                team = ngpl.Team;
                Debug.Log($"Matched NGPL → Player info set: {playerName}, Team: {team}");
                break;
            }
        }

        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogError("Failed to match a NGPL with this PlayerStats.");
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!clientSidePlayers.Contains(this))
            clientSidePlayers.Add(this);
        StartCoroutine(SetPlayerInfo());
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        allPlayers.Add(this);
        Debug.Log("Player added to allPlayers list: " + playerName);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if (clientSidePlayers.Contains(this))
            clientSidePlayers.Remove(this);
        Debug.Log("Player removed from clientSidePlayers list: " + playerName);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        allPlayers.Remove(this);
        if (clientSidePlayers.Contains(this))
            clientSidePlayers.Remove(this);

        Debug.Log("Player removed from allPlayers list: " + playerName);
    }

    public void SetFirebaseUID(string uid)
    {
        FirebaseUID = uid;
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        string uid = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
        if (!string.IsNullOrEmpty(uid))
        {
            CmdSetFirebaseUID(uid);
        }
        else
        {
            Debug.LogWarning("[Auth] Firebase UID is null on local player!");
        }
    }

    [Command]
    void CmdSetFirebaseUID(string uid)
    {
        FirebaseUID = uid; // synced to server
        PlayerAuthCache.Register(connectionToClient.connectionId, uid);
        Debug.Log($"[Auth] Registered {uid} for conn {connectionToClient.connectionId}");
    }
}
