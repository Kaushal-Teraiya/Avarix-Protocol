using System.Collections;
using Mirror;
using TMPro;
using UnityEngine;

public class MatchTimer : NetworkBehaviour
{
    public TextMeshProUGUI timerText;
    public float matchDuration = 300f; // 5 minutes in seconds

    private float currentTime;

    public static MatchTimer Instance;

    [SyncVar]
    public bool matchTimeRanOut = false;

    [SyncVar]
    public bool hasMatchEnded = false;

    void Start()
    {
        // Try to auto-find timerText if it's not set
        if (timerText == null)
        {
            timerText = GameObject.Find("matchTimer")?.GetComponent<TextMeshProUGUI>();
            if (timerText == null)
            {
                Debug.LogError("MatchTimer: Couldn't find timerText on client!");
            }
        }

        if (isServer)
        {
            currentTime = matchDuration;
            StartCoroutine(UpdateTimer());
        }
    }

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

    // This will run only on the server
    IEnumerator UpdateTimer()
    {
        while (currentTime > 0)
        {
            // Update the timer and sync it across all clients
            int minutes = Mathf.FloorToInt(currentTime / 60f);
            int seconds = Mathf.FloorToInt(currentTime % 60f);

            // Call Rpc function to update all clients
            RpcUpdateTimer(minutes, seconds);

            yield return new WaitForSeconds(1f); // update every second
            currentTime -= 1f;
            if (currentTime <= 0)
            {
                hasMatchEnded = true;
            }
        }

        // After match ends, set the timer to 00:00 and notify all clients
        RpcUpdateTimer(0, 0);

        // Call EndMatch() or any other logic you want for when the match ends
        EndMatch();
    }

    // [ClientRpc] to update all clients' timer text
    [ClientRpc]
    void RpcUpdateTimer(int minutes, int seconds)
    {
        if (hasMatchEnded)
            return;

        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // Function to handle what happens when the match ends
    void EndMatch()
    {
        // You can add logic here for what should happen at the end of the match
        matchTimeRanOut = true;
        Debug.Log("Match Ended!");
    }
}
