using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class BlindPathTrigger : MonoBehaviour
{
    private readonly HashSet<Collider> ownerCollidersInside = new HashSet<Collider>();
    private OwnerFollower trackedOwner;

    public bool IsOwnerInside => ownerCollidersInside.Count > 0;

    private void Awake()
    {
        if (!TryGetComponent(out BoxCollider triggerCollider))
        {
            Debug.LogError("BlindPathTrigger requires a BoxCollider.", this);
            enabled = false;
            return;
        }

        if (!triggerCollider.isTrigger)
        {
            Debug.LogError("BlindPathTrigger BoxCollider must have Is Trigger enabled.", this);
            enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        OwnerFollower owner = other.GetComponentInParent<OwnerFollower>();

        if (owner == null)
        {
            return;
        }

        if (trackedOwner != null && owner != trackedOwner)
        {
            return;
        }

        bool wasOutside = ownerCollidersInside.Count == 0;

        if (!ownerCollidersInside.Add(other))
        {
            return;
        }

        trackedOwner = owner;

        if (wasOutside)
        {
            Debug.Log("Owner entered blind path.", owner);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        OwnerFollower owner = other.GetComponentInParent<OwnerFollower>();

        if (owner == null || owner != trackedOwner)
        {
            return;
        }

        if (!ownerCollidersInside.Remove(other))
        {
            return;
        }

        if (ownerCollidersInside.Count == 0)
        {
            Debug.Log("Owner exited blind path.", owner);
            trackedOwner = null;
        }
    }

    private void OnDisable()
    {
        ownerCollidersInside.Clear();
        trackedOwner = null;
    }
}
