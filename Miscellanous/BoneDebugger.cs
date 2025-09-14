using UnityEngine;

public class BoneDebugger : MonoBehaviour
{
    public Transform rootObject;

    void Start()
    {
        if (rootObject == null)
        {
            Debug.LogWarning("Assign rootObject in Inspector!");
            return;
        }

        // foreach (Transform t in rootObject.GetComponentsInChildren<Transform>(true))
        // {
        //     Debug.Log("📍 Bone: [" + t.name + "] - Path: " + GetFullPath(t));
        // }
    }

    string GetFullPath(Transform current)
    {
        string path = current.name;
        while (current.parent != null && current.parent != rootObject)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }
        return path;
    }
}
