using System;
using System.Threading.Tasks;
using Mirror;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;

public class WinningConditions : NetworkBehaviour
{
    public static WinningConditions Instance;

    [SyncVar]
    [SerializeField]
    private int blueTeamScore = 0;

    [SyncVar]
    [SerializeField]
    private int redTeamScore = 0;

    [SyncVar]
    [SerializeField]
    public string winningTeam;
    private bool hasHandledTimeOut = false;

    [SyncVar]
    [SerializeField]
    private int maxScore = 3; // Set the maximum score needed to wins

    [SyncVar]
    public bool hasMatchWonOrLost;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // Prevent duplicates
        }
    }

    [Server]
    public void AddScore(string team)
    {
        if (team == "Blue")
            blueTeamScore++;
        else if (team == "Red")
            redTeamScore++;

        CheckWinCondition();
    }

    void Update()
    {
        if (MatchTimer.Instance.matchTimeRanOut && !hasHandledTimeOut)
        {
            hasHandledTimeOut = true;
            TimeRanOut();
        }
    }

    [Server]
    void TimeRanOut()
    {
        if (blueTeamScore > redTeamScore)
        {
            BlueTeamWins();
        }
        else if (redTeamScore > blueTeamScore)
        {
            RedTeamWins();
        }
        else
        {
            Draw();
        }
    }

    [Server]
    void CheckWinCondition()
    {
        if (blueTeamScore >= maxScore && redTeamScore >= maxScore)
        {
            Debug.LogWarning("Both teams reached the maximum score. It's a draw!");
            Draw();
        }
        else if (blueTeamScore >= maxScore)
        {
            BlueTeamWins();
        }
        else if (redTeamScore >= maxScore)
        {
            RedTeamWins();
        }
    }

    [Server]
    public async void EndMatchServerSide(string result)
    {
        string matchId = Guid.NewGuid().ToString();
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        foreach (var ps in PlayerStats.allPlayers)
        {
            string userId = PlayerAuthCache.GetUserId(ps.connectionToClient.connectionId);
            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogWarning($"[MatchSave] No Firebase UID for {ps.PlayerName}");
                continue;
            }

            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogWarning($"[MatchSave] No UID for {ps?.PlayerName}");
                continue;
            }

            if (ps == null)
            {
                Debug.LogWarning("[MatchSave] Null PlayerStats object!");
                continue;
            }

            if (string.IsNullOrEmpty(ps.PlayerName))
            {
                Debug.LogWarning("[MatchSave] PlayerName is null!");
                continue;
            }

            await FirebaseManager.Instance.SaveMatchResultAsync(
                userId,
                matchId,
                ps.PlayerName,
                ps.Team,
                ps.Kills,
                ps.Deaths,
                result,
                timestamp
            );
        }

        // ✅ Only after all saves, notify clients
        RpcEndMatch(result);
    }

    [ClientRpc]
    void RpcEndMatch(string result)
    {
        Debug.Log("Match ended: " + result);
        FlagAudioManager.Instance.RpcPlayFlagSound("EndGameMusic", winningTeam);
        //stop all movements                            DONE   playerController.cs/PlayerMotor.cs/PlayerShoot.cs
        //stop match timer                              DONE   MatchTimer.cs
        //Ui animation and winning text                 DONE   MatchWinUI.cs/EndGameUI.cs
        //few seconds delay                                 ||
        //final K/D UI board                            DONE   NetworkScoreBoard.cs/PlayerStats.cs
        //few seconds delay                                 ||
        //redirect to selection menu / Disconnection    DONE   SceneChangeHandler.cs/clientDisconnectHandler.cs
        //Call UI functions here

        // === NEW Firebase Save Match Result ===
    }

    void BlueTeamWins()
    {
        Debug.Log("Blue Team Wins!");
        FlagAudioManager.Instance.RpcPlayFlagSound("Winning", "Blue");
        winningTeam = "Blue";
        MatchTimer.Instance.hasMatchEnded = true;
        EndMatchServerSide("Blue");
    }

    void RedTeamWins()
    {
        Debug.Log("Red Team Wins!");
        FlagAudioManager.Instance.RpcPlayFlagSound("Winning", "Red");
        winningTeam = "Red";
        MatchTimer.Instance.hasMatchEnded = true;
        EndMatchServerSide("Red");
    }

    void Draw()
    {
        Debug.Log("Draw!");
        winningTeam = null;
        MatchTimer.Instance.hasMatchEnded = true;
        EndMatchServerSide("Draw");
    }
}
