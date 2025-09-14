using System;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Firestore;
using Mirror;
using UnityEngine;

public class MatchResultHandler : NetworkBehaviour
{
    [Server] // run only on server/host
    public void SaveMatchResults(string matchId)
    {
        FirebaseFirestore db = FirebaseInit.Instance.db;
        FirebaseAuth auth = FirebaseInit.Instance.auth;

        foreach (var p in PlayerStats.allPlayers)
        {
            PlayerData update = new PlayerData
            {
                userID = auth.CurrentUser.UserId, // or auth.CurrentUser.UserId if mapped
                kills = p.Kills,
                deaths = p.Deaths,
                XP = CalculateXP(p.Kills, p.Deaths),
                coins = CalculateCoins(p.Kills, p.Deaths),
                aetherShards = UnityEngine.Random.Range(0, 2), // rare drop chance
                lastLogin = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                currentSessionId = matchId,
            };

            // Merge with existing Firestore doc
            DocumentReference docRef = db.Collection("players").Document(update.userID);

            Dictionary<string, object> updates = new Dictionary<string, object>
            {
                { "kills", FieldValue.Increment(update.kills) },
                { "deaths", FieldValue.Increment(update.deaths) },
                { "XP", FieldValue.Increment(update.XP) },
                { "coins", FieldValue.Increment(update.coins) },
                { "aetherShards", FieldValue.Increment(update.aetherShards) },
                { "lastLogin", update.lastLogin },
                { "currentSessionId", update.currentSessionId },
            };

            docRef
                .SetAsync(updates, SetOptions.MergeAll)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        Debug.LogError($"Failed to save {update.userID}: {t.Exception}");
                    else
                        Debug.Log($"Updated stats for {update.userID}");
                });
        }
    }

    private int CalculateXP(int kills, int deaths)
    {
        return (kills * 100) - (deaths * 20);
    }

    private int CalculateCoins(int kills, int deaths)
    {
        return (kills * 10); // simple example
    }
}
