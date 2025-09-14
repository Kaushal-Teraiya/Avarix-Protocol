using System.Threading;
using Unity.VisualScripting;

[System.Serializable]
public class PlayerData
{
    public string userID = "";
    public string username = "";
    public string email = "";
    public int XP = 0;
    public int level = 1;
    public int kills = 0;
    public int deaths = 0;
    public int coins = 0; // main currency
    public int aetherShards = 0; // rare currency
    public long createdAt = 0;
    public long lastLogin = 0;
    public string currentSessionId = null;

    // Required for Firestore
    public PlayerData() { }
}
