using System.Collections;
using Mirror;
using UnityEngine;

public class FlagAudioManager : NetworkBehaviour
{
    [SerializeField]
    private AudioClip yourTeamTakesFlag;

    [SerializeField]
    private AudioClip enemyTeamTakesFlag;

    [SerializeField]
    private AudioClip yourTeamDropsFlag;

    [SerializeField]
    private AudioClip enemyTeamDropsFlag;

    [SerializeField]
    private AudioClip yourTeamReturnsFlag;

    [SerializeField]
    private AudioClip enemyTeamReturnsFlag;

    [SerializeField]
    private AudioClip yourTeamCapturesFlag;

    [SerializeField]
    private AudioClip enemyTeamCapturesFlag;

    [SerializeField]
    private AudioClip yourTeamWins;

    [SerializeField]
    private AudioClip enemyTeamWins;

    [SerializeField]
    public AudioClip winClip;

    [SerializeField]
    public AudioClip loseClip;

    private AudioSource audioSource;
    private FlagHandler localPlayerFlagHandler; // Store local player's team
    public static FlagAudioManager Instance;

    void Awake()
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

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        localPlayerFlagHandler = FindAnyObjectByType<FlagHandler>();

        if (audioSource == null)
        {
            Debug.LogError(
                "[FlagAudioManager] AudioSource is NULL! Attach an AudioSource component."
            );
        }
        else
        {
            Debug.Log("FOUND AUDIO SOURCE");
        }

        StartCoroutine(WaitForLocalPlayer());
    }

    private IEnumerator WaitForLocalPlayer()
    {
        while (localPlayerFlagHandler == null)
        {
            foreach (var player in FindObjectsByType<FlagHandler>(FindObjectsSortMode.None))
            {
                if (player.isOwned)
                {
                    localPlayerFlagHandler = player;
                    Debug.Log(
                        "[FlagAudioManager] Found local player with team: "
                            + localPlayerFlagHandler.Team
                    );
                    yield break; // Exit the coroutine once found
                }
            }

            Debug.Log("[FlagAudioManager] Waiting for local player...");
            yield return new WaitForSeconds(0.5f); // Check every 0.5 seconds
        }
    }

    [ClientRpc]
    public void RpcPlayFlagSound(string eventType, string flagOwnerTeam)
    {
        if (localPlayerFlagHandler == null)
        {
            Debug.LogError("[FlagAudioManager] localPlayerFlagHandler is NULL!");
            return;
        }

        Debug.Log(
            $"[FlagAudioManager] Playing sound: {eventType} for flag owner team: {flagOwnerTeam}"
        );

        AudioClip clipToPlay = null;

        switch (eventType)
        {
            case "FlagTaken":
                clipToPlay =
                    (flagOwnerTeam == localPlayerFlagHandler.Team)
                        ? yourTeamTakesFlag
                        : enemyTeamTakesFlag;
                break;

            case "FlagDropped":
                clipToPlay =
                    (flagOwnerTeam == localPlayerFlagHandler.Team)
                        ? yourTeamDropsFlag
                        : enemyTeamDropsFlag;
                break;

            case "FlagReturned":
                clipToPlay =
                    (flagOwnerTeam == localPlayerFlagHandler.Team)
                        ? yourTeamReturnsFlag
                        : enemyTeamReturnsFlag;
                break;

            case "FlagCaptured":
                clipToPlay =
                    (flagOwnerTeam == localPlayerFlagHandler.Team)
                        ? yourTeamCapturesFlag
                        : enemyTeamCapturesFlag;
                break;
            case "Winning":
                clipToPlay =
                    (flagOwnerTeam == localPlayerFlagHandler.Team) ? yourTeamWins : enemyTeamWins;
                break;
            case "EndGameMusic":
                clipToPlay = (flagOwnerTeam == localPlayerFlagHandler.Team) ? winClip : loseClip;
                break;
        }

        if (clipToPlay != null && audioSource != null)
        {
            audioSource.PlayOneShot(clipToPlay);
            Debug.Log($"[FlagAudioManager] Sound played: {eventType}");
        }
        else
        {
            Debug.LogError($"[FlagAudioManager] Missing AudioClip for event: {eventType}");
        }
    }
}
