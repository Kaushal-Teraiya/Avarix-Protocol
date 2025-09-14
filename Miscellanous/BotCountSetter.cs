using TMPro;
using UnityEngine;
using Unity.Netcode;

public class BotCountSetter : MonoBehaviour
{
    [SerializeField] private TMP_InputField botInputField;

    public void OnSetBotCountButtonClicked()
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.LogWarning("Only host can set bot count.");
            return;
        }

        if (int.TryParse(botInputField.text, out int count))
        {
            PlayerPrefs.SetInt("BotCount", count);
            PlayerPrefs.Save();
            Debug.Log("Saved bot count: " + count);
        }
        else
        {
            Debug.LogError("Invalid input for bot count.");
        }
    }
}
