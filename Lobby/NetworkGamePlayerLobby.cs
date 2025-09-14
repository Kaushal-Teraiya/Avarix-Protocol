using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NetworkGamePlayerLobby : NetworkBehaviour
{
    [SyncVar]
    public string DisplayName = "Loading..";

    //[SyncVar] public string DisplayName;
    [SerializeField]
    [SyncVar(hook = nameof(OnTeamChanged))]
    public string Team = "None"; // "Blue" or "Red"

    private void OnTeamChanged(string oldTeam, string newTeam)
    {
        Debug.Log($"Team changed from {oldTeam} to {newTeam}");
        FlagHandler flagHandler = GetComponent<FlagHandler>();
        if (flagHandler != null)
        {
            SetTeam(newTeam);
        }
    }

    public static List<NetworkGamePlayerLobby> NGPLlist = new List<NetworkGamePlayerLobby>();

    public void SetDisplayName(string displayName)
    {
        DisplayName = displayName;
    }

    public void SetTeam(string team)
    {
        Debug.Log($"Setting Team for {DisplayName}: {team}");
        Team = team;
    }

    private NetworkManagerLobby room;
    private NetworkManagerLobby Room
    {
        get
        {
            if (room == null)
            {
                room = NetworkManager.singleton as NetworkManagerLobby;
            }
            return room;
        }
    }

    public int SelectedCharacterIndex { get; private set; }

    public override void OnStartClient()
    {
        base.OnStartClient();
        DontDestroyOnLoad(gameObject);
        StartCoroutine(CheckForUpdatedTeam());

        if (!Room.GamePlayers.Contains(this))
            Room.GamePlayers.Add(this);
        else
            Debug.LogWarning("⚠️ GamePlayer already in list, skipping.");

        // ✅ If character index hasn't been set on server, resend it
        if (isLocalPlayer && SelectedCharacterIndex == 0) // or whatever default you use
        {
            int savedIndex = PlayerPrefs.GetInt("SelectedCharacter", 0);
            Debug.Log($"🔁 Re-applying character index: {savedIndex}");
            CmdSetCharacterIndex(savedIndex);
        }

        CmdRequestCharacterSpawn();
        Debug.Log($"🛠 Requested character spawn for index: {SelectedCharacterIndex}");
    }

    private IEnumerator CheckForUpdatedTeam()
    {
        yield return new WaitUntil(() => GetComponent<NetworkGamePlayerLobby>().Team != "None");

        NetworkGamePlayerLobby player = GetComponent<NetworkGamePlayerLobby>();
        if (player != null)
        {
            Debug.Log($"FlagHandler detected team: {player.Team}");
            Team = player.Team;
        }
    }

    public static void Shutdown()
    {
        NetworkClient.Shutdown();
        NetworkServer.Shutdown();
        if (NetworkManager.singleton != null)
        {
            NetworkManager.singleton.StopHost();
            NetworkManager.singleton.StopClient();
            NetworkManager.singleton.StopServer();
        }
    }

    public override void OnStopClient()
    {
        if (Room != null && Room.GamePlayers != null)
        {
            Room.GamePlayers.Remove(this);
        }

        // Destroy(this.gameObject); // Add this

        foreach (var go in FindObjectsByType<NetworkRoomPlayerLobby>(FindObjectsSortMode.None))
        {
            Destroy(go.gameObject);
        }

        ClientDisconnectHandler.Instance.FullResetAndReturnToCharacterSelection();
    }

    [Command]
    public void CmdSetCharacterIndex(int index)
    {
        SelectedCharacterIndex = index;
    }

    [Command]
    public void CmdRequestCharacterSpawn()
    {
        CharacterSpawner spawner = FindObjectsByType<CharacterSpawner>(FindObjectsSortMode.None)[0];
        int charIndex = SelectedCharacterIndex;

        if (spawner != null)
            spawner.CmdSpawnCharacter(charIndex, connectionToClient);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        NGPLlist.Add(this);
        Debug.Log("Player added to NGPLlist: " + DisplayName);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        NGPLlist.Remove(this);
        Debug.Log("Player removed from NGPLlist: " + DisplayName);
    }

    // [Server]
    // public void SetDisplayName(string displayName)
    // {
    //      Debug.Log($"Setting DisplayName: {name}");
    //        if (displayName == null)
    //     {
    //         Debug.LogError("DisplayName is NULL!");
    //     }
    //     this.DisplayName = displayName;
    // }
}
