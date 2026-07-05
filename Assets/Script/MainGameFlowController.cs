using System;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MainGameFlowController : MonoBehaviour
{
    [Header("Gameplay")]
    [SerializeField]
    private LevelManager levelManager;

    [Header("Name Input")]
    [SerializeField]
    private GameObject startUI;

    [SerializeField]
    private GameObject inputNameUI;

    [SerializeField]
    private TMP_InputField nameInputer;

    [SerializeField]
    private TMP_Text validationMessage;

    [SerializeField]
    private string emptyNameMessage = "请输入玩家ID";

    [Header("Runtime UI")]
    [SerializeField]
    private GameObject runtimeUI;

    private CanvasGroup runtimeUiCanvasGroup;
    private Font runtimeNameInputSourceFont;
    private TMP_FontAsset runtimeNameInputFontAsset;
    private bool imeModeCaptured;
    private IMECompositionMode previousImeCompositionMode;

    private void Awake()
    {
        ResolveReferences();
        ConfigureNameInput();
        ApplyInitialState();
    }

    private void Start()
    {
        if (inputNameUI != null && inputNameUI.activeInHierarchy)
        {
            FocusNameInput();
        }
    }

    public void ShowNameInputFromStartScreen()
    {
        GameSession.MarkStartScreenCompleted();

        if (startUI != null)
        {
            startUI.SetActive(false);
        }

        if (inputNameUI != null)
        {
            inputNameUI.SetActive(true);
        }

        SetRuntimeUIVisible(false);

        Time.timeScale = 0f;
        FocusNameInput();
    }

    public void ConfirmPlayerName()
    {
        if (nameInputer == null)
        {
            Debug.LogError("MainGameFlowController requires the nameInputer TMP_InputField.", this);
            return;
        }

        if (!GameSession.TrySetPlayerId(nameInputer.text))
        {
            SetValidationMessage(emptyNameMessage);
            nameInputer.Select();
            nameInputer.ActivateInputField();
            return;
        }

        SetValidationMessage(string.Empty);
        RestoreImeMode();

        if (inputNameUI != null)
        {
            inputNameUI.SetActive(false);
        }

        SetRuntimeUIVisible(true);

        if (levelManager != null)
        {
            levelManager.BeginLevel();
        }
        else
        {
            Debug.LogError("MainGameFlowController could not find a LevelManager.", this);
            Time.timeScale = 1f;
        }
    }

    private void ApplyInitialState()
    {
        bool requiresNameInput = GameSession.RequiresPlayerNameInput;
        bool showStartScreen = requiresNameInput &&
                               !GameSession.StartScreenCompleted &&
                               startUI != null;

        if (startUI != null)
        {
            startUI.SetActive(showStartScreen);
        }

        if (inputNameUI != null)
        {
            inputNameUI.SetActive(requiresNameInput && !showStartScreen);
        }

        SetRuntimeUIVisible(!requiresNameInput);

        SetValidationMessage(string.Empty);

        if (requiresNameInput)
        {
            Time.timeScale = 0f;
        }
    }

    private void ResolveReferences()
    {
        if (levelManager == null)
        {
            levelManager = FindObjectOfType<LevelManager>();
        }

        if (nameInputer == null && inputNameUI != null)
        {
            nameInputer = inputNameUI.GetComponentInChildren<TMP_InputField>(true);
        }

        if (startUI == null)
        {
            GameObject[] gameObjects = Resources.FindObjectsOfTypeAll<GameObject>();

            foreach (GameObject candidate in gameObjects)
            {
                if (candidate != null &&
                    candidate.scene == gameObject.scene &&
                    candidate.name == "StartCanvas")
                {
                    startUI = candidate;
                    break;
                }
            }
        }

        if (runtimeUI != null)
        {
            runtimeUiCanvasGroup = runtimeUI.GetComponent<CanvasGroup>();
        }
    }

    private void SetRuntimeUIVisible(bool isVisible)
    {
        if (runtimeUI == null)
        {
            return;
        }

        runtimeUI.SetActive(isVisible);

        if (runtimeUiCanvasGroup == null)
        {
            runtimeUiCanvasGroup = runtimeUI.GetComponent<CanvasGroup>();
        }

        if (runtimeUiCanvasGroup != null)
        {
            runtimeUiCanvasGroup.alpha = isVisible ? 1f : 0f;
            runtimeUiCanvasGroup.interactable = isVisible;
            runtimeUiCanvasGroup.blocksRaycasts = isVisible;
        }
    }

    private void FocusNameInput()
    {
        if (nameInputer == null)
        {
            return;
        }

        CaptureAndEnableIme();
        nameInputer.Select();
        nameInputer.ActivateInputField();
    }

    private void ConfigureNameInput()
    {
        if (nameInputer == null)
        {
            return;
        }

        // Keep the field unrestricted so IME composition can commit Chinese characters.
        nameInputer.contentType = TMP_InputField.ContentType.Standard;
        nameInputer.characterValidation = TMP_InputField.CharacterValidation.None;
        nameInputer.inputValidator = null;
        nameInputer.keyboardType = TouchScreenKeyboardType.Default;

        TMP_Text inputText = nameInputer.textComponent;

        if (inputText == null || (inputText.font != null && inputText.font.HasCharacter('\u4e2d')))
        {
            return;
        }

        string chineseFontName = FindInstalledChineseFontName();

        if (string.IsNullOrEmpty(chineseFontName))
        {
            Debug.LogWarning(
                "No installed Chinese font was found. Chinese input is accepted, but glyphs may not render.",
                this);
            return;
        }

        runtimeNameInputSourceFont = Font.CreateDynamicFontFromOSFont(chineseFontName, 32);

        if (runtimeNameInputSourceFont == null)
        {
            Debug.LogWarning($"Could not create runtime font from '{chineseFontName}'.", this);
            return;
        }

        runtimeNameInputFontAsset = TMP_FontAsset.CreateFontAsset(runtimeNameInputSourceFont);

        if (runtimeNameInputFontAsset == null)
        {
            Debug.LogWarning($"Could not create TMP font asset from '{chineseFontName}'.", this);
            return;
        }

        runtimeNameInputFontAsset.name = "Runtime Chinese Name Input Font";
        inputText.font = runtimeNameInputFontAsset;

        if (nameInputer.placeholder is TMP_Text placeholderText)
        {
            placeholderText.font = runtimeNameInputFontAsset;
        }
    }

    private static string FindInstalledChineseFontName()
    {
        string[] preferredFontNames =
        {
            "Microsoft YaHei UI",
            "Microsoft YaHei",
            "SimHei",
            "SimSun",
            "Noto Sans CJK SC",
            "PingFang SC",
            "WenQuanYi Micro Hei"
        };
        string[] installedFontNames = Font.GetOSInstalledFontNames();

        foreach (string preferredFontName in preferredFontNames)
        {
            foreach (string installedFontName in installedFontNames)
            {
                if (string.Equals(preferredFontName, installedFontName, StringComparison.OrdinalIgnoreCase))
                {
                    return installedFontName;
                }
            }
        }

        return null;
    }

    private void CaptureAndEnableIme()
    {
        if (!imeModeCaptured)
        {
            previousImeCompositionMode = Input.imeCompositionMode;
            imeModeCaptured = true;
        }

        Input.imeCompositionMode = IMECompositionMode.On;
    }

    private void RestoreImeMode()
    {
        if (!imeModeCaptured)
        {
            return;
        }

        Input.imeCompositionMode = previousImeCompositionMode;
        imeModeCaptured = false;
    }

    private void OnDestroy()
    {
        RestoreImeMode();

        if (runtimeNameInputFontAsset != null)
        {
            Destroy(runtimeNameInputFontAsset);
        }

        if (runtimeNameInputSourceFont != null)
        {
            Destroy(runtimeNameInputSourceFont);
        }
    }

    private void SetValidationMessage(string message)
    {
        if (validationMessage != null)
        {
            validationMessage.text = message;
        }
        else if (!string.IsNullOrEmpty(message))
        {
            Debug.LogWarning(message, this);
        }
    }
}
