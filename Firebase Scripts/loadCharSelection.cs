using UnityEngine;
using UnityEngine.SceneManagement;

public class loadCharSelection : MonoBehaviour
{
    public void LoadCharSelScene()
    {
        SceneManager.LoadScene("Character Selection");
    }
}
