using System.Collections.Generic;

public static class PlayerAuthCache
{
    public static Dictionary<int, string> connToUid = new Dictionary<int, string>();

    public static void Register(int connId, string uid)
    {
        connToUid[connId] = uid;
    }

    public static string GetUserId(int connId)
    {
        return connToUid.ContainsKey(connId) ? connToUid[connId] : null;
    }
}
