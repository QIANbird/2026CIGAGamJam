using UnityEngine;

public static class ScoreSystemBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureScoreSystemExists()
    {
        LevelManager levelManager = Object.FindObjectOfType<LevelManager>();

        if (levelManager == null)
        {
            return;
        }

        ScoreManager scoreManager = levelManager.GetComponent<ScoreManager>();

        if (scoreManager == null)
        {
            scoreManager = levelManager.gameObject.AddComponent<ScoreManager>();
        }

        if (Object.FindObjectOfType<ScoreRuntimePanel>() == null)
        {
            GameObject panelObject = new GameObject("ScoreRuntimePanel");
            panelObject.AddComponent<ScoreRuntimePanel>();
        }
    }
}
