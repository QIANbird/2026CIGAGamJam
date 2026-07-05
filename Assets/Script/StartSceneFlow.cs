using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class StartSceneFlow : MonoBehaviour
{
    [SerializeField]
    private string mainSceneName = "MainScene";

    [SerializeField]
    private Animator startAnimator;

    [Tooltip("Optional when StartCanvas and InputName_UI are in the same scene.")]
    [SerializeField]
    private GameObject inputNameUI;

    private bool isLoadingMainScene;
    private bool hasTransitioned;

    private void Awake()
    {
        string activeSceneName = SceneManager.GetActiveScene().name;

        if (activeSceneName == "Start Scene")
        {
            GameSession.PrepareForNameInput();
        }

        ResolveInputNameUI();
        ConfigureUnscaledStartEffects();
        Time.timeScale = 0f;

        if (activeSceneName == mainSceneName && GameSession.StartScreenCompleted)
        {
            if (inputNameUI != null)
            {
                inputNameUI.SetActive(true);
            }

            gameObject.SetActive(false);
        }
        else if (inputNameUI != null)
        {
            inputNameUI.SetActive(false);
        }
    }

    // Add this method as an Animation Event on the last frame of the start animation.
    public void LoadMainSceneForNameInput()
    {
        if (hasTransitioned)
        {
            return;
        }

        hasTransitioned = true;
        GameSession.MarkStartScreenCompleted();

        if (SceneManager.GetActiveScene().name == mainSceneName)
        {
            MainGameFlowController mainFlow = FindObjectOfType<MainGameFlowController>(true);

            if (mainFlow != null)
            {
                mainFlow.ShowNameInputFromStartScreen();
            }
            else
            {
                if (inputNameUI != null)
                {
                    inputNameUI.SetActive(true);
                }

                gameObject.SetActive(false);
            }

            return;
        }

        isLoadingMainScene = true;
        SceneManager.LoadScene(mainSceneName);
    }

    private void ResolveInputNameUI()
    {
        if (inputNameUI != null)
        {
            return;
        }

        GameObject[] gameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        Scene activeScene = SceneManager.GetActiveScene();

        foreach (GameObject candidate in gameObjects)
        {
            if (candidate != null &&
                candidate.scene == activeScene &&
                candidate.name == "InputName_UI")
            {
                inputNameUI = candidate;
                return;
            }
        }
    }

    private void ConfigureUnscaledStartEffects()
    {
        if (startAnimator == null)
        {
            startAnimator = GetComponent<Animator>();
        }

        if (startAnimator != null)
        {
            startAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }

        ParticleSystem[] particleSystems = FindObjectsOfType<ParticleSystem>(true);

        foreach (ParticleSystem particleSystem in particleSystems)
        {
            ParticleSystem.MainModule main = particleSystem.main;
            main.useUnscaledTime = true;
        }
    }

    private void OnDestroy()
    {
        if (!isLoadingMainScene && Time.timeScale <= 0f)
        {
            Time.timeScale = 1f;
        }
    }
}
