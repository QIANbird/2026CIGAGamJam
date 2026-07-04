using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class GoalTrigger : MonoBehaviour
{
    [SerializeField]
    private LevelManager levelManager;

    private void Awake()
    {
        if (!TryGetComponent(out BoxCollider triggerCollider))
        {
            Debug.LogError("GoalTrigger requires a BoxCollider.", this);
            enabled = false;
            return;
        }

        if (!triggerCollider.isTrigger)
        {
            Debug.LogError("GoalTrigger BoxCollider must have Is Trigger enabled.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        if (levelManager != null)
        {
            return;
        }

        Debug.LogError("GoalTrigger requires a LevelManager reference.", this);
        enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        OwnerFollower owner = other.GetComponentInParent<OwnerFollower>();

        if (owner == null)
        {
            return;
        }

        levelManager.CompleteLevel();
    }
}
