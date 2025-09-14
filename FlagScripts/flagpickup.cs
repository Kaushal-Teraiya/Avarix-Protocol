using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class FlagHandler : NetworkBehaviour
{
    public Transform flagHolder;

    [SyncVar(hook = nameof(OnTeamChanged))]
    public string Team = null;
    public FlagAudioManager audioManager;
    public player _player;
    public static FlagHandler local;

    private void Start()
    {
        audioManager = FindAnyObjectByType<FlagAudioManager>();
        _player = GetComponent<player>();
        if (audioManager == null)
        {
            Debug.LogError("[FlagHandler] AudioManager is NULL! Sound will not play.");
        }
        else
        {
            Debug.Log("[FlagHandler] AudioManager successfully assigned.");
        }
    }

    private void OnTeamChanged(string oldTeam, string newTeam)
    {
        Debug.Log($"[FlagHandler] Team changed from {oldTeam} to {newTeam} for {netId}");
    }

    [SyncVar(hook = nameof(OnHeldFlagChanged))]
    public GameObject heldFlag = null;

    public override void OnStartClient()
    {
        base.OnStartClient();
        StartCoroutine(DelayedTeamCheck());
        Debug.Log($"[FlagHandler] Player {netId} joined as {Team}");
    }

    public override void OnStartLocalPlayer()
    {
        local = this;
    }

    private IEnumerator DelayedTeamCheck()
    {
        yield return new WaitForSeconds(1f);
        NetworkGamePlayerLobby player = GetComponent<NetworkGamePlayerLobby>();

        if (player != null)
        {
            Debug.Log($"[FlagHandler] Detected team assignment: {player.Team}");
            Team = player.Team;
        }
    }

    private void OnHeldFlagChanged(GameObject oldFlag, GameObject newFlag)
    {
        if (newFlag != null)
        {
            newFlag.transform.SetParent(flagHolder);
            newFlag.transform.localPosition = Vector3.zero;
            newFlag.transform.localRotation = Quaternion.identity;
            newFlag.GetComponent<Rigidbody>().isKinematic = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (heldFlag == null)
        {
            if (
                other.CompareTag("BlueFlag")
                && Team == "Red"
                && GameManager.instance.canPickUpBlueFlag
                && !_player.isDead
            )
            {
                CmdPickFlag(other.gameObject);
            }
            else if (
                other.CompareTag("RedFlag")
                && Team == "Blue"
                && GameManager.instance.canPickUpRedFlag
                && !_player.isDead
            )
            {
                CmdPickFlag(other.gameObject);
            }
        }

        if (
            other.CompareTag("BlueFlag")
            && Team == "Blue"
            && GameManager.instance.isStolenBlue
            && GameManager.instance.canPickUpBlueFlag
        )
        {
            CmdReturnFlag(other.gameObject, "Blue");
        }
        else if (
            other.CompareTag("RedFlag")
            && Team == "Red"
            && GameManager.instance.isStolenRed
            && GameManager.instance.canPickUpRedFlag
        )
        {
            CmdReturnFlag(other.gameObject, "Red");
        }
        if (heldFlag != null)
        {
            if (
                other.CompareTag("BL")
                && Team == "Blue"
                && heldFlag.CompareTag("RedFlag")
                && !GameManager.instance.isStolenBlue
            )
            {
                CmdCaptureFlag("Blue");
            }
            else if (
                other.CompareTag("RL")
                && Team == "Red"
                && heldFlag.CompareTag("BlueFlag")
                && !GameManager.instance.isStolenRed
            )
            {
                CmdCaptureFlag("Red");
            }
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdPickFlag(GameObject flag)
    {
        if (flag == null || heldFlag != null)
            return;

        heldFlag = flag;
        flag.transform.SetParent(transform);
        flag.transform.localPosition = Vector3.zero;
        flag.transform.localRotation = Quaternion.identity;
        flag.GetComponent<Rigidbody>().isKinematic = true;

        if (flag.CompareTag("RedFlag"))
        {
            CmdSetFlagStolen("Red", true);
            GameManager.instance.canPickUpRedFlag = false;
        }
        else if (flag.CompareTag("BlueFlag"))
        {
            CmdSetFlagStolen("Blue", true);
            GameManager.instance.canPickUpBlueFlag = false;
        }

        RpcPickFlag(flag);
        Debug.Log($"[FlagHandler] Flag picked up: {flag.tag}");

        if (audioManager != null)
        {
            Debug.Log($"[FlagHandler] Playing sound for flag pickup ({Team})");
            audioManager.RpcPlayFlagSound("FlagTaken", Team);
        }
        else
        {
            Debug.LogError(
                "[FlagHandler] AudioManager is NULL when attempting to play FlagTaken sound."
            );
        }
    }

    [ClientRpc]
    private void RpcPickFlag(GameObject flag)
    {
        if (flag == null)
            return;

        flag.transform.SetParent(flagHolder);
        flag.transform.localPosition = Vector3.zero;
        flag.transform.localRotation = Quaternion.identity;
        flag.GetComponent<Rigidbody>().isKinematic = true;
    }

    [Command(requiresAuthority = false)]
    public void CmdReturnFlag(GameObject flag, string flagColor)
    {
        CmdSetFlagStolen(flagColor, false);

        flag.transform.SetParent(null);
        flag.transform.position = flag.GetComponent<Flag>().originalPosition;
        flag.GetComponent<Rigidbody>().isKinematic = false;

        if (heldFlag == flag)
            heldFlag = null;

        RpcReturnFlag(flag);
        Debug.Log($"[FlagHandler] Flag returned: {flagColor}");

        if (audioManager != null)
        {
            Debug.Log($"[FlagHandler] Playing sound for flag return ({Team})");
            audioManager.RpcPlayFlagSound("FlagReturned", Team);
        }
        else
        {
            Debug.LogError(
                "[FlagHandler] AudioManager is NULL when attempting to play FlagReturned sound."
            );
        }
    }

    [ClientRpc]
    private void RpcReturnFlag(GameObject flag)
    {
        flag.transform.SetParent(null);
        flag.transform.position = flag.GetComponent<Flag>().originalPosition;
        flag.GetComponent<Rigidbody>().isKinematic = false;
    }

    [Command(requiresAuthority = false)]
    public void CmdDropFlag(Vector3 dropPosition)
    {
        if (heldFlag == null)
        {
            Debug.Log("[FlagHandler] CmdDropFlag called but no flag is held.");
            return;
        }

        Debug.Log($"[FlagHandler] Dropping flag: {heldFlag.name}");

        GameObject flagToDrop = heldFlag;
        heldFlag = null;
        flagToDrop.transform.SetParent(null);
        flagToDrop.transform.position = dropPosition;
        flagToDrop.GetComponent<Rigidbody>().isKinematic = false;
        StartCoroutine(PickupCooldown());
        RpcDropFlag(flagToDrop, dropPosition);

        if (audioManager != null)
        {
            Debug.Log($"[FlagHandler] Playing sound for flag drop ({Team})");
            audioManager.RpcPlayFlagSound("FlagDropped", Team);
        }
        else
        {
            Debug.LogError(
                "[FlagHandler] AudioManager is NULL when attempting to play FlagDropped sound."
            );
        }
    }

    [ClientRpc]
    private void RpcDropFlag(GameObject flag, Vector3 dropPosition)
    {
        if (flag == null)
        {
            Debug.Log("[FlagHandler] RpcDropFlag received null flag!");
            return;
        }

        Debug.Log($"[FlagHandler] RpcDropFlag received: {flag.name}");

        flag.transform.SetParent(null);
        flag.transform.position = dropPosition;
        flag.GetComponent<Rigidbody>().isKinematic = false;
        if (flag.CompareTag("BlueFlag"))
        {
            GameManager.instance.isDroppedBlue = true;
        }
        else
        {
            GameManager.instance.isDroppedRed = true;
        }
        StartCoroutine(PickupCooldown());
    }

    [Command(requiresAuthority = false)]
    public void CmdCaptureFlag(string team)
    {
        if (heldFlag == null)
            return;

        Debug.Log($"[FlagHandler] {team} captured the flag! Resetting flags...");
        heldFlag = null;

        GameManager.instance.CmdSetFlagStolen("Red", false);
        GameManager.instance.CmdSetFlagStolen("Blue", false);
        GameManager.instance.CmdSetFlagDropped("Red", false);
        GameManager.instance.CmdSetFlagDropped("Blue", false);
        GameManager.instance.canPickUpRedFlag = true;
        GameManager.instance.canPickUpBlueFlag = true;

        RpcResetFlags();
        WinningConditions.Instance.AddScore(team);

        if (audioManager != null)
        {
            Debug.Log($"[FlagHandler] Playing sound for flag capture ({Team})");
            audioManager.RpcPlayFlagSound("FlagCaptured", Team);
        }
        else
        {
            Debug.LogError(
                "[FlagHandler] AudioManager is NULL when attempting to play FlagCaptured sound."
            );
        }
    }

    [ClientRpc]
    private void RpcResetFlags()
    {
        foreach (GameObject flag in GameObject.FindGameObjectsWithTag("RedFlag"))
        {
            flag.transform.position = flag.GetComponent<Flag>().originalPosition;
            flag.transform.SetParent(null);
        }

        foreach (GameObject flag in GameObject.FindGameObjectsWithTag("BlueFlag"))
        {
            flag.transform.position = flag.GetComponent<Flag>().originalPosition;
            flag.transform.SetParent(null);
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdSetFlagStolen(string flagColor, bool state)
    {
        if (GameManager.instance == null)
            return;
        StartCoroutine(WaitAndSendFlagStolen(flagColor, state));

        //GameManager.instance.CmdSetFlagStolen(flagColor, state);
    }

    private IEnumerator WaitAndSendFlagStolen(string teamName, bool stolen)
    {
        while (!NetworkClient.ready)
        {
            Debug.Log("[FlagHandler] Waiting for NetworkClient to be ready...");
            yield return new WaitForSeconds(0.5f);
        }

        GameManager.instance.CmdSetFlagStolen(teamName, stolen);
    }

    private IEnumerator PickupCooldown()
    {
        GameManager.instance.canPickUpRedFlag = false;
        GameManager.instance.canPickUpBlueFlag = false;
        yield return new WaitForSeconds(0.1f);
        GameManager.instance.canPickUpRedFlag = true;
        GameManager.instance.canPickUpBlueFlag = true;
    }

    public static Transform GetFlagTransform(string team)
    {
        if (team == "Red")
            return GameObject.FindWithTag("RedFlag")?.transform;
        else if (team == "Blue")
            return GameObject.FindWithTag("BlueFlag")?.transform;

        return null;
    }

    public bool HasFlag()
    {
        if (heldFlag != null)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
