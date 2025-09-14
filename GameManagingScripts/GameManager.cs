using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

public class GameManager : NetworkBehaviour
{
    public static GameManager instance;
    public MatchSettings matchSettings;
    [SerializeField] public GameObject sceneCamera;
    [SyncVar] public bool canPickUpRedFlag = true;
    [SyncVar] public bool canPickUpBlueFlag = true;

    [SyncVar(hook = nameof(OnIsStolenRedChanged))] public bool isStolenRed;
    [SyncVar(hook = nameof(OnIsStolenBlueChanged))] public bool isStolenBlue;

    [SyncVar(hook = nameof(OnIsDroppedRedChanged))] public bool isDroppedRed;
    [SyncVar(hook = nameof(OnIsDroppedBlueChanged))] public bool isDroppedBlue;


    // [SyncVar] public int MaxBluePlayers = 4;
    // [SyncVar] public int MaxRedPlayers = 4;

    // [SyncVar] public int BluePlayersCount;
    // [SyncVar] public int RedPlayersCount;

    void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("more than one gamemanager in scene");
        }
        else
        {
            instance = this;
        }
    }

    public void setSceneCameraActive(bool isActive)
    {
        if (sceneCamera == null)
        {
            return;
        }
        else
        {
            sceneCamera.SetActive(isActive);
        }
    }

    public void SetSceneCameraAbovePlayer(Vector3 playerPosition)
    {
        if (!isClient) return; // Ensure it runs only on the client

        if (sceneCamera == null) return;

        // Set camera position above and behind the player's death position
        sceneCamera.transform.position = playerPosition + new Vector3(0, 2, -4);
        sceneCamera.transform.LookAt(playerPosition);

        // Enable free look control
        sceneCamera.GetComponent<SceneCameraController>().enabled = true;

        // Activate scene camera
        sceneCamera.SetActive(true);
    }

    #region Player Tracking


    private const string PLAYER_ID_PREFIX = "Player ";
    private static Dictionary<string, player> Players = new Dictionary<string, player>();
    public static void RegisterPlayer(string _netID, player _player)
    {
        string _playerID = PLAYER_ID_PREFIX + _netID;
        Players.Add(_playerID, _player);
        _player.transform.name = _playerID;
    }

    public static void UnRegisterPlayer(string _playerID)
    {
        Players.Remove(_playerID);
    }

    public static player GetPlayer(string _playerID)
    {
        if (!Players.ContainsKey(_playerID))
        {
            Debug.LogError($"Player ID '{_playerID}' not found in GameManager.");
            return null; // Prevents crashing if the key is missing
        }
        return Players[_playerID];
    }


    private void OnIsStolenRedChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"🔴 isStolenRed updated globally: {newValue}");
    }

    private void OnIsStolenBlueChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"🔵 isStolenBlue updated globally: {newValue}");
    }
    private void OnIsDroppedRedChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"🔴 isDroppedRed updated globally: {newValue}");
    }

    private void OnIsDroppedBlueChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"🔵 isDroppedBlue updated globally: {newValue}");
    }

    [Command(requiresAuthority = false)]
    public void CmdSetFlagStolen(string flagColor, bool state)
    {


        if (!NetworkClient.ready)
        {
            Debug.LogWarning("[FlagHandler] Tried to call CmdSetFlagStolen but the client is not ready.");
            return;
        }
        Debug.Log($"[FlagHandler] CmdSetFlagStolen called: {flagColor}, stolen: {state}");
        if (flagColor == "Red")
            isStolenRed = state;
        else if (flagColor == "Blue")
            isStolenBlue = state;
    }

    [Command(requiresAuthority = false)]
    public void CmdSetFlagDropped(string flagColor, bool state)
    {
        if (!NetworkClient.ready)
        {
            Debug.LogWarning("[FlagHandler] Tried to call CmdSetFlagStolen but the client is not ready.");
            return;
        }
        Debug.Log($"[FlagHandler] CmdSetFlagStolen called: {flagColor}, stolen: {state}");
        if (flagColor == "Red")
            isDroppedRed = state;
        else if (flagColor == "Blue")
            isDroppedBlue = state;
    }

    internal void AddPlayerToTeam(FlagHandler flagHandler, string team)
    {
        throw new NotImplementedException();
    }


    public bool CanPickUpFlag(string team, GameObject flag)
    {
        if (flag.CompareTag("RedFlag") && team == "Blue")
        {
            return canPickUpRedFlag;  // Check if the red flag is available for pickup
        }
        else if (flag.CompareTag("BlueFlag") && team == "Red")
        {
            return canPickUpBlueFlag;  // Check if the blue flag is available for pickup
        }
        return false; // Default case: Flag cannot be picked up
    }

    /* void OnGUI()
{
    GUILayout.BeginArea(new Rect(200 ,200 ,200 , 500));
    GUILayout.BeginVertical();
    foreach (string _playerID in Players.Keys)
    {
        GUILayout.Label(_playerID+ " _ " + Players[_playerID].transform.name);
    }
    GUILayout.EndVertical();
    GUILayout.EndArea();
}*/

    #endregion

}
