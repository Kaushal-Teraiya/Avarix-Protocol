using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager instance; // Singleton for easy access

    private List<FlagHandler> players = new List<FlagHandler>();

    void Awake()
    {
        if (instance == null)
            instance = this;
    }

    public void RegisterPlayer(FlagHandler player)
    {
        if (!players.Contains(player))
        {
            players.Add(player);
        }
    }

    [Server] // Only the host should run this
    public void AssignTeams()
    {
        if (players.Count < 2)
            return; // Minimum players required

        // Shuffle the player list to randomize teams
        List<FlagHandler> shuffledPlayers = new List<FlagHandler>(players);
        shuffledPlayers.Sort((a, b) => Random.value.CompareTo(Random.value));

        int half = shuffledPlayers.Count / 2;

        for (int i = 0; i < shuffledPlayers.Count; i++)
        {
            shuffledPlayers[i].Team = (i < half) ? "Red" : "Blue";
        }
    }
}
