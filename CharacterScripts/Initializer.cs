using UnityEngine;

public class Initializer : MonoBehaviour
{
    [SerializeField]private GameObject CharacterSelectionUI;

    void Start()
    {
        if(CharacterSelectionUI != null)
        {CharacterSelectionUI.SetActive(true);}
        else
        {
            Debug.Log("SelectionUI is null");
        }
    }
    
}
