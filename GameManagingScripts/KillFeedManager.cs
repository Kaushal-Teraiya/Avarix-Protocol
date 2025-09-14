using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KillFeedManager : MonoBehaviour
{
    public static KillFeedManager Instance;

    [Header("Kill Feed UI")]
    public GameObject killFeedEntryPrefab; // Prefab with HorizontalLayoutGroup: [Killer] [Icon] [Victim]
    public Transform killFeedParent; // VerticalLayoutGroup
    public Sprite weaponIconSprite; // Assign in Inspector

    [Header("Settings")]
    public float entryLifetime = 5f; // Duration before auto-removal
    public int maxEntries = 5; // Max number of visible entries

    private Queue<GameObject> killFeedQueue = new Queue<GameObject>();

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void AddKillFeedEntry(string killerName, string victimName)
    {
        // Create new entry
        GameObject entry = Instantiate(killFeedEntryPrefab, killFeedParent);

        // Expecting 3 children: [Killer Text] [Icon Image] [Victim Text]
        TextMeshProUGUI killerText = entry.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        Image icon = entry.transform.GetChild(2).GetComponent<Image>();
        TextMeshProUGUI victimText = entry.transform.GetChild(1).GetComponent<TextMeshProUGUI>();

        // Set texts
        if (killerText)
            killerText.text = killerName;
        if (victimText)
            victimText.text = victimName;

        // Set icon sprite
        if (icon && weaponIconSprite)
        {
            icon.sprite = weaponIconSprite;
            icon.preserveAspect = true;
            icon.enabled = true;
        }

        // Add to queue and remove oldest if exceeding max
        killFeedQueue.Enqueue(entry);

        if (killFeedQueue.Count > maxEntries)
        {
            Destroy(killFeedQueue.Dequeue());
        }

        // Auto-destroy after lifetime
        StartCoroutine(RemoveAfterTime(entry, entryLifetime));
    }

    private IEnumerator RemoveAfterTime(GameObject entry, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (entry != null)
        {
            if (killFeedQueue.Contains(entry))
                killFeedQueue.Dequeue();

            Destroy(entry);
        }
    }
}
