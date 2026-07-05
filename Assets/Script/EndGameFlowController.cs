using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class EndGameFlowController : MonoBehaviour
{
    private const int EasterEggPressureCount = 5;

    [Header("Gameplay")]
    [SerializeField]
    private LevelManager levelManager;

    [SerializeField]
    private ScoreManager scoreManager;

    [Header("Panels")]
    [SerializeField]
    private GameObject runtimeUI;

    [SerializeField]
    private GameObject resultPanel;

    [SerializeField]
    private GameObject leaderboardPanel;

    [Header("Result Text")]
    [SerializeField]
    private TMP_Text finalScoreText;

    [SerializeField]
    private TMP_Text pressureFullCountText;

    [SerializeField]
    private TMP_Text scoreEndingTitleText;

    [Tooltip("可选。未挂载时，彩蛋评价会追加到分数结局标题下一行。")]
    [SerializeField]
    private TMP_Text easterEggTitleText;

    private Text finalScoreLegacyText;
    private Text pressureFullCountLegacyText;
    private Text scoreEndingTitleLegacyText;

    [Header("Leaderboard")]
    [SerializeField]
    private Transform leaderboardContent;

    [SerializeField]
    private GameObject leaderboardItemTemplate;

    [SerializeField]
    private TMP_Text emptyLeaderboardText;

    [Header("Scene")]
    [SerializeField]
    private string startSceneName = "MainScene";

    private readonly List<GameObject> spawnedLeaderboardItems = new List<GameObject>();
    private LevelManager subscribedLevelManager;
    private bool completionHandled;

    private void Awake()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(false);
        }

        if (leaderboardItemTemplate != null)
        {
            leaderboardItemTemplate.SetActive(false);
        }

        SetEasterEggTitle(false);
    }

    private void Start()
    {
        ResolveReferences();
        SubscribeToLevelCompleted();
    }

    public void ShowLeaderboard()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(true);
        }

        PopulateLeaderboard(LocalLeaderboardStore.LoadTopEntries());
    }

    public void ReturnToStartScene()
    {
        Time.timeScale = 1f;
        GameSession.PrepareForNameInput();
        SceneManager.LoadScene(startSceneName);
    }

    private void HandleLevelCompleted()
    {
        if (completionHandled)
        {
            return;
        }

        completionHandled = true;
        ResolveReferences();

        if (scoreManager == null)
        {
            Debug.LogError("EndGameFlowController could not find a ScoreManager.", this);
            return;
        }

        int finalScore = Mathf.RoundToInt(scoreManager.TotalScore);
        int pressureFullCount = scoreManager.LeashPressureFullCount;

        if (runtimeUI != null)
        {
            runtimeUI.SetActive(false);
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(false);
        }

        SetText(finalScoreText, finalScoreLegacyText, finalScore.ToString());
        SetText(
            pressureFullCountText,
            pressureFullCountLegacyText,
            $"\u538b\u529b\u6b21\u6570\uff1a{pressureFullCount}");

        string scoreEndingTitle = GetScoreEndingTitle(finalScore);
        bool hasEasterEgg = pressureFullCount >= EasterEggPressureCount;

        if (scoreEndingTitleText != null)
        {
            scoreEndingTitleText.text = hasEasterEgg && easterEggTitleText == null
                ? scoreEndingTitle + "\n不吃压力之人"
                : scoreEndingTitle;
        }

        if (scoreEndingTitleLegacyText != null)
        {
            scoreEndingTitleLegacyText.text = hasEasterEgg && easterEggTitleText == null
                ? scoreEndingTitle + "\n\u4e0d\u5403\u538b\u529b\u4e4b\u4eba"
                : scoreEndingTitle;
        }

        SetEasterEggTitle(hasEasterEgg);
        LocalLeaderboardStore.AddEntry(GameSession.PlayerId, finalScore, pressureFullCount);
    }

    private void PopulateLeaderboard(IReadOnlyList<LocalLeaderboardEntry> entries)
    {
        ClearSpawnedLeaderboardItems();

        bool canCreateItems = leaderboardContent != null && leaderboardItemTemplate != null;

        if (!canCreateItems)
        {
            Debug.LogError(
                "EndGameFlowController requires leaderboard Content and RankingItem template references.",
                this);
            return;
        }

        if (emptyLeaderboardText != null)
        {
            emptyLeaderboardText.gameObject.SetActive(entries.Count == 0);
        }

        for (int index = 0; index < entries.Count; index++)
        {
            LocalLeaderboardEntry entry = entries[index];
            GameObject item = Instantiate(leaderboardItemTemplate, leaderboardContent);
            item.name = $"RankingItem_{index + 1}";
            item.SetActive(true);

            SetNamedText(item.transform, "rankingbar_rank", (index + 1).ToString());
            SetNamedText(item.transform, "rankingbar_name", entry.playerId);
            SetNamedText(item.transform, "rankingbar_score", entry.score.ToString());
            spawnedLeaderboardItems.Add(item);
        }
    }

    private void ClearSpawnedLeaderboardItems()
    {
        foreach (GameObject item in spawnedLeaderboardItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }

        spawnedLeaderboardItems.Clear();
    }

    private static void SetNamedText(Transform root, string childName, string value)
    {
        Transform child = root.Find(childName);

        if (child == null)
        {
            Debug.LogError($"RankingItem is missing text child '{childName}'.", root);
            return;
        }

        if (child.TryGetComponent(out TMP_Text tmpText))
        {
            tmpText.text = value;
            return;
        }

        if (child.TryGetComponent(out UnityEngine.UI.Text legacyText))
        {
            legacyText.text = value;
            return;
        }

        Debug.LogError($"RankingItem child '{childName}' has no supported text component.", root);
    }

    private static void SetText(TMP_Text tmpText, Text legacyText, string value)
    {
        if (tmpText != null)
        {
            tmpText.text = value;
        }

        if (legacyText != null)
        {
            legacyText.text = value;
        }
    }

    private static string GetScoreEndingTitle(int score)
    {
        if (score >= 900)
        {
            return "世界上最厉害的狗勾";
        }

        if (score >= 650)
        {
            return "好狗勾";
        }

        if (score >= 400)
        {
            return "差点迷路了…";
        }

        return "绳子打结了";
    }

    private void SetEasterEggTitle(bool isVisible)
    {
        if (easterEggTitleText == null)
        {
            return;
        }

        easterEggTitleText.text = isVisible ? "不吃压力之人" : string.Empty;
        easterEggTitleText.gameObject.SetActive(isVisible);
    }

    private void ResolveReferences()
    {
        if (levelManager == null)
        {
            levelManager = FindObjectOfType<LevelManager>();
        }

        if (scoreManager == null)
        {
            scoreManager = FindObjectOfType<ScoreManager>();
        }

        ResolveResultTextReferences();
    }

    private void ResolveResultTextReferences()
    {
        if (resultPanel == null)
        {
            return;
        }

        ResolveTextReference(
            resultPanel.transform,
            "T_finalScore",
            ref finalScoreText,
            ref finalScoreLegacyText);
        ResolveTextReference(
            resultPanel.transform,
            "T_presureAccount",
            ref pressureFullCountText,
            ref pressureFullCountLegacyText);
        ResolveTextReference(
            resultPanel.transform,
            "T_comment",
            ref scoreEndingTitleText,
            ref scoreEndingTitleLegacyText);
    }

    private static void ResolveTextReference(
        Transform root,
        string objectName,
        ref TMP_Text tmpText,
        ref Text legacyText)
    {
        if (tmpText != null || legacyText != null)
        {
            return;
        }

        Transform[] descendants = root.GetComponentsInChildren<Transform>(true);

        foreach (Transform descendant in descendants)
        {
            if (descendant.name != objectName)
            {
                continue;
            }

            tmpText = descendant.GetComponent<TMP_Text>();
            legacyText = descendant.GetComponent<Text>();
            return;
        }
    }

    private void SubscribeToLevelCompleted()
    {
        if (levelManager == null)
        {
            Debug.LogError("EndGameFlowController could not find a LevelManager.", this);
            return;
        }

        subscribedLevelManager = levelManager;
        subscribedLevelManager.LevelCompleted += HandleLevelCompleted;
    }

    private void OnDestroy()
    {
        if (subscribedLevelManager != null)
        {
            subscribedLevelManager.LevelCompleted -= HandleLevelCompleted;
        }
    }
}
