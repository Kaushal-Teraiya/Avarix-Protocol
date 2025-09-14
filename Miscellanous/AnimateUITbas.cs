using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class TabPanelController : MonoBehaviour
{
    public GameObject[] allTabs;
    public float collapsedHeight = 60f;
    public float expandedHeight = 200f;
    public float tweenDuration = 0.3f;
    public float moveUpY = -100f; // How far up the tab should move

    private GameObject currentlyExpanded = null;
    private Vector2[] originalPositions;
    private bool isExpanded = false;

    void Start()
    {
        // Store the original anchored positions of all tabs
        originalPositions = new Vector2[allTabs.Length];
        for (int i = 0; i < allTabs.Length; i++)
        {
            originalPositions[i] = allTabs[i].GetComponent<RectTransform>().anchoredPosition;
        }
    }

    public void ToggleTab(GameObject selectedTab)
    {
        if (currentlyExpanded == selectedTab && isExpanded)
        {
            CollapseTab(selectedTab);
            ResetAllTabs();
            currentlyExpanded = null;
            isExpanded = false;
        }
        else
        {
            HideAllTabsExcept(selectedTab);
            MoveToTopAndExpand(selectedTab);
            currentlyExpanded = selectedTab;
            isExpanded = true;
        }
    }

    void HideAllTabsExcept(GameObject keepVisible)
    {
        foreach (var tab in allTabs)
        {
            if (tab != keepVisible)
                tab.SetActive(false);
        }
    }

    void ResetAllTabs()
    {
        for (int i = 0; i < allTabs.Length; i++)
        {
            GameObject tab = allTabs[i];
            tab.SetActive(true);

            RectTransform rt = tab.GetComponent<RectTransform>();
            rt.DOAnchorPos(originalPositions[i], tweenDuration).SetEase(Ease.OutQuad);
            CollapseTab(tab);
        }
    }

    void MoveToTopAndExpand(GameObject tab)
    {
        RectTransform rt = tab.GetComponent<RectTransform>();

        // Move it upward (you can tune moveUpY or calculate dynamically)
        rt.DOAnchorPos(new Vector2(rt.anchoredPosition.x, moveUpY), tweenDuration).SetEase(Ease.OutQuad);
        rt.DOSizeDelta(new Vector2(rt.sizeDelta.x, expandedHeight), tweenDuration).SetEase(Ease.OutQuad);

        var content = tab.transform.Find("Content");
        if (content != null) content.gameObject.SetActive(true);
    }

    void CollapseTab(GameObject tab)
    {
        RectTransform rt = tab.GetComponent<RectTransform>();
        rt.DOSizeDelta(new Vector2(rt.sizeDelta.x, collapsedHeight), tweenDuration).SetEase(Ease.OutQuad);

        var content = tab.transform.Find("Content");
        if (content != null) content.gameObject.SetActive(false);
    }
}
