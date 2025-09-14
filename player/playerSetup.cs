using Mirror;
using UnityEngine;

//using Unity.Netcode;
//using UnityEngine.Networking;

[RequireComponent(typeof(player))]
[RequireComponent(typeof(playerController))]
public class playerSetup : NetworkBehaviour
{
    [SerializeField]
    Behaviour[] componentsToDisable;

    [SerializeField]
    string remoteLayerName = "RemotePlayer";

    [SerializeField]
    string dontDrawLayerName = "DontDraw";

    [SerializeField]
    GameObject PlayerGraphics;

    [SerializeField]
    GameObject PlayerUIPrefab;

    [HideInInspector]
    public GameObject playerUIInstance;

    void Start()
    {
        if (!isLocalPlayer)
        {
            DisableComponents();
            //  AssingRemoteLayer();
        }
        else
        {
            SetLayerRecursively(PlayerGraphics, LayerMask.NameToLayer(dontDrawLayerName));
            playerUIInstance = Instantiate(PlayerUIPrefab);
            playerUIInstance.name = PlayerUIPrefab.name;

            PlayerUI ui = playerUIInstance.GetComponent<PlayerUI>();
            if (ui == null)
            {
                Debug.LogError("no playerUI on playerUI prefab");
            }
            ui.setController(GetComponent<playerController>());
            GetComponent<player>().PlayerSetup();
        }
    }

    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        // Set the layer only if this object is NOT tagged as "ShadowMesh"
        if (!obj.CompareTag("ShadowMesh"))
        {
            obj.layer = newLayer;
        }

        // Now iterate through children and apply the function recursively
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        string _netID = GetComponent<NetworkIdentity>().netId.ToString();
        player _player = GetComponent<player>();
        GameManager.RegisterPlayer(_netID, _player);
        if (_player == null || _netID == null)
        {
            Debug.Log("player not found");
        }
    }

    void AssingRemoteLayer()
    {
        gameObject.layer = LayerMask.NameToLayer(remoteLayerName);
    }

    void DisableComponents()
    {
        for (int i = 0; i < componentsToDisable.Length; i++)
        {
            componentsToDisable[i].enabled = false;
        }
    }

    void OnDisable()
    {
        Destroy(playerUIInstance);

        if (isLocalPlayer)
        {
            GameManager.instance.setSceneCameraActive(true);
        }

        GameManager.UnRegisterPlayer(transform.name);
    }
}
