using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ScoreRuntimePanel : MonoBehaviour
{
    [Header("\u6570\u636e\u6765\u6e90")]
    [SerializeField]
    private ScoreManager scoreManager;

    [Header("Runtime_UI \u6839\u8282\u70b9")]
    [SerializeField]
    private string runtimeUiName = "Runtime_UI";

    [Header("\u9759\u6001\u63a7\u4ef6\u540d\u79f0")]
    [SerializeField]
    private string testTextName = "Text_test";

    [SerializeField]
    private string totalScoreTextName = "Text_totalScore";

    [SerializeField]
    private string timeTextName = "Text_time";

    [SerializeField]
    private string pressureSliderName = "Slider_presure";

    [SerializeField]
    private string pressureValueTextName = "Text_PresureValue";

    [Header("\u538b\u529b\u6761\u989c\u8272")]
    [SerializeField]
    private Color normalPressureColor = new Color(0.2f, 0.8f, 0.2f, 1f);

    [SerializeField]
    private Color warningPressureColor = new Color(1f, 0.85f, 0.1f, 1f);

    [SerializeField]
    private Color dangerPressureColor = new Color(1f, 0.2f, 0.2f, 1f);

    [SerializeField, Range(0f, 1f)]
    private float warningThresholdNormalized = 0.7f;

    [SerializeField, Range(0f, 1f)]
    private float dangerThresholdNormalized = 0.9f;

    private RectTransform runtimeUiRoot;

    private Text testLegacyText;
    private TMP_Text testTmpText;

    private Text totalScoreLegacyText;
    private TMP_Text totalScoreTmpText;

    private Text timeLegacyText;
    private TMP_Text timeTmpText;

    private Text pressureValueLegacyText;
    private TMP_Text pressureValueTmpText;

    private Slider pressureSlider;
    private Image pressureFillImage;

    private bool missingTimeTextLogged;

    private void Awake()
    {
        ResolveScoreManager();
        EnsureUiReferencesBound();
    }

    private void OnEnable()
    {
        ResolveScoreManager();

        if (scoreManager != null)
        {
            scoreManager.ScoresChanged -= RefreshUi;
            scoreManager.ScoresChanged += RefreshUi;
        }

        RefreshUi();
    }

    private void OnDisable()
    {
        if (scoreManager != null)
        {
            scoreManager.ScoresChanged -= RefreshUi;
        }
    }

    private void OnValidate()
    {
        warningThresholdNormalized = Mathf.Clamp01(warningThresholdNormalized);
        dangerThresholdNormalized = Mathf.Clamp(dangerThresholdNormalized, warningThresholdNormalized, 1f);
    }

    private void ResolveScoreManager()
    {
        if (scoreManager == null)
        {
            scoreManager = FindObjectOfType<ScoreManager>();
        }
    }

    private void EnsureUiReferencesBound()
    {
        if (runtimeUiRoot == null)
        {
            runtimeUiRoot = FindRuntimeUiRoot();
        }

        if (runtimeUiRoot == null)
        {
            Debug.LogError("ScoreRuntimePanel could not find Runtime_UI.", this);
            enabled = false;
            return;
        }

        if (testLegacyText == null && testTmpText == null)
        {
            BindText(testTextName, out testLegacyText, out testTmpText);
        }

        if (totalScoreLegacyText == null && totalScoreTmpText == null)
        {
            BindText(totalScoreTextName, out totalScoreLegacyText, out totalScoreTmpText);
        }

        if (timeLegacyText == null && timeTmpText == null)
        {
            BindText(timeTextName, out timeLegacyText, out timeTmpText);
        }

        if (pressureValueLegacyText == null && pressureValueTmpText == null)
        {
            BindText(pressureValueTextName, out pressureValueLegacyText, out pressureValueTmpText);
        }

        if (pressureSlider == null)
        {
            pressureSlider = FindComponentInRuntimeUi<Slider>(pressureSliderName);
        }

        if (pressureFillImage == null)
        {
            pressureFillImage = ResolvePressureFillImage(pressureSlider);
        }
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

            if (NamesMatch(rectTransform.name, runtimeUiName))
            {
                return rectTransform;
            }
        }

        return null;
    }

    private void BindText(string objectName, out Text legacyText, out TMP_Text tmpText)
    {
        legacyText = null;
        tmpText = null;

        Transform target = FindDeepChild(runtimeUiRoot, objectName);

        if (target == null)
        {
            return;
        }

        legacyText = target.GetComponent<Text>();

        if (legacyText != null)
        {
            return;
        }

        tmpText = target.GetComponent<TMP_Text>();
    }

    private T FindComponentInRuntimeUi<T>(string objectName)
        where T : Component
    {
        Transform target = FindDeepChild(runtimeUiRoot, objectName);
        return target != null ? target.GetComponent<T>() : null;
    }

    private static Transform FindDeepChild(Transform parent, string targetName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        int childCount = parent.childCount;

        for (int i = 0; i < childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (NamesMatch(child.name, targetName))
            {
                return child;
            }

            Transform nested = FindDeepChild(child, targetName);

            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private static bool NamesMatch(string actualName, string expectedName)
    {
        if (string.IsNullOrWhiteSpace(actualName) || string.IsNullOrWhiteSpace(expectedName))
        {
            return false;
        }

        return string.Equals(
            actualName.Trim(),
            expectedName.Trim(),
            System.StringComparison.OrdinalIgnoreCase);
    }

    private Image ResolvePressureFillImage(Slider slider)
    {
        if (slider == null || slider.fillRect == null)
        {
            return null;
        }

        Image fillImage = slider.fillRect.GetComponent<Image>();

        if (fillImage != null)
        {
            return fillImage;
        }

        return slider.fillRect.GetComponentInChildren<Image>(true);
    }

    private void RefreshUi()
    {
        if (scoreManager == null)
        {
            ResolveScoreManager();
        }

        EnsureUiReferencesBound();

        if (scoreManager == null)
        {
            SetText(totalScoreLegacyText, totalScoreTmpText, "\u5206\u6570\uff1a--");
            SetText(testLegacyText, testTmpText, "\u76f2\u9053\u5f97\u5206\uff1a--\n\u6263\u5206\u603b\u8ba1\uff1a--");
            SetText(timeLegacyText, timeTmpText, "\u5df2\u7528\u65f6\u95f4\uff1a--");
            SetText(pressureValueLegacyText, pressureValueTmpText, "\u538b\u529b\u503c\uff1a--");
            return;
        }

        SetText(
            totalScoreLegacyText,
            totalScoreTmpText,
            $"\u5206\u6570\uff1a{Mathf.RoundToInt(scoreManager.TotalScore)}");

        SetText(
            testLegacyText,
            testTmpText,
            $"\u76f2\u9053\u5f97\u5206\uff1a{scoreManager.BlindPathMoveScore:F1}\n\u6263\u5206\u603b\u8ba1\uff1a{scoreManager.PenaltyTotal:F1}");

        if (timeLegacyText != null || timeTmpText != null)
        {
            SetText(
                timeLegacyText,
                timeTmpText,
                $"\u5df2\u7528\u65f6\u95f4\uff1a{scoreManager.ElapsedTime:F1}");
            missingTimeTextLogged = false;
        }
        else if (!missingTimeTextLogged)
        {
            Debug.LogWarning(
                $"ScoreRuntimePanel could not find time text '{timeTextName}'. " +
                "Please confirm the object exists under Runtime_UI and the scene is saved.",
                this);
            missingTimeTextLogged = true;
        }

        SetText(
            pressureValueLegacyText,
            pressureValueTmpText,
            $"\u538b\u529b\u503c\uff1a{scoreManager.CurrentLeashPressure:F1}/{scoreManager.MaxLeashPressure:F0}");

        RefreshPressureSlider();
    }

    private void RefreshPressureSlider()
    {
        if (pressureSlider == null || scoreManager == null)
        {
            return;
        }

        pressureSlider.minValue = 0f;
        pressureSlider.maxValue = scoreManager.MaxLeashPressure;
        pressureSlider.value = scoreManager.CurrentLeashPressure;

        if (pressureFillImage == null)
        {
            return;
        }

        float normalizedPressure = scoreManager.LeashPressureNormalized;

        if (normalizedPressure >= dangerThresholdNormalized)
        {
            pressureFillImage.color = dangerPressureColor;
        }
        else if (normalizedPressure >= warningThresholdNormalized)
        {
            pressureFillImage.color = warningPressureColor;
        }
        else
        {
            pressureFillImage.color = normalPressureColor;
        }
    }

    private static void SetText(Text legacyText, TMP_Text tmpText, string content)
    {
        if (legacyText != null)
        {
            legacyText.text = content;
        }

        if (tmpText != null)
        {
            tmpText.text = content;
        }
    }
}
