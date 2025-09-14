using UnityEngine;

[CreateAssetMenu(fileName = "NewMap", menuName = "Game/Map Data")]
public class MapData : ScriptableObject
{
    public string mapName;
    public string mapSceneName;
    public Sprite mapPreview;
}
