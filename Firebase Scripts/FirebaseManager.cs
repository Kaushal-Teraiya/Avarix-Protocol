using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using Firebase.Functions;
using UnityEngine;

public class FirebaseManager : MonoBehaviour
{
    FirebaseAuth auth;
    FirebaseFirestore db;
    FirebaseFunctions functions;
    public static FirebaseManager Instance;

    private void Awake()
    {
        auth = FirebaseInit.Instance.auth;
        db = FirebaseInit.Instance.db;
        functions = FirebaseFunctions.DefaultInstance;
    }

    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // public void RequestMatchReward(int kills, int deaths, string matchId)
    // {
    //     var data = new Dictionary<string, object>
    //     {
    //         { "userID", auth.CurrentUser.UserId },
    //         { "kills", kills },
    //         { "deaths", deaths },
    //         { "matchID", matchId },
    //     };

    //     functions
    //         .GetHttpsCallable("calculateMatchReward")
    //         .CallAsync(data)
    //         .ContinueWithOnMainThread(task =>
    //         {
    //             if (task.IsFaulted || task.IsCanceled)
    //             {
    //                 Debug.LogError($"Reward request failed: {task.Exception}");
    //             }
    //             else
    //             {
    //                 var result = task.Result.Data as Dictionary<string, object>;
    //                 Debug.Log(
    //                     $"Rewards: +{result["coins"]} coins, +{result["aetherShards"]} shards"
    //                 );
    //             }
    //         });
    // }


    public async Task SaveMatchResultAsync(
        string userId,
        string matchId,
        string playerName,
        string team,
        int kills,
        int deaths,
        string matchResult,
        long timestamp
    )
    {
        try
        {
            int xp = CalculateXP(kills, deaths);
            int coins = CalculateCoins(kills, deaths);
            int shards = UnityEngine.Random.Range(0, 2);

            // Update player profile
            Dictionary<string, object> updates = new Dictionary<string, object>
            {
                { "kills", FieldValue.Increment(kills) },
                { "deaths", FieldValue.Increment(deaths) },
                { "XP", FieldValue.Increment(xp) },
                { "coins", FieldValue.Increment(coins) },
                { "aetherShards", FieldValue.Increment(shards) },
                { "currentMatchId", matchId },
            };

            if (db == null)
            {
                Debug.Log("[FirebaseManager] Firestore DB is null!");
                db = FirebaseInit.Instance.db;
                Debug.Log($"[FirebaseManager] Reinitialized Firestore DB: {db != null}");
            }

            if (db == null)
            {
                Debug.LogError("[FirebaseManager] Firestore DB is still null. Aborting save.");
                return;
            }

            await db.Collection("users").Document(userId).SetAsync(updates, SetOptions.MergeAll);

            // Save match history record (per match)
            var matchData = new Dictionary<string, object>
            {
                { "userId", userId },
                { "playerName", playerName },
                { "team", team },
                { "kills", kills },
                { "deaths", deaths },
                { "xpEarned", xp },
                { "coinsEarned", coins },
                { "shardsEarned", shards },
                { "matchResult", matchResult },
                { "matchId", matchId },
                { "timestamp", Timestamp.GetCurrentTimestamp() },
            };

            await db.Collection("matches").Document(matchId + "_" + userId).SetAsync(matchData);

            Debug.Log($"[FirebaseManager] Saved match result for {playerName} ({userId})");
        }
        catch (Exception e)
        {
            Debug.LogError($"[FirebaseManager] Failed saving match result: {e}");
        }
    }

    private int CalculateXP(int kills, int deaths)
    {
        return (kills * 100) - (deaths * 20);
    }

    private int CalculateCoins(int kills, int deaths)
    {
        return kills * 10; // simple example
    }
}
