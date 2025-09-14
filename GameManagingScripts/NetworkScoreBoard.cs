using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NetworkScoreBoard : NetworkBehaviour
{
    private NetworkManagerLobby players;

    [SerializeField]
    private Button hideButton;

    [SerializeField]
    private GameObject scoreBoardUI;

    [SerializeField]
    private TMP_Text[] blueTeamSlots = new TMP_Text[4];

    [SerializeField]
    private TMP_Text[] redTeamSlots = new TMP_Text[4];

    [SerializeField]
    private TMP_Text[] blueTeamkills = new TMP_Text[4];

    [SerializeField]
    private TMP_Text[] blueTeamDeaths = new TMP_Text[4];

    [SerializeField]
    private TMP_Text[] redTeamkills = new TMP_Text[4];

    [SerializeField]
    private TMP_Text[] redTeamDeaths = new TMP_Text[4];

    void Start()
    {
        players = GetComponent<NetworkManagerLobby>();
        StartCoroutine(SetUpScoreBoard());
        InvokeRepeating("CmdUpdateScoreBoard", 0f, 1f);
        Debug.Log("network connections count :  " + NetworkServer.connections.Count);
        Debug.Log("PlayerStats.allPlayers count : " + PlayerStats.allPlayers.Count);
    }

    void Update()
    {
        hideButton.onClick.AddListener(HideBoard);
    }

    private IEnumerator SetUpScoreBoard()
    {
        // Wait until all PlayerStats have been registered
        yield return new WaitUntil(
            () => PlayerStats.allPlayers.Count == NetworkServer.connections.Count
        );

        Debug.Log("Players in allPlayers: " + PlayerStats.allPlayers.Count);
        CmdUpdateScoreBoard();
    }

    [Command(requiresAuthority = false)]
    [Server]
    public void CmdUpdateScoreBoard()
    {
        RpcUpdateScoreBoard();
    }

    [ClientRpc]
    private void RpcUpdateScoreBoard()
    {
        List<PlayerStats> playersToUse = PlayerStats.clientSidePlayers;

        int blueIndex = 0;
        int redIndex = 0;

        for (int i = 0; i < playersToUse.Count; i++)
        {
            PlayerStats player = playersToUse[i];
            string teamColor = player.Team == "Blue" ? "blue" : "red";
            string coloredName = $"<color={teamColor}>{player.PlayerName}</color>";

            if (player.Team == "Blue" && blueIndex < blueTeamSlots.Length)
            {
                blueTeamSlots[blueIndex].text = coloredName;
                blueTeamkills[blueIndex].text = player.Kills.ToString();
                blueTeamDeaths[blueIndex].text = player.Deaths.ToString();
                blueIndex++; // increment only blue index
            }
            else if (player.Team == "Red" && redIndex < redTeamSlots.Length)
            {
                redTeamSlots[redIndex].text = coloredName;
                redTeamkills[redIndex].text = player.Kills.ToString();
                redTeamDeaths[redIndex].text = player.Deaths.ToString();
                redIndex++; // increment only red index
            }
        }
    }

    private void HideBoard()
    {
        scoreBoardUI.SetActive(false);
        hideButton.gameObject.SetActive(false);
    }
}
