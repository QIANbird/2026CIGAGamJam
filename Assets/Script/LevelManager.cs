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
        Debug.Log("Level Complete", this);

        if (dogMovement != null)
        {
            dogMovement.enabled = false;
        }

        if (ownerFollower != null)
        {
            ownerFollower.enabled = false;
        }
    }
}
