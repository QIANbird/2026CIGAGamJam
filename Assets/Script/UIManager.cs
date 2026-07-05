using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class UIManager : MonoBehaviour
{
    [Header("Gameplay")]
    [SerializeField]
    private LevelManager levelManager;

    [Header("Pause Menu")]
    [SerializeField]
    private GameObject pauseUI;

    [SerializeField]
    private Animator pauseMenuAnimator;

    [SerializeField]
    private string openTrigger = "open";

    [SerializeField]
    private string closeTrigger = "close";

    [SerializeField, Min(0f)]
    private float pauseUiCloseHideDelay = 0.2f;

    [Header("Level Complete")]
    [SerializeField]
    private GameObject endUI;

    [SerializeField]
    private Animator endUiAnimator;

    [SerializeField]
    private string endUiOverTrigger = "OVER";

    private LevelManager subscribedLevelManager;
    private CanvasGroup endUiCanvasGroup;
    private CanvasGroup pauseUiCanvasGroup;
    private Coroutine hidePauseUiCoroutine;
    private EndGameFlowController endGameFlowController;

    private void Awake()
    {
        if (pauseMenuAnimator != null)
        {
            pauseMenuAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }

        CachePauseUiReferences();
        SetPauseUIHidden();
        endGameFlowController = GetComponent<EndGameFlowController>();

        if (endUI != null)
        {
            CacheEndUiReferences();
            endUI.SetActive(false);
        }
    }

    private void Start()
    {
        SubscribeToLevelCompleted();
        StartCoroutine(EnsurePauseUIHiddenAfterAnimatorInitialization());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (!TryGetLevelManager(out LevelManager manager) ||
            manager.IsLevelComplete ||
            manager.IsAwaitingPlayerName)
        {
            return;
        }

        if (manager.IsPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        if (!TryGetLevelManager(out LevelManager manager) ||
            manager.IsLevelComplete ||
            manager.IsAwaitingPlayerName ||
            manager.IsPaused)
        {
            return;
        }

        if (hidePauseUiCoroutine != null)
        {
            StopCoroutine(hidePauseUiCoroutine);
            hidePauseUiCoroutine = null;
        }

        if (pauseUI != null)
        {
            pauseUI.SetActive(true);
        }

        SetPauseUIInteraction(true);

        manager.PauseGame();
        PlayPauseMenuTrigger(openTrigger, closeTrigger);
    }

    public void ResumeGame()
    {
        if (!TryGetLevelManager(out LevelManager manager))
        {
            Time.timeScale = 1f;
            return;
        }

        if (manager.IsAwaitingPlayerName)
        {
            return;
        }

        manager.ResumeGame();
        PlayPauseMenuTrigger(closeTrigger, openTrigger);

        if (pauseUI != null)
        {
            if (hidePauseUiCoroutine != null)
            {
                StopCoroutine(hidePauseUiCoroutine);
            }

            hidePauseUiCoroutine = StartCoroutine(HidePauseUIAfterClose());
        }
    }

    public void LoadMainScene()
    {
        RestoreTimeScaleBeforeSceneChange();
        GameSession.PrepareForNameInput();
        SceneManager.LoadScene("MainScene");
    }

    public void LoadStartScene()
    {
        RestoreTimeScaleBeforeSceneChange();
        GameSession.PrepareForNameInput();
        SceneManager.LoadScene("MainScene");
    }

    public void RestartLevel()
    {
        RestoreTimeScaleBeforeSceneChange();
        GameSession.PrepareForNameInput();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private System.Collections.IEnumerator HidePauseUIAfterClose()
    {
        yield return new WaitForSecondsRealtime(pauseUiCloseHideDelay);

        SetPauseUIHidden();

        hidePauseUiCoroutine = null;
    }

    private System.Collections.IEnumerator EnsurePauseUIHiddenAfterAnimatorInitialization()
    {
        yield return null;
        SetPauseUIHidden();
    }

    private void CachePauseUiReferences()
    {
        if (pauseUI != null && pauseUiCanvasGroup == null)
        {
            pauseUiCanvasGroup = pauseUI.GetComponent<CanvasGroup>();
        }
    }

    private void SetPauseUIInteraction(bool isEnabled)
    {
        CachePauseUiReferences();

        if (pauseUiCanvasGroup != null)
        {
            pauseUiCanvasGroup.interactable = isEnabled;
            pauseUiCanvasGroup.blocksRaycasts = isEnabled;
        }
    }

    private void SetPauseUIHidden()
    {
        CachePauseUiReferences();

        if (pauseUiCanvasGroup != null)
        {
            pauseUiCanvasGroup.alpha = 0f;
            pauseUiCanvasGroup.interactable = false;
            pauseUiCanvasGroup.blocksRaycasts = false;
        }
        else if (pauseUI != null)
        {
            pauseUI.SetActive(false);
        }
    }

    private void SubscribeToLevelCompleted()
    {
        if (levelManager == null)
        {
            levelManager = FindObjectOfType<LevelManager>();
        }

        if (levelManager == null)
        {
            return;
        }

        subscribedLevelManager = levelManager;
        subscribedLevelManager.LevelCompleted += ShowEndUI;
    }

    private void ShowEndUI()
    {
        if (endUI == null)
        {
            Debug.LogError("UIManager requires an END_UI GameObject reference.", this);
            return;
        }

        endUI.SetActive(true);
        endUI.transform.SetAsLastSibling();

        CacheEndUiReferences();

        if (endUiCanvasGroup != null)
        {
            if (endUiAnimator == null)
            {
                endUiCanvasGroup.alpha = 1f;
            }

            endUiCanvasGroup.interactable = true;
            endUiCanvasGroup.blocksRaycasts = true;
        }

        if (endGameFlowController == null)
        {
            endGameFlowController = GetComponent<EndGameFlowController>();
        }

        if (endGameFlowController != null)
        {
            endGameFlowController.ShowResults();
        }
        else
        {
            Debug.LogError("UIManager requires an EndGameFlowController component.", this);
        }

        if (endUiAnimator != null && !string.IsNullOrEmpty(endUiOverTrigger))
        {
            endUiAnimator.ResetTrigger(endUiOverTrigger);
            endUiAnimator.SetTrigger(endUiOverTrigger);
        }
    }

    private bool TryGetLevelManager(out LevelManager manager)
    {
        if (levelManager == null)
        {
            levelManager = FindObjectOfType<LevelManager>();
        }

        manager = levelManager;

        if (manager != null)
        {
            return true;
        }

        Debug.LogError("UIManager could not find a LevelManager in the loaded scenes.", this);
        return false;
    }

    private void PlayPauseMenuTrigger(string triggerToSet, string triggerToReset)
    {
        if (pauseMenuAnimator == null)
        {
            Debug.LogError("UIManager requires a pause menu Animator.", this);
            return;
        }

        if (!string.IsNullOrEmpty(triggerToReset))
        {
            pauseMenuAnimator.ResetTrigger(triggerToReset);
        }

        if (!string.IsNullOrEmpty(triggerToSet))
        {
            pauseMenuAnimator.SetTrigger(triggerToSet);
        }
    }

    private void RestoreTimeScaleBeforeSceneChange()
    {
        if (levelManager != null)
        {
            levelManager.ResumeGame();
        }
        else
        {
            Time.timeScale = 1f;
        }
    }

    private void CacheEndUiReferences()
    {
        if (endUI == null)
        {
            endUiAnimator = null;
            endUiCanvasGroup = null;
            return;
        }

        if (endUiAnimator == null)
        {
            endUiAnimator = endUI.GetComponent<Animator>();

            if (endUiAnimator == null)
            {
                endUiAnimator = endUI.GetComponentInChildren<Animator>(true);
            }
        }

        if (endUiCanvasGroup == null)
        {
            endUiCanvasGroup = endUI.GetComponent<CanvasGroup>();

            if (endUiCanvasGroup == null)
            {
                endUiCanvasGroup = endUI.GetComponentInChildren<CanvasGroup>(true);
            }
        }
    }

    private void OnDestroy()
    {
        if (subscribedLevelManager != null)
        {
            subscribedLevelManager.LevelCompleted -= ShowEndUI;
        }
    }
}
