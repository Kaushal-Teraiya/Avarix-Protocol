using System.Collections;
using DG.Tweening;
using Mirror;
using TMPro;
using UnityEngine;

public class MatchWinUI : NetworkBehaviour
{
    public GameObject winPanel;
    public GameObject losePanel;
    public GameObject FinalScoreUIBoard;
    public TextMeshProUGUI winText;
    public TextMeshProUGUI loseText;

    public static MatchWinUI Instance;
    public float startDelay = 0.3f;
    public float delayBeforeScoreBoard = 3f;
    public float delayBeforeReturnToMenu = 2f;
    private bool hasHandledResult = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        winPanel.SetActive(false);
        losePanel.SetActive(false);
        FinalScoreUIBoard.SetActive(true);
    }

    void Update()
    {
        if (MatchTimer.Instance.hasMatchEnded && !hasHandledResult)
        {
            hasHandledResult = true;

            string result = WinningConditions.Instance.winningTeam;
            string myTeam = FlagHandler.local.Team;

            if (result == "Draw")
            {
                // Optional: Show draw UI
            }
            else if (result == myTeam)
            {
                ShowWinUI();
            }
            else
            {
                ShowLoseUI();
            }
        }
    }

    public void ShowWinUI()
    {
        winPanel.SetActive(true);
        StartCoroutine(AnimateTextUI(winText));
    }

    private void ShowLoseUI()
    {
        losePanel.SetActive(true);
        StartCoroutine(AnimateTextUI(loseText));
    }

    private IEnumerator AnimateTextUI(TextMeshProUGUI textUI)
    {
        textUI.transform.localScale = Vector3.zero;
        textUI.alpha = 0f;
        textUI.rectTransform.localRotation = Quaternion.Euler(0, 0, 20);

        Sequence seq = DOTween.Sequence();
        seq.AppendInterval(startDelay)
            .Append(textUI.DOFade(1f, 0.6f))
            .Join(textUI.transform.DOScale(1.5f, 0.6f).SetEase(Ease.OutBack))
            .Join(textUI.rectTransform.DORotate(Vector3.zero, 0.6f).SetEase(Ease.OutElastic))
            .Append(textUI.transform.DOScale(1f, 0.3f).SetEase(Ease.InOutSine))
            .Join(textUI.transform.DOScale(1.1f, 0.5f).SetLoops(2, LoopType.Yoyo))
            .Join(textUI.rectTransform.DOShakePosition(0.5f, 5f, 10, 90));

        yield return new WaitForSeconds(delayBeforeScoreBoard);

        FinalScoreUIBoard.SetActive(true);

        yield return new WaitForSeconds(delayBeforeReturnToMenu + 1f);

        ClientDisconnectHandler.Instance.FullResetAndReturnToCharacterSelection();
    }
}
