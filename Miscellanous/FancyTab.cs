using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class FancyTab : MonoBehaviour
{
    public RectTransform mainPanel;
    public GameObject infoPanel; // Contains text/buttons/icons
    private CanvasGroup infoGroup;

    public float expandHeight = 200f;
    public float collapseHeight = 50f;

    private bool isExpanded = false;

    void Start()
    {
        infoGroup = infoPanel.GetComponent<CanvasGroup>();
        if (infoGroup == null)
        {
            infoGroup = infoPanel.AddComponent<CanvasGroup>();
        }

        infoGroup.alpha = 0;
        infoPanel.SetActive(false);
    }

    public void ToggleTab()
    {
        if (isExpanded)
            Collapse();
        else
            Expand();
    }

    void Expand()
    {
        isExpanded = true;

        // Move to top position (optional, tweak this depending on layout)
        mainPanel.SetAsLastSibling();

        // Expand height
        mainPanel
            .DOSizeDelta(new Vector2(mainPanel.sizeDelta.x, expandHeight), 0.3f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                infoPanel.SetActive(true);
                StartCoroutine(BlinkThenFadeIn());
            });
    }

    System.Collections.IEnumerator BlinkThenFadeIn()
    {
        // Quick flash on/off like screen turning on
        infoGroup.alpha = 0;
        yield return new WaitForSeconds(0.05f);
        infoGroup.alpha = 1;
        yield return new WaitForSeconds(0.05f);
        infoGroup.alpha = 0;
        yield return new WaitForSeconds(0.05f);

        // Smooth fade-in
        infoGroup.DOFade(1f, 0.4f).SetEase(Ease.InOutSine);
    }

    void Collapse()
    {
        isExpanded = false;

        // Fade out before collapsing
        infoGroup
            .DOFade(0f, 0.2f)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                infoPanel.SetActive(false);
                mainPanel
                    .DOSizeDelta(new Vector2(mainPanel.sizeDelta.x, collapseHeight), 0.3f)
                    .SetEase(Ease.InQuad);
            });
    }
}
