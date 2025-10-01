using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Firebase.Auth;
using Firebase.Firestore;
using kcp2k;
using UnityEngine;
using UnityEngine.UI;

public class StoreManager : MonoBehaviour
{
    public GameObject storeButtonPrefab;
    public Transform contentParent; // where buttons go

    private FirebaseStore firebaseStore;
    private FirebaseAuth auth;
    private FirebaseFirestore db;

    private void Start()
    {
        firebaseStore = gameObject.GetComponent<FirebaseStore>();
        LoadStoreItems();
    }

    async void LoadStoreItems()
    {
        try
        {
            auth = FirebaseInit.Instance.auth;
            db = FirebaseInit.Instance.db;
            string userId = auth.CurrentUser.UserId;
            QuerySnapshot itemSnapshot = await FirebaseInit
                .Instance.db.Collection("storeItems")
                .GetSnapshotAsync();

            DocumentSnapshot userSnapShot = await db.Collection("users")
                .Document(userId)
                .GetSnapshotAsync();

            foreach (DocumentSnapshot doc in itemSnapshot.Documents)
            {
                string itemId = doc.Id; // THIS is the dynamic itemId
                string itemName = doc.GetValue<string>("Name");
                int price = doc.GetValue<int>("Price");
                bool isConsumable = doc.GetValue<bool>("isConsumable");
                string currency = doc.GetValue<string>("currency");
                Dictionary<string, object> consumables = userSnapShot.ContainsField("consumables")
                    ? userSnapShot.GetValue<Dictionary<string, object>>("consumables")
                    : new Dictionary<string, object>();
                int ownedCount = 0;

                if (consumables.ContainsKey(itemId))
                {
                    ownedCount = System.Convert.ToInt32(consumables[itemId]);
                }

                // int availableCount = doc.GetValue<int>("Owned");

                // Instantiate button prefab
                GameObject buttonObj = Instantiate(storeButtonPrefab, contentParent);
                Button button = buttonObj.GetComponentInChildren<Button>();
                StoreItemUI itemUI = buttonObj.GetComponent<StoreItemUI>();

                // Set UI text
                itemUI.SetItem(itemName, price, ownedCount, itemId, isConsumable, currency); // owned count will be updated later

                // Add listener dynamically using the itemId
                button.onClick.AddListener(() => firebaseStore.BuyBtn(itemId, itemUI));
            }
        }
        catch (Exception e)
        {
            Debug.Log("error:" + e.Message);
        }
    }
}
