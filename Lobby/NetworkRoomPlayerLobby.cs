using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NetworkRoomPlayerLobby : NetworkBehaviour
{
    [Header("UI")]
    [SerializeField]
    private GameObject lobbyUI = null;

    [SerializeField]
    private TMP_Text[] playerNameTexts = new TMP_Text[4];

    [SerializeField]
    private TMP_Text[] playerReadyTexts = new TMP_Text[4];

    [SerializeField]
    private TMP_Text[] blueTeamSlots = new TMP_Text[4];

    [SerializeField]
    private TMP_Text[] redTeamSlots = new TMP_Text[4];

    [SerializeField]
    private TMP_Text[] blueTeamReadyTexts = new TMP_Text[4];

    [SerializeField]
    private TMP_Text[] redTeamReadyTexts = new TMP_Text[4];

    [SerializeField]
    private Button startGameButton = null;

    [SyncVar(hook = nameof(HandleDisplayNameChanged))]
    public string DisplayName = "Loading..";

    [SyncVar(hook = nameof(HandleReadyStatusChanged))]
    public bool IsReady = false;

    // Store Selected Character Index
    [SyncVar(hook = nameof(OnCharacterIndexChanged))]
    public int SelectedCharacterIndex;

    // [SyncVar] public string DisplayName;
    //[SyncVar] public bool IsLeader;
    [SyncVar]
    public string Team; // "Blue" or "Red"

    public void SetTeam(string team)
    {
        Team = team;
        Debug.Log($"Assigning {DisplayName} to {team} team");
        UpdateDisplay(); // Update UI immediately
        UpdateLobbyUI(); // Log changes in console
    }

    private void OnCharacterIndexChanged(int oldIndex, int newIndex)
    {
        Debug.Log($"🎭 Character index changed from {oldIndex} to {newIndex}");
    }

    private bool isLeader;
    public bool IsLeader
    {
        set
        {
            isLeader = value;
            if (startGameButton != null)
            {
                startGameButton.gameObject.SetActive(value);
            }
        }
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

    public void SetSelectedCharacter(int index)
    {
        if (!isLocalPlayer)
            return; // Ensure only local player can set this

        CmdSetSelectedCharacter(index);
    }

    [Command]
    private void CmdSetSelectedCharacter(int index)
    {
        SelectedCharacterIndex = index;
    }

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        Debug.Log($"OnStartAuthority called for {netId}");

        // ✅ Ensure we get the right name
        string savedName = PlayerPrefs.GetString(PlayerNameInput.PlayerPrefsNameKey, "Unknown");

        Debug.Log($"Retrieved Name from PlayerPrefs: {savedName}");

        PlayerNameInput.SetDisplayName(savedName);

        // ✅ Wait a frame before sending the name to ensure PlayerPrefs is fully updated
        Invoke(nameof(DelayedSetName), 0.1f);

        if (lobbyUI != null)
        {
            lobbyUI.SetActive(true); // Show UI only for the local player
        }
    }

    private void DelayedSetName()
    {
        CmdSetDisplayName(PlayerNameInput.DisplayName);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (isLocalPlayer)
        {
            int selectedCharacter = PlayerPrefs.GetInt("SelectedCharacter", 0);
            SetSelectedCharacter(selectedCharacter);
        }
        Debug.Log($"OnStartClient called for {netId}");

        if (Room == null || Room.RoomPlayers == null)
        {
            Debug.LogError("Room or RoomPlayers list is null in OnStartClient");
            return;
        }

        // ✅ Ensure this player is not already in the list
        if (!Room.RoomPlayers.Contains(this))
        {
            Room.RoomPlayers.Add(this);
        }
        else
        {
            Debug.LogWarning($"Duplicate player detected on host: {DisplayName} ({netId})");
        }

        UpdateDisplay();

        // ✅ Hide UI for non-local players to prevent overlapping UI
        if (!isLocalPlayer && lobbyUI != null)
        {
            lobbyUI.SetActive(false);
        }
    }

    public override void OnStopClient()
    {
        if (Room != null && Room.RoomPlayers != null)
        {
            Room.RoomPlayers.Remove(this);
        }
        // Destroy(this.gameObject); // Add this
        UpdateDisplay();
    }

    private void HandleReadyStatusChanged(bool oldValue, bool newValue) => UpdateDisplay();

    private void HandleDisplayNameChanged(string oldValue, string newValue) => UpdateDisplay();

    private void UpdateDisplay()
    {
        if (!isLocalPlayer)
        {
            foreach (var player in Room?.RoomPlayers ?? new())
            {
                if (player.isOwned)
                {
                    player.UpdateDisplay();
                    break;
                }
            }
            return;
        }

        for (int i = 0; i < playerNameTexts.Length; i++)
        {
            if (playerNameTexts[i] != null)
                playerNameTexts[i].text = "Waiting For Players...";
            if (playerReadyTexts[i] != null)
                playerReadyTexts[i].text = string.Empty;
        }

        for (int i = 0; i < Room.RoomPlayers.Count && i < playerNameTexts.Length; i++)
        {
            if (playerNameTexts[i] != null)
            {
                // Set player name in team color
                string teamColor = Room.RoomPlayers[i].Team == "Blue" ? "blue" : "red";
                playerNameTexts[i].text =
                    $"<color={teamColor}>{Room.RoomPlayers[i].DisplayName}</color>";
            }
            if (playerReadyTexts[i] != null)
                playerReadyTexts[i].text = Room.RoomPlayers[i].IsReady
                    ? "<color=green>Ready</color>"
                    : "<color=red>Not Ready</color>";
        }

        for (int i = 0; i < blueTeamSlots.Length; i++)
        {
            if (blueTeamSlots[i] != null)
                blueTeamSlots[i].text = "Waiting For Players...";
            if (blueTeamReadyTexts[i] != null)
                blueTeamReadyTexts[i].text = string.Empty;
        }

        for (int i = 0; i < redTeamSlots.Length; i++)
        {
            if (redTeamSlots[i] != null)
                redTeamSlots[i].text = "Waiting For Players...";
            if (redTeamReadyTexts[i] != null)
                redTeamReadyTexts[i].text = string.Empty;
        }

        int blueIndex = 0;
        int redIndex = 0;

        // Assign players to the correct team panel
        for (int i = 0; i < Room.RoomPlayers.Count; i++)
        {
            var player = Room.RoomPlayers[i];

            // Set player name in team color
            string teamColor = player.Team == "Blue" ? "blue" : "red";
            string coloredName = $"<color={teamColor}>{player.DisplayName}</color>";
            string readyStatus = player.IsReady
                ? "<color=green>Ready</color>"
                : "<color=red>Not Ready</color>";

            if (player.Team == "Blue" && blueIndex < blueTeamSlots.Length)
            {
                if (blueTeamSlots[blueIndex] != null)
                    blueTeamSlots[blueIndex].text = coloredName;
                if (blueTeamReadyTexts[blueIndex] != null)
                    blueTeamReadyTexts[blueIndex].text = readyStatus;
                blueIndex++;
            }
            else if (player.Team == "Red" && redIndex < redTeamSlots.Length)
            {
                if (redTeamSlots[redIndex] != null)
                    redTeamSlots[redIndex].text = coloredName;
                if (redTeamReadyTexts[redIndex] != null)
                    redTeamReadyTexts[redIndex].text = readyStatus;
                redIndex++;
            }
        }
    }

    public void HandleReadyToStart(bool readyToStart)
    {
        if (!isLeader || startGameButton == null)
            return;
        startGameButton.interactable = readyToStart;
    }

    [Command]
    private void CmdSetDisplayName(string displayName)
    {
        Debug.Log($"CmdSetDisplayName called with: {displayName}");
        DisplayName = displayName;
    }

    public void OnReadyButtonClicked()
    {
        if (!isLocalPlayer) // Ensures only the local player calls this function
        {
            Debug.LogWarning("Ready button clicked, but this is not the local player!");
            return;
        }

        if (!NetworkClient.ready)
        {
            Debug.LogWarning("⚠️ Client is not ready! Calling ClientScene.Ready()...");
            NetworkClient.Ready();
        }

        CmdReadyUp();
    }

    [Command]
    private void CmdReadyUp()
    {
        if (connectionToClient != null && connectionToClient != base.connectionToClient)
        {
            Debug.LogWarning($"CmdReadyUp() called without proper authority on {netId}");
            return;
        }

        IsReady = !IsReady;
        Room?.NotifyPlayersOfReadyState();

        Debug.Log($"[{netId}] Ready state toggled to {IsReady} by local player");
    }

    [Command(requiresAuthority = true)]
    public void CmdStartGame()
    {
        if (!isServer)
        {
            Debug.LogError("[CmdStartGame] ERROR: Only the host can start the game!");
            return;
        }

        // if (NetworkServer.connections.Count < 2) // Change 2 to the minimum number of players required
        // {
        //     Debug.LogError("[CmdStartGame] ERROR: Not enough players in the room!");
        //     return;
        // }

        Debug.Log("[CmdStartGame] All players are in! Starting the game...");
        Debug.Log("outside if cmdstart");
        if (
            Room != null
            && Room.RoomPlayers.Count > 0
            && Room.RoomPlayers[0].connectionToClient == connectionToClient
        )
        {
            Room.StartGame();
            Debug.Log("inside if");
        }
    }

    void UpdateLobbyUI()
    {
        foreach (var player in FindObjectsByType<NetworkRoomPlayerLobby>(FindObjectsSortMode.None))
        {
            string teamText =
                player.Team == "Blue" ? "<color=blue>Blue</color>" : "<color=red>Red</color>";
            Debug.Log($"{player.DisplayName} - Team: {teamText}");
        }
    }
}
