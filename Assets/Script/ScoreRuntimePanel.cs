using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ScoreRuntimePanel : MonoBehaviour
{
    [SerializeField]
    private ScoreManager scoreManager;

    [SerializeField]
    private string runtimeUiName = "Runtime_UI";

    [SerializeField, Min(0f)]
    private float leftPadding = 16f;

    [SerializeField, Min(0f)]
    private float topPadding = 16f;

    [SerializeField, Min(1)]
    private int fontSize = 20;

    [SerializeField]
    private Color textColor = Color.white;

    private readonly StringBuilder stringBuilder = new StringBuilder(128);

    private Text legacyText;
    private TMP_Text tmpText;

    private void Awake()
    {
        if (scoreManager == null)
        {
            scoreManager = FindObjectOfType<ScoreManager>();
        }

        EnsureRuntimeTextExists();
    }

    private void OnEnable()
    {
        if (scoreManager == null)
        {
            scoreManager = FindObjectOfType<ScoreManager>();
        }

        if (scoreManager != null)
        {
            scoreManager.ScoresChanged += RefreshText;
        }

        RefreshText();
    }

    private void OnDisable()
    {
        if (scoreManager != null)
        {
            scoreManager.ScoresChanged -= RefreshText;
        }
    }

    private void EnsureRuntimeTextExists()
    {
        if (legacyText != null || tmpText != null)
        {
            return;
        }

        RectTransform panelRoot = FindOrCreateRuntimeUiRoot();

        if (panelRoot == null)
        {
            Debug.LogError("ScoreRuntimePanel could not find or create a Runtime_UI root.", this);
            enabled = false;
            return;
        }

        if (TryBindExistingText(panelRoot))
        {
            return;
        }

        legacyText = CreateRuntimeLegacyText(panelRoot);
    }

    private RectTransform FindOrCreateRuntimeUiRoot()
    {
        RectTransform existingRoot = FindRuntimeUiRoot();

        if (existingRoot != null)
        {
            return existingRoot;
        }

        Canvas canvas = FindObjectOfType<Canvas>();

        if (canvas == null)
        {
            return null;
        }

        GameObject runtimeUiObject = new GameObject(runtimeUiName);
        runtimeUiObject.layer = canvas.gameObject.layer;

        RectTransform rootRect = runtimeUiObject.AddComponent<RectTransform>();
        rootRect.SetParent(canvas.transform, false);
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.anchoredPosition = new Vector2(leftPadding, -topPadding);
        rootRect.sizeDelta = new Vector2(360f, 140f);

        return rootRect;
    }

    private RectTransform FindRuntimeUiRoot()
    {
        GameObject existingRoot = GameObject.Find(runtimeUiName);

        if (existingRoot != null)
        {
            return existingRoot.GetComponent<RectTransform>();
        }

        RectTransform[] rectTransforms = Resources.FindObjectsOfTypeAll<RectTransform>();

        foreach (RectTransform rectTransform in rectTransforms)
        {
            if (rectTransform == null ||
                rectTransform.hideFlags != HideFlags.None ||
                rectTransform.gameObject.scene.rootCount == 0)
            {
                continue;
            }

            if (string.Equals(
                    rectTransform.name,
                    runtimeUiName,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                return rectTransform;
            }
        }

        return null;
    }

    private bool TryBindExistingText(RectTransform panelRoot)
    {
        Transform explicitScoreText = panelRoot.Find("ScoreRuntimeText");

        if (TryAssignTextComponent(explicitScoreText))
        {
            return true;
        }

        Transform headerText = panelRoot.Find("Text_header");

        if (TryAssignTextComponent(headerText))
        {
            return true;
        }

        Text[] legacyTexts = panelRoot.GetComponentsInChildren<Text>(true);

        if (legacyTexts.Length > 0)
        {
            legacyText = legacyTexts[0];
            ConfigureLegacyText(legacyText);
            return true;
        }

        TMP_Text[] tmpTexts = panelRoot.GetComponentsInChildren<TMP_Text>(true);

        if (tmpTexts.Length > 0)
        {
            tmpText = tmpTexts[0];
            ConfigureTmpText(tmpText);
            return true;
        }

        return false;
    }

    private bool TryAssignTextComponent(Transform target)
    {
        if (target == null)
        {
            return false;
        }

        if (target.TryGetComponent(out Text foundLegacyText))
        {
            legacyText = foundLegacyText;
            ConfigureLegacyText(legacyText);
            return true;
        }

        if (target.TryGetComponent(out TMP_Text foundTmpText))
        {
            tmpText = foundTmpText;
            ConfigureTmpText(tmpText);
            return true;
        }

        return false;
    }

    private void ConfigureLegacyText(Text textComponent)
    {
        textComponent.font = textComponent.font != null
            ? textComponent.font
            : Resources.GetBuiltinResource<Font>("Arial.ttf");
        textComponent.fontSize = fontSize;
        textComponent.color = textColor;
        textComponent.alignment = TextAnchor.UpperLeft;
        textComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
        textComponent.verticalOverflow = VerticalWrapMode.Overflow;
        textComponent.raycastTarget = false;
        textComponent.supportRichText = false;
    }

    private void ConfigureTmpText(TMP_Text textComponent)
    {
        textComponent.fontSize = fontSize;
        textComponent.color = textColor;
        textComponent.alignment = TextAlignmentOptions.TopLeft;
        textComponent.enableWordWrapping = false;
        textComponent.raycastTarget = false;
        textComponent.richText = false;
        textComponent.overflowMode = TextOverflowModes.Overflow;
    }

    private Text CreateRuntimeLegacyText(RectTransform panelRoot)
    {
        GameObject textObject = new GameObject("ScoreRuntimeText");
        textObject.layer = panelRoot.gameObject.layer;

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.SetParent(panelRoot, false);
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(0f, 1f);
        textRect.pivot = new Vector2(0f, 1f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(360f, 140f);

        Text textComponent = textObject.AddComponent<Text>();
        ConfigureLegacyText(textComponent);
        return textComponent;
    }

    private void RefreshText()
    {
        if (legacyText == null && tmpText == null)
        {
            EnsureRuntimeTextExists();
        }

        string displayText;

        if (scoreManager == null)
        {
            displayText = "Score system not found.";
        }
        else
        {
            stringBuilder.Length = 0;
            stringBuilder.Append("\u603b\u5206: ");
            stringBuilder.Append(scoreManager.TotalScore.ToString("F1"));
            stringBuilder.Append('\n');
            stringBuilder.Append("\u76f2\u9053\u5f97\u5206: ");
            stringBuilder.Append(scoreManager.BlindPathMoveScore.ToString("F1"));
            stringBuilder.Append('\n');
            stringBuilder.Append("\u6263\u5206\u603b\u8ba1: ");
            stringBuilder.Append(scoreManager.PenaltyTotal.ToString("F1"));
            stringBuilder.Append('\n');
            stringBuilder.Append("\u7d2f\u8ba1\u65f6\u95f4: ");
            stringBuilder.Append(scoreManager.ElapsedTime.ToString("F1"));
            displayText = stringBuilder.ToString();
        }

        if (legacyText != null)
        {
            legacyText.text = displayText;
        }

        if (tmpText != null)
        {
            tmpText.text = displayText;
        }
    }
}
