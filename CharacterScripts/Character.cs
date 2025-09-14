using UnityEngine;

[CreateAssetMenu(fileName = "New Character", menuName = "Character Selection/Character")]
public class Character : ScriptableObject
{
    [SerializeField]
    private string characterName;

    [SerializeField]
    private string characterAbility;

    [SerializeField]
    private string characterGun;

    [SerializeField]
    private GameObject characterPreviewPrefab;

    [SerializeField]
    private GameObject gameplayCharacterPrefab;

    // [SerializeField] private string teaM;
    [SerializeField]
    private AnimationClip characterSelectionAnimation;

    public string CharacterName => characterName;
    public string CharacterAbility => characterAbility;
    public string CharacterGun => characterGun;
    public GameObject CharacterPreviewPrefab => characterPreviewPrefab;
    public GameObject GameplayCharacterPrefab => gameplayCharacterPrefab;

    // public  string TeaM => teaM;
    public AnimationClip CharacterSelectionAnimation => characterSelectionAnimation;
}
