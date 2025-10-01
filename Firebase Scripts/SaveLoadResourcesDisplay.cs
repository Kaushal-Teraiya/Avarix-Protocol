using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;

public class ResourceUIManager : MonoBehaviour
{
    public TMPro.TextMeshProUGUI credits;
    public TMPro.TextMeshProUGUI shardsText;
    public TMPro.TextMeshProUGUI xpText;
    public TMPro.TextMeshProUGUI levelText;

    private ListenerRegistration listener;

    private void Start()
    {
        string userId = FirebaseInit.Instance.auth.CurrentUser.UserId;
        ListenToPlayerData(userId);
    }

    private void OnDestroy()
    {
        if (listener != null)
            listener.Stop();
    }

    private void ListenToPlayerData(string userId)
    {
        string userID = FirebaseInit.Instance.auth.CurrentUser.UserId;
        var docRef = FirebaseInit.Instance.db.Collection("users").Document(userID);

        listener = docRef.Listen(snapshot =>
        {
            if (snapshot.Exists)
            {
                PlayerData data = snapshot.ConvertTo<PlayerData>();
                UpdateUI(data);
            }
        });
    }

    private void UpdateUI(PlayerData data)
    {
        credits.text = data.coins.ToString();
        shardsText.text = data.aetherShards.ToString();
        xpText.text = data.XP.ToString();
        levelText.text = "Lvl " + data.level;
    }
}
