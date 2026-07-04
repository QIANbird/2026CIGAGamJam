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
    private Animator pauseMenuAnimator;

    [SerializeField]
    private string openTrigger = "open";

    [SerializeField]
    private string closeTrigger = "close";

    [Header("Level Complete")]
    [SerializeField]
    private GameObject endUI;

    [SerializeField]
    private Animator endUiAnimator;

    [SerializeField]
    private string endUiOverTrigger = "OVER";

    private LevelManager subscribedLevelManager;
    private CanvasGroup endUiCanvasGroup;

    private void Awake()
    {
        if (pauseMenuAnimator != null)
        {
            pauseMenuAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }

        if (endUI != null)
        {
            CacheEndUiReferences();
            endUI.SetActive(false);
        }
    }

    private void Start()
    {
        SubscribeToLevelCompleted();
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
        if (!TryGetLevelManager(out LevelManager manager) || manager.IsLevelComplete)
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
        if (!TryGetLevelManager(out LevelManager manager) || manager.IsLevelComplete || manager.IsPaused)
        {
            return;
        }

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

        manager.ResumeGame();
        PlayPauseMenuTrigger(closeTrigger, openTrigger);
    }

    public void LoadMainScene()
    {
        RestoreTimeScaleBeforeSceneChange();
        SceneManager.LoadScene("MainScene");
    }

    public void LoadStartScene()
    {
        RestoreTimeScaleBeforeSceneChange();
        SceneManager.LoadScene("Start Scene");
    }

    public void RestartLevel()
    {
        RestoreTimeScaleBeforeSceneChange();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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

        CacheEndUiReferences();

        if (endUiAnimator == null && endUiCanvasGroup != null)
        {
            endUiCanvasGroup.alpha = 1f;
            endUiCanvasGroup.interactable = true;
            endUiCanvasGroup.blocksRaycasts = true;
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
