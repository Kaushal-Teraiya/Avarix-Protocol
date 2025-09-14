using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterSelectionMono : NetworkBehaviour
{
    [SerializeField]
    private GameObject CharacterSelectDisplay;

    [SerializeField]
    private Transform CharacterPreviewParent;

    [SerializeField]
    private TMP_Text CharacterNameText;

    [SerializeField]
    private TMP_Text CharacterGunText;

    [SerializeField]
    private TMP_Text CharacterAbilityText;

    [SerializeField]
    private float turnSpeed = 90f;

    [SerializeField]
    private Character[] characters;
    private int currentCharacterIndex = 0;
    private List<GameObject> characterInstances = new List<GameObject>();
    private Animator characterAnimator; // Add this line to hold the Animator reference.

    [SerializeField]
    private GameObject[] characterInfoPanels; // Each character's UI panel

    void Awake()
    {
        Debug.Log("🟢 Awake called - Setting Character Select Display Active");
        CharacterSelectDisplay.SetActive(true);
    }

    void Start()
    {
        Debug.Log("✅ Character Select Started");
        CharacterSelectDisplay.SetActive(true);

        if (characters.Length == 0)
        {
            Debug.LogError("❌ No characters assigned in the CharacterSelect script!");
            return;
        }

        InitializeCharacters();
        UpdateCharacterPanel();
    }

    void Update()
    {
        // RotateCharacterPreview();
    }

    private void InitializeCharacters()
    {
        Debug.Log("🔄 Initializing Characters...");

        foreach (Transform child in CharacterPreviewParent)
        {
            Debug.Log("🗑 Destroying old character preview: " + child.gameObject.name);
            Destroy(child.gameObject);
        }
        characterInstances.Clear();

        foreach (var character in characters)
        {
            // Debug.Log("🎭 Instantiating character preview: " + character.CharacterName);
            GameObject characterInstance = Instantiate(
                character.CharacterPreviewPrefab,
                CharacterPreviewParent
            );
            characterInstance.SetActive(false);
            characterInstances.Add(characterInstance);
        }

        if (characterInstances.Count > 0)
        {
            characterInstances[currentCharacterIndex].SetActive(true);
            CharacterNameText.text = characters[currentCharacterIndex].CharacterName;
            CharacterAbilityText.text = characters[currentCharacterIndex].CharacterAbility;
            CharacterGunText.text = characters[currentCharacterIndex].CharacterGun;
            // Play the animation for the current character preview.
            characterAnimator = characterInstances[currentCharacterIndex].GetComponent<Animator>();
            if (
                characterAnimator != null
                && characters[currentCharacterIndex].CharacterSelectionAnimation != null
            )
            {
                characterAnimator.Play(
                    characters[currentCharacterIndex].CharacterSelectionAnimation.name
                );
            }
            Debug.Log(
                $"🎭 Showing first character: {characters[currentCharacterIndex].CharacterName} (Index: {currentCharacterIndex})"
            );
            Debug.Log(
                $"🎭 Showing first character: {characters[currentCharacterIndex].CharacterAbility} (Index: {currentCharacterIndex})"
            );
            Debug.Log(
                $"🎭 Showing first character: {characters[currentCharacterIndex].CharacterGun} (Index: {currentCharacterIndex})"
            );
        }
    }

    private void RotateCharacterPreview()
    {
        if (characterInstances.Count > 0)
        {
            // Debug.Log("🔄 Rotating Character Preview...");
            CharacterPreviewParent.Rotate(Vector3.up * turnSpeed * Time.deltaTime);
        }
    }

    public void Select()
    {
        Debug.Log("✅ Select Button Pressed");
        int selectedCharacter = currentCharacterIndex;
        Debug.Log($"🎯 Selected Character Index: {selectedCharacter}");

        if (NetworkClient.active && NetworkClient.localPlayer != null)
        {
            Debug.Log("🌐 Multiplayer mode detected, sending character selection to server");
            NetworkGamePlayerLobby player =
                NetworkClient.localPlayer.GetComponent<NetworkGamePlayerLobby>();
            if (player != null)
            {
                player.CmdSetCharacterIndex(selectedCharacter);
                Debug.Log($"🛠 Selected Character Sent to Server: {selectedCharacter}");
            }
            else
            {
                Debug.LogError("❌ NetworkGamePlayerLobby component not found!");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ Not in a multiplayer session, storing selection locally");
            PlayerPrefs.SetInt("SelectedCharacter", selectedCharacter);
            PlayerPrefs.Save();
        }

        if (NetworkServer.active)
        {
            Debug.Log("🔄 Changing Scene to Lobby (Server)");
            NetworkManager.singleton.ServerChangeScene("Lobby");
        }
        else
        {
            Debug.Log("🔄 Changing Scene to Lobby (Client)");
            SceneManager.LoadScene("Lobby");
        }
    }

    public void Right()
    {
        Debug.Log("➡ Button Pressed: Right");

        if (characterInstances.Count == 0)
        {
            Debug.LogWarning("⚠️ No characters available to switch");
            return;
        }

        Debug.Log(
            $"🔄 Hiding character: {characters[currentCharacterIndex].CharacterName} (Index: {currentCharacterIndex})"
        );
        characterInstances[currentCharacterIndex].SetActive(false);
        currentCharacterIndex = (currentCharacterIndex + 1) % characterInstances.Count;
        Debug.Log(
            $"🎭 Showing new character: {characters[currentCharacterIndex].CharacterName} (Index: {currentCharacterIndex})"
        );
        characterInstances[currentCharacterIndex].SetActive(true);
        CharacterNameText.text = characters[currentCharacterIndex].CharacterName;
        CharacterAbilityText.text = characters[currentCharacterIndex].CharacterAbility;
        CharacterGunText.text = characters[currentCharacterIndex].CharacterGun;
    }

    public void Left()
    {
        Debug.Log("⬅ Button Pressed: Left");

        if (characterInstances.Count == 0)
        {
            Debug.LogWarning("⚠️ No characters available to switch");
            return;
        }

        Debug.Log(
            $"🔄 Hiding character: {characters[currentCharacterIndex].CharacterName} (Index: {currentCharacterIndex})"
        );
        characterInstances[currentCharacterIndex].SetActive(false);
        currentCharacterIndex =
            (currentCharacterIndex - 1 + characterInstances.Count) % characterInstances.Count;
        Debug.Log(
            $"🎭 Showing new character: {characters[currentCharacterIndex].CharacterName} (Index: {currentCharacterIndex})"
        );
        characterInstances[currentCharacterIndex].SetActive(true);
        CharacterNameText.text = characters[currentCharacterIndex].CharacterName;
        CharacterAbilityText.text = characters[currentCharacterIndex].CharacterAbility;
        CharacterGunText.text = characters[currentCharacterIndex].CharacterGun;
    }

    private void UpdateCharacterPanel()
    {
        for (int i = 0; i < characterInfoPanels.Length; i++)
        {
            characterInfoPanels[i].SetActive(i == currentCharacterIndex);
        }
    }
}
