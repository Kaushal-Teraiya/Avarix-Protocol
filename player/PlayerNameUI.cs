using Mirror;
using TMPro;
using Unity.Collections;
using UnityEngine;

public class PlayerNameUI : NetworkBehaviour
{
    public FlagHandler flagHandler;
    public TextMeshPro textMesh; // Assign in Inspector
    public Transform target; // Assign Player Transform in Inspector
    public Color teamBlueColor = Color.blue;
    public Color teamRedColor = Color.red;

    public Camera mainCam;

    void Start()
    {
        flagHandler = GetComponent<FlagHandler>();
        Invoke(nameof(FindCamera), 0.5f); // Delay to ensure camera is assigned
    }

    void FindCamera()
    {
        mainCam = Camera.main;
        if (mainCam == null)
        {
            mainCam = GameObject.FindFirstObjectByType<Camera>();
        }
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        if (mainCam != null)
        {
            transform.LookAt(mainCam.transform);
            transform.Rotate(0, 180, 0); // Invert text to face correctly
        }
        // Face the Camera
    }

    public virtual void SetPlayerName(string playerName, string teamName)
    {
        textMesh.text = playerName;

        textMesh.color = teamName == "Blue" ? teamBlueColor : teamRedColor;
    }
}
