using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField]
    private NetworkManagerLobby networkManager = null;

    [Header("UI")]
    [SerializeField]
    private GameObject loadingPagePanel = null;

    public void HostLobby()
    {
        networkManager.StartHost();
        loadingPagePanel.SetActive(false);
    }

    public void Server()
    {
        networkManager.StartServer();
        loadingPagePanel.SetActive(false);
    }
}
