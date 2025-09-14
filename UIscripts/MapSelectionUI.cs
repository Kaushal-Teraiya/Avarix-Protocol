using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Collections.Generic;

public class MapSelectionUI : MonoBehaviour
{
    public Dropdown mapDropdown;  // Dropdown to select maps
    public Button confirmButton;  // Button to confirm the selection
    public MapData[] availableMaps;  // List of available maps (assign in Inspector)

    private void Start()
    {
        // Populate dropdown with available maps
        PopulateMapDropdown();

        // Button listener to send map selection
        confirmButton.onClick.AddListener(SendMapSelection);
    }

    private void PopulateMapDropdown()
    {
        mapDropdown.ClearOptions();
        List<string> mapNames = new List<string>();

        foreach (var map in availableMaps)
        {
            mapNames.Add(map.mapName); // Add the map name to the dropdown list
        }

        mapDropdown.AddOptions(mapNames); // Add map names as options in the dropdown
    }

    private void SendMapSelection()
    {
        if (!NetworkClient.active) return;

        // Get the scene name for the selected map
        string selectedMap = availableMaps[mapDropdown.value].mapName;
        NetworkManagerLobby.MapSelectionMessage msg = new NetworkManagerLobby.MapSelectionMessage { mapName = selectedMap };

        // Send the message to the server
        NetworkClient.Send(msg);
        Debug.Log($"📡 Sent map selection: {selectedMap}");
    }
}
