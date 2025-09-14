using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkManagerLobby : NetworkManager
{
    [SerializeField]
    private string menuScene = "Lobby";

    //[SerializeField][SyncVar] private string mapName = null;

    [Header("Room")]
    [SerializeField]
    private NetworkRoomPlayerLobby loomPlayerPrefab = null;

    [SerializeField]
    private int minPlayers = 2;

    [Header("Game")]
    [SerializeField]
    private NetworkGamePlayerLobby gamePlayerPrefab = null;

    public static event Action onClientConnected;
    public static event Action OnClientDisconnected;

    public List<NetworkRoomPlayerLobby> RoomPlayers { get; } = new List<NetworkRoomPlayerLobby>();
    public List<NetworkGamePlayerLobby> GamePlayers { get; } = new List<NetworkGamePlayerLobby>();
    public static NetworkManagerLobby instance;
    public MapSelectionMessage MapSelection;
    public string selectedMapName;
    public MapData[] availableMaps;

    //private object availableMaps;

    public struct MapSelectionMessage : NetworkMessage
    {
        public string mapName;
    }

    private void OnReceiveMapSelection(NetworkConnectionToClient conn, MapSelectionMessage msg)
    {
        Debug.Log("📥 Received Map Selection Message!");

        if (conn != NetworkServer.localConnection) // Only process from the host
        {
            return;
        }

        selectedMapName = msg.mapName;
        Debug.Log($"🌍 Map selection updated: {selectedMapName}");

        // Get map scene name
        MapData selectedMapData = availableMaps.FirstOrDefault(map =>
            map.mapName == selectedMapName
        );
        if (selectedMapData != null)
        {
            selectedMapName = selectedMapData.mapName;
            Debug.Log($"🌍 Selected scene: {selectedMapName}");
        }

        // Broadcast to all clients
        NetworkServer.SendToAll(msg);
    }

    public override void Awake()
    {
        instance = this; // Singleton setup
        //MapSelection = GetComponent<MapSelectionMessage>();

        NetworkClient.OnConnectedEvent += OnClientConnect;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        spawnPrefabs = Resources.LoadAll<GameObject>("SpawnablePrefabs").ToList();
        NetworkServer.RegisterHandler<MapSelectionMessage>(OnReceiveMapSelection);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        NetworkClient.RegisterHandler<MapSelectionMessage>(
            (conn, msg) =>
            {
                Debug.Log($"Client received map name: {selectedMapName}");
            }
        );

        var spawnablePrefabs = Resources.LoadAll<GameObject>("SpawnablePrefabs");
        foreach (var prefab in spawnablePrefabs)
        {
            NetworkClient.RegisterPrefab(prefab);
        }

        // Request current map selection from the server
        // if (!NetworkServer.active)
        // {
        //     // Send the map selection message to the server
        //   //  NetworkClient.Send(new MapSelectionMessage { mapName = selectedMapName });
        // }

        Debug.Log("🔹 Registering scene objects...");
        NetworkServer.SpawnObjects(); // Ensure scene objects sync correctly
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        onClientConnected?.Invoke();

        // 🔹 Grab Firebase UID from client’s auth
        string uid = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
        Debug.LogWarning("[Client] Firebase UID: " + uid);

        // if (!string.IsNullOrEmpty(uid))
        // {
        //     // store locally for the player object
        //     var ps = NetworkClient.connection.identity.GetComponent<PlayerStats>();
        //     if (ps != null)
        //     {
        //         ps.FirebaseUID = uid; // directly set the UID
        //     }
        //     else
        //     {
        //         Debug.LogError("PlayerStats component not found on local player!");
        //     }

        // request spawn
        if (NetworkClient.localPlayer == null)
        {
            Debug.Log("Requesting AddPlayer...");
            NetworkClient.Send(new AddPlayerMessage());
        }
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        OnClientDisconnected?.Invoke();
        Debug.Log("Client disconnected (custom manager).");

        // If we're not the host (server + client), load character selection
        if (!NetworkServer.active)
        {
            Debug.Log("Client-only disconnected. Returning to Character Selection...");
            SceneManager.LoadScene("Character Selection");
        }
        else
        {
            Debug.Log("Host/server disconnect detected. Not loading scene.");
        }
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);
        if (numPlayers >= maxConnections)
        {
            conn.Disconnect();
            return;
        }

        if (SceneManager.GetActiveScene().path != menuScene)
        {
            conn.Disconnect();
            return;
        }
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        // base.OnServerAddPlayer(conn);
        if (
            SceneManager.GetActiveScene().name
            == System.IO.Path.GetFileNameWithoutExtension(menuScene)
        )
        {
            bool isLeader = RoomPlayers.Count == 0;
            NetworkRoomPlayerLobby roomPlayerInstance = Instantiate(loomPlayerPrefab);
            roomPlayerInstance.IsLeader = isLeader;
            int blueCount = RoomPlayers.Count(p => p.Team == "Blue");
            int redCount = RoomPlayers.Count(p => p.Team == "Red");
            roomPlayerInstance.SetTeam(blueCount <= redCount ? "Blue" : "Red");
            RoomPlayers.Add(roomPlayerInstance);
            NetworkServer.AddPlayerForConnection(conn, roomPlayerInstance.gameObject);
        }

        Debug.Log($"🧠 OnServerAddPlayer called for {conn.connectionId}");

        // ✅ Register Firebase UID for this player
        // var ps = conn.identity.GetComponent<PlayerStats>();
        // if (ps != null && !string.IsNullOrEmpty(ps.FirebaseUID))
        // {
        //     PlayerAuthCache.Register(conn.connectionId, ps.FirebaseUID);
        //     Debug.Log($"[Auth] Registered connId {conn.connectionId} with UID {ps.FirebaseUID}");
        // }
        // else
        // {
        //     Debug.LogWarning($"[Auth] No UID found for connId {conn.connectionId}");
        // }
    }

    // public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    // {
    //     Debug.Log($"🔗 OnServerAddPlayer called for connection: {conn.connectionId}");

    //     // 1. If the connection already has an identity, force a clean replacement
    //     if (conn.identity != null)
    //     {
    //         Debug.LogWarning(
    //             "⚠️ Existing player identity detected. Cleaning up and retrying in a moment..."
    //         );
    //         NetworkServer.Destroy(conn.identity.gameObject);
    //         StartCoroutine(DelayedAddPlayer(conn, fallback: true));
    //         return;
    //     }

    //     // 2. Which scene are we in?
    //     string current = SceneManager.GetActiveScene().name;
    //     if (current == System.IO.Path.GetFileNameWithoutExtension(menuScene))
    //     {
    //         // Lobby scene logic: instantiate and add the lobby player
    //         bool isLeader = RoomPlayers.Count == 0;
    //         NetworkRoomPlayerLobby roomPlayer = Instantiate(loomPlayerPrefab);
    //         roomPlayer.IsLeader = isLeader;

    //         // Assign a balanced team
    //         int blueCount = RoomPlayers.Count(p => p.Team == "Blue");
    //         int redCount = RoomPlayers.Count(p => p.Team == "Red");
    //         roomPlayer.SetTeam(blueCount <= redCount ? "Blue" : "Red");

    //         RoomPlayers.Add(roomPlayer);
    //         NetworkServer.AddPlayerForConnection(conn, roomPlayer.gameObject);
    //         Debug.Log($"🧍 Added lobby player for connection {conn.connectionId}");
    //     }
    //     else
    //     {
    //         // 3. In-game scene or midgame reconnect — delegate to Scene-Ready logic
    //         Debug.Log($"🎮 Connection {conn.connectionId} rejoining mid-game or joining in-game.");

    //         StartCoroutine(DelayedAddPlayer(conn, fallback: false));
    //     }

    //     base.OnServerAddPlayer(conn);
    // }

    /// <summary>
    /// Tries to add or replace a player after a short delay, when scene and cleanup are stable.
    /// </summary>
    private IEnumerator DelayedAddPlayer(NetworkConnectionToClient conn, bool fallback)
    {
        yield return new WaitForSeconds(0.5f); // allow teardown/loading to finish

        if (conn.identity == null)
        {
            Debug.Log($"🧪 Delayed spawning logic triggered for conn {conn.connectionId}");

            // If scene is lobby, trigger the normal logic again
            if (
                SceneManager.GetActiveScene().name
                == System.IO.Path.GetFileNameWithoutExtension(menuScene)
            )
            {
                OnServerAddPlayer(conn);
            }
            else
            {
                // Otherwise spawn mid-game using your character spawner logic
                Debug.LogError("Couldnt SpaennnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnNNNN");
            }
        }
        else
        {
            Debug.LogWarning(
                $"⚠️ conn.identity still present for {conn.connectionId}, skipping re-spawn."
            );
        }
    }

    /// <summary>
    /// Custom midgame spawn logic using existing room-player index/team.
    /// </summary>






    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        if (conn.identity != null)
        {
            var player = conn.identity.GetComponent<NetworkRoomPlayerLobby>();
            Debug.Log($"🧹 Destroying leftover player for conn {conn.connectionId}");
            RoomPlayers.Remove(player);
            NotifyPlayersOfReadyState();
            NetworkServer.Destroy(conn.identity.gameObject); // ✅ ACTUALLY destroy the player
        }

        base.OnServerDisconnect(conn);
    }

    public override void OnStopServer()
    {
        RoomPlayers.Clear();
    }

    public void NotifyPlayersOfReadyState()
    {
        foreach (var player in RoomPlayers)
        {
            player.HandleReadyToStart(IsReadyToStart());
        }
    }

    private bool IsReadyToStart()
    {
        if (numPlayers < minPlayers)
        {
            Debug.Log($"❌ Not enough players! Current: {numPlayers}, Required: {minPlayers}");
            return false;
        }

        foreach (var player in RoomPlayers)
        {
            if (!player.IsReady)
            {
                Debug.Log($"❌ Player {player.DisplayName} is NOT ready!");
                return false;
            }
        }

        Debug.Log("✅ All players are ready!");
        return true;
    }

    public void StartGame()
    {
        Debug.Log("🚀 StartGame() function was called!");
        Debug.Log($"🛠 menuScene is assigned as: {menuScene}");

        Debug.Log($"🛠 Active Scene: {SceneManager.GetActiveScene().name}");
        Debug.Log($"🛠 menuScene variable: {menuScene}");

        string currentScene = "Lobby";
        Debug.Log($"🔍 Current Scene: {currentScene}, Expected: {menuScene}");
        Debug.Log($"🛠 menuScene is assigned as: {menuScene}");

        if (currentScene == "Lobby")
        {
            if (!IsReadyToStart())
            {
                Debug.Log("⚠️ Not all players are ready, game cannot start.");
                return;
            }

            Debug.Log("✅ All players are ready! Changing scene...");
            ServerChangeScene(selectedMapName); // load Map scene
            Debug.Log("🎮 Start button clicked, changing scene.");
        }
        else
        {
            Debug.LogError("❌ Scene does not match menuScene! Cannot start game.");
        }
    }

    public override void Start()
    {
        if (string.IsNullOrEmpty(menuScene))
        {
            Debug.LogError("⚠️ menuScene is not set in the Inspector! Assigning default...");
            menuScene = "Lobby"; // Change this to your actual menu scene name.
        }
    }

    public override void ServerChangeScene(string newSceneName)
    {
        Debug.Log($"🟢 ServerChangeScene() called! Changing to: {newSceneName}");
        Debug.Log($"🔄 ServerChangeScene to {newSceneName}");

        Debug.Log($"📋 RoomPlayers count: {RoomPlayers.Count}");
        for (int i = 0; i < RoomPlayers.Count; i++)
        {
            Debug.Log(
                $"🧍 RoomPlayer[{i}]: {RoomPlayers[i].DisplayName} - Team: {RoomPlayers[i].Team}"
            );
        }

        if (SceneManager.GetActiveScene().name == "Lobby")
        {
            for (int i = RoomPlayers.Count - 1; i >= 0; i--)
            {
                var conn = RoomPlayers[i].connectionToClient;
                Debug.Log($"🔄 Replacing player for connection: {conn}");

                var gameplayerInstance = Instantiate(gamePlayerPrefab);
                gameplayerInstance.SetDisplayName(RoomPlayers[i].DisplayName);
                gameplayerInstance.SetTeam(RoomPlayers[i].Team);

                if (conn.identity != null)
                {
                    Debug.Log($"💀 Destroying: {conn.identity.gameObject.name}");
                    NetworkServer.Destroy(conn.identity.gameObject);
                }
                else
                {
                    Debug.LogError($"❌ ERROR: conn.identity is NULL for player {i}!");
                }

                NetworkServer.ReplacePlayerForConnection(
                    conn,
                    gameplayerInstance.gameObject,
                    ReplacePlayerOptions.KeepAuthority
                );

                Debug.Log(
                    $"✨ ReplacePlayerForConnection called for conn {conn.connectionId} with {gameplayerInstance.name}"
                );
            }
        }

        StartCoroutine(DelayedSpawnObjects());

        base.ServerChangeScene(newSceneName);
    }

    IEnumerator DelayedSpawnObjects()
    {
        yield return new WaitForSeconds(1f);
        NetworkServer.SpawnObjects();
        Debug.Log("✅ SpawnObjects called post-scene change.");
    }

    // public override void ServerChangeScene(string newSceneName)
    // {
    //     Debug.Log($"🟢 ServerChangeScene() called! Changing to: {newSceneName}");

    //     if (SceneManager.GetActiveScene().name == "Lobby")
    //     {
    //         for (int i = RoomPlayers.Count - 1; i >= 0; i--)
    //         {
    //             var conn = RoomPlayers[i].connectionToClient;
    //             Debug.Log($"🔄 Replacing player for connection: {conn}");
    //             Debug.Log($"👀 conn.identity: {conn.identity}");
    //             var gameplayerInstance = Instantiate(gamePlayerPrefab);
    //             gameplayerInstance.SetDisplayName(RoomPlayers[i].DisplayName);
    //             // Transfer team assignment
    //             gameplayerInstance.SetTeam(RoomPlayers[i].Team);

    //             if (conn.identity != null)
    //             {
    //                 Debug.Log($"💀 Destroying: {conn.identity.gameObject.name}");
    //                 NetworkServer.Destroy(conn.identity.gameObject);
    //             }
    //             else
    //             {
    //                 Debug.LogError($"❌ ERROR: conn.identity is NULL for player {i}!");
    //             }

    //             NetworkServer.ReplacePlayerForConnection(
    //                 conn,
    //                 gameplayerInstance.gameObject,
    //                 ReplacePlayerOptions.KeepAuthority
    //             );
    //         }
    //     }

    //     base.ServerChangeScene(newSceneName);
    // }
}
