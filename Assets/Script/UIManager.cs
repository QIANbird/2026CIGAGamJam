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

    private void Awake()
    {
        if (pauseMenuAnimator != null)
        {
            pauseMenuAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }
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
}
