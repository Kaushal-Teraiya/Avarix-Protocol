using TMPro;
using UnityEngine;

public class PlayerNameInput : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private TMP_InputField nameInputField = null;

    public static string DisplayName { get; private set; }
    public const string PlayerPrefsNameKey = "PlayerName";

    void Start()
    {
        if (PlayerPrefs.HasKey(PlayerPrefsNameKey))
        {
            string savedName = PlayerPrefs.GetString(PlayerPrefsNameKey);
            nameInputField.text = savedName;
            SetDisplayName(savedName); // ✅ This ensures DisplayName is updated
        }

        // Add listener for input field changes
        nameInputField.onEndEdit.AddListener(SetDisplayNameFromInput);
    }

    public void SetDisplayNameFromInput(string name)
    {
        SetDisplayName(name);
    }

    public static void SetDisplayName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            Debug.LogError("Tried to set empty display name!");
            return;
        }

        DisplayName = name.Trim();
        PlayerPrefs.SetString(PlayerPrefsNameKey, DisplayName);
        PlayerPrefs.Save(); // ✅ Ensures it’s actually saved

        Debug.Log($"PlayerNameInput: Name set to {DisplayName}");
    }
}
