using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreItemUI : MonoBehaviour
{
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI ownedCountText;
    public RawImage itemImage;
    public Image currencyImage;

    //private string itemId;

    [HideInInspector]
    public string itemId;

    public void SetItem(
        string name,
        int price,
        int ownedCount,
        string id,
        bool isConsumable,
        string currency
    )
    {
        itemNameText.text = name;
        itemId = id;
        priceText.text = price.ToString();

        if (currency == "Coins")
        {
            Sprite currTexC = Resources.Load<Sprite>("storeItems/Coins");
            if (currTexC != null)
            {
                currencyImage.sprite = currTexC;
            }
        }
        else
        {
            Sprite currTexS = Resources.Load<Sprite>("storeItems/Shards");
            if (currTexS != null)
            {
                currencyImage.sprite = currTexS;
            }
        }

        if (isConsumable)
        {
            ownedCountText.text = "Owned:" + ownedCount;
        }
        else
        {
            ownedCountText.text = "OWNED!";
        }

        Texture2D tex = Resources.Load<Texture2D>("storeItems/" + itemId);
        if (tex != null)
        {
            itemImage.texture = tex;
        }
    }

    public void UpdateOwnedCount(int newCount)
    {
        ownedCountText.text = "Owned: " + newCount;
    }

    public string GetItemId() => itemId;
}
