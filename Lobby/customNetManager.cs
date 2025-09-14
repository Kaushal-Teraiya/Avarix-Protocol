using UnityEngine;
using Mirror;

public class CustomNetworkManager : NetworkRoomManager
{
    public GameObject[] characterPrefabs; // Assign different character prefabs in Inspector

    public override void OnRoomServerAddPlayer(NetworkConnectionToClient conn)
    {
        // Find the corresponding Room Player
        RoomPlayer roomPlayer = conn.identity.GetComponent<RoomPlayer>();

        // Get the selected character index from Room Player
        int selectedCharacterIndex = roomPlayer.selectedCharacterIndex;

        // Instantiate the correct character prefab
        GameObject gamePlayerPrefab = Instantiate(characterPrefabs[selectedCharacterIndex]);

        // Spawn the game player
        NetworkServer.ReplacePlayerForConnection(conn, gamePlayerPrefab , ReplacePlayerOptions.KeepAuthority);
    }
}
