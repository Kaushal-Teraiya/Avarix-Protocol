using UnityEngine;
using Mirror;

public class RoomPlayer : NetworkRoomPlayer
{
    [SyncVar]
    public int selectedCharacterIndex; // Syncs the character index across the network

    public void SetCharacterIndex(int index)
    {
        selectedCharacterIndex = index;
    }

    public override void OnStartClient()
    {
        if (isLocalPlayer)
        {
            selectedCharacterIndex = PlayerPrefs.GetInt("SelectedCharacterIndex", 0);
            CmdSetCharacterIndex(selectedCharacterIndex);
        }
    }

    [Command]
    void CmdSetCharacterIndex(int index)
    {
        selectedCharacterIndex = index;
    }
}
