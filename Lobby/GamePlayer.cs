using UnityEngine;
using Mirror;

public class GamePlayer : NetworkBehaviour
{
    [SyncVar]
    public int characterIndex;

    public GameObject[] characterPrefabs; // Assign all character prefabs in Inspector

    public override void OnStartClient()
    {
        SpawnCharacter();
    }

    void SpawnCharacter()
    {
        foreach (GameObject character in characterPrefabs)
        {
            character.SetActive(false);
        }

        if (characterIndex >= 0 && characterIndex < characterPrefabs.Length)
        {
            characterPrefabs[characterIndex].SetActive(true);
        }
    }
}
