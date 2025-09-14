using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class XRayVision : NetworkBehaviour
{
    public LayerMask xRayLayer;
    public GameObject xRayQuadPrefab; // Assign a Quad prefab in the Inspector
    private bool xRayActive = false;
    private GameObject xRayQuadInstance;

    void Update()
    {
        if (!isLocalPlayer) return; // Only local player can toggle X-ray

        if (Input.GetKeyDown(KeyCode.J))
        {
            ToggleXRayVision();
        }
    }

    void ToggleXRayVision()
    {
        xRayActive = !xRayActive; // Toggle X-ray state

        foreach (var netIdentity in NetworkClient.spawned.Values)
        {
            if (!netIdentity.isLocalPlayer && netIdentity.CompareTag("Player")) // Only affect other players
            {
                int layerNum = xRayActive ? (int)Mathf.Log(xRayLayer.value, 2) : LayerMask.NameToLayer("Default");
                netIdentity.gameObject.layer = layerNum;

                if (netIdentity.transform.childCount > 0)
                    SetLayerAllChildren(netIdentity.transform, layerNum);
            }
        }

        // Toggle the Quad for local player
        if (xRayActive)
        {
            if (xRayQuadInstance == null)
            {
                xRayQuadInstance = Instantiate(xRayQuadPrefab);
                xRayQuadInstance.transform.SetParent(Camera.main.transform, false);
                xRayQuadInstance.transform.localPosition = new Vector3(0, 0, 0.7f); // Adjust if needed
            }
            xRayQuadInstance.SetActive(true);
        }
        else
        {
            if (xRayQuadInstance != null)
                xRayQuadInstance.SetActive(false);
        }
    }

    void SetLayerAllChildren(Transform root, int layer)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(includeInactive: true))
        {
            child.gameObject.layer = layer;
        }
    }
}
