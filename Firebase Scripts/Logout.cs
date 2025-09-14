using System.Threading.Tasks;
using UnityEngine;

public class Logout : MonoBehaviour
{
    public void OnLogout()
    {
        AuthManager.Instance.Logout();
    }
}
