using UnityEngine;
using Mirror;
using TMPro;
using System.Collections.Generic;

public class CharacterSelect : NetworkBehaviour
{
    [SerializeField] private GameObject CharacterSelectDisplay = default;
    [SerializeField] private Transform CharacterPreviewParent = default;
    [SerializeField] private TMP_Text CharacterNameText = default;
    [SerializeField] private float turnSpeed = 90f;
    [SerializeField] private Character[] characters = default;

    private int currentCharacterIndex = 0;
    private List<GameObject> characterInstances = new List<GameObject>();

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (CharacterPreviewParent.childCount == 0)
        {
            foreach (var character in characters)
            {
                GameObject characterInstance = Instantiate(character.CharacterPreviewPrefab, CharacterPreviewParent);
                characterInstance.SetActive(false);
                characterInstances.Add(characterInstance);
            }
        }


        characterInstances[currentCharacterIndex].SetActive(true);
        CharacterNameText.text = characters[currentCharacterIndex].CharacterName;
        CharacterSelectDisplay.SetActive(true);
    }

    void Update()
    {
        CharacterPreviewParent.RotateAround(
            CharacterPreviewParent.position,
            CharacterPreviewParent.up,
            turnSpeed * Time.deltaTime);
    }
    public void Select()
    {
        CmdSelect(currentCharacterIndex);
        CharacterSelectDisplay.SetActive(false);
    }
     

    [Command(requiresAuthority = false)]
    public void CmdSelect(int characterIndex, NetworkConnectionToClient sender = null)
    {
        GameObject PlayerInstance = Instantiate(characters[characterIndex].GameplayCharacterPrefab);
       
        NetworkServer.ReplacePlayerForConnection(sender, PlayerInstance , ReplacePlayerOptions.KeepAuthority);
    }
    public void Right()
    {
        characterInstances[currentCharacterIndex].SetActive(false);
        currentCharacterIndex = (currentCharacterIndex + 1) % characterInstances.Count;
        characterInstances[currentCharacterIndex].SetActive(true);
        CharacterNameText.text = characters[currentCharacterIndex].CharacterName;
    }

    public void Left()
    {
        characterInstances[currentCharacterIndex].SetActive(false);
        currentCharacterIndex--;
        if (currentCharacterIndex < 0)
        {
            currentCharacterIndex += characterInstances.Count;
        }
        characterInstances[currentCharacterIndex].SetActive(true);
        CharacterNameText.text = characters[currentCharacterIndex].CharacterName;
    }
}