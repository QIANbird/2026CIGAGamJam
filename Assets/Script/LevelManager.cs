using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LevelManager : MonoBehaviour
{
    [Header("Movement Components")]
    [SerializeField]
    private DogMovement dogMovement;

    [SerializeField]
    private OwnerFollower ownerFollower;

    public bool IsLevelComplete { get; private set; }
    public bool IsPaused { get; private set; }
    public bool IsAwaitingPlayerName { get; private set; }

    public event Action LevelCompleted;

    private void Awake()
    {
        IsAwaitingPlayerName = GameSession.RequiresPlayerNameInput;
        IsPaused = IsAwaitingPlayerName;
        Time.timeScale = IsAwaitingPlayerName ? 0f : 1f;
    }

    private void Start()
    {
        if (dogMovement == null)
        {
            Debug.LogError("LevelManager requires the PlayerDog DogMovement component.", this);
        }

        if (ownerFollower == null)
        {
            Debug.LogError("LevelManager requires the Owner OwnerFollower component.", this);
        }
    }

    public void CompleteLevel()
    {
        if (IsLevelComplete)
        {
            return;
        }

        IsLevelComplete = true;
        IsAwaitingPlayerName = false;
        IsPaused = false;
        Time.timeScale = 1f;
        Debug.Log("Level Complete", this);

        if (dogMovement != null)
        {
            dogMovement.enabled = false;
        }

        if (ownerFollower != null)
        {
            ownerFollower.enabled = false;
        }

        LevelCompleted?.Invoke();
    }

    public void PauseGame()
    {
        if (IsLevelComplete || IsAwaitingPlayerName || IsPaused)
        {
            return;
        }

        IsPaused = true;
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        if (IsAwaitingPlayerName)
        {
            return;
        }

        IsPaused = false;
        Time.timeScale = 1f;
    }

    public void BeginLevel()
    {
        if (IsLevelComplete)
        {
            return;
        }

        IsAwaitingPlayerName = false;
        IsPaused = false;
        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        if (IsPaused)
        {
            Time.timeScale = 1f;
        }
    }
}
