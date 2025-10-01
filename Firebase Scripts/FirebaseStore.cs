using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Firebase.Auth;
using Firebase.Firestore;
using kcp2k;
//using Mono.Cecil.Cil;
using Unity.AppUI.UI;
using UnityEngine;

public class FirebaseStore : MonoBehaviour
{
    FirebaseAuth auth;
    FirebaseFirestore db;
    public StoreManager storeManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        storeManager = FindFirstObjectByType<StoreManager>();
        auth = FirebaseInit.Instance.auth;
        db = FirebaseInit.Instance.db;
        if (auth == null || db == null)
        {
            Debug.LogWarning(
                "firebaseinit script needs to run first , make sure to load the store scene after logging in/or from the character selection scene using store button"
            );
        }
    }

    public void BuyBtn(string itemId, StoreItemUI itemUI)
    {
        _ = BuyItem(itemId, itemUI);
    }

    private async Task BuyItem(string itemId, StoreItemUI itemUI)
    {
        string userId = auth.CurrentUser.UserId;
        DocumentReference userRef = db.Collection("users").Document(userId);
        DocumentReference itemRef = db.Collection("storeItems").Document(itemId);

        try
        {
            int newConsumableCount = 0;
            bool isConsumable = false;

            await db.RunTransactionAsync(async transaction =>
            {
                DocumentSnapshot userSnapshot = await transaction.GetSnapshotAsync(userRef);
                DocumentSnapshot itemSnapshot = await transaction.GetSnapshotAsync(itemRef);

                int playerCoins = userSnapshot.GetValue<int>("coins");
                int shards = userSnapshot.GetValue<int>("aetherShards");
                int price = itemSnapshot.GetValue<int>("Price");
                string currency = itemSnapshot.GetValue<string>("currency");
                isConsumable = itemSnapshot.GetValue<bool>("isConsumable");

                Debug.Log(
                    $"available coins: {playerCoins} , price: {price} , currency: {currency} , isConsumable: {isConsumable}"
                );

                // Deduct currency
                if (currency == "Coins")
                {
                    if (playerCoins < price)
                        throw new Exception("Not enough coins");
                    playerCoins -= price;
                }
                else if (currency == "Shards")
                {
                    if (shards < price)
                        throw new Exception("Not enough shards");
                    shards -= price;
                }

                if (isConsumable)
                {
                    // Get current consumables or init new dict
                    Dictionary<string, object> consumables = userSnapshot.ContainsField(
                        "consumables"
                    )
                        ? new Dictionary<string, object>(
                            userSnapshot.GetValue<Dictionary<string, object>>("consumables")
                        )
                        : new Dictionary<string, object>();

                    if (consumables.ContainsKey(itemId))
                    {
                        int current = System.Convert.ToInt32(consumables[itemId]);
                        newConsumableCount = current + 1;
                        consumables[itemId] = newConsumableCount;
                    }
                    else
                    {
                        newConsumableCount = 1;
                        consumables[itemId] = newConsumableCount;
                    }

                    transaction.Update(
                        userRef,
                        new Dictionary<string, object>
                        {
                            { "coins", playerCoins },
                            { "aetherShards", shards },
                            { "consumables", consumables },
                        }
                    );

                    Debug.Log($"Purchased consumable {itemId}, new count: {newConsumableCount}");
                }
                else
                {
                    List<string> ownedItems = userSnapshot.ContainsField("ownedItems")
                        ? new List<string>(userSnapshot.GetValue<List<string>>("ownedItems"))
                        : new List<string>();

                    if (ownedItems.Contains(itemId))
                        throw new Exception("You already own this item!");

                    ownedItems.Add(itemId);

                    transaction.Update(
                        userRef,
                        new Dictionary<string, object>
                        {
                            { "coins", playerCoins },
                            { "aetherShards", shards },
                            { "ownedItems", ownedItems },
                        }
                    );

                    Debug.Log($"Purchased permanent item: {itemId}");
                }
            });

            // ✅ Update UI outside transaction (safe from retries)
            if (isConsumable && newConsumableCount > 0)
            {
                itemUI.UpdateOwnedCount(newConsumableCount);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Purchase failed: {e.Message}");
        }
    }

    // Update is called once per frame
    void Update() { }
}
