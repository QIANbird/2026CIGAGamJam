using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DogScoreCollisionReporter : MonoBehaviour
{
    private const string ObstacleNameFallback = "obstacle";
    private const string ObstacleNameLegacyFallback = "obstcle";

    private readonly Dictionary<Transform, int> activeObstacleContacts =
        new Dictionary<Transform, int>();

    public event Action<Transform> ObstacleEntered;

    public string ObstacleTag { get; set; } = "obstacle";

    private void OnCollisionEnter(Collision collision)
    {
        RegisterObstacleContact(collision.collider);
    }

    private void OnCollisionExit(Collision collision)
    {
        UnregisterObstacleContact(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        RegisterObstacleContact(other);
    }

    private void OnTriggerExit(Collider other)
    {
        UnregisterObstacleContact(other);
    }

    private void OnDisable()
    {
        activeObstacleContacts.Clear();
    }

    private void RegisterObstacleContact(Collider other)
    {
        if (!TryGetObstacleRoot(other, out Transform obstacleRoot))
        {
            return;
        }

        if (activeObstacleContacts.TryGetValue(obstacleRoot, out int contactCount))
        {
            activeObstacleContacts[obstacleRoot] = contactCount + 1;
            return;
        }

        activeObstacleContacts.Add(obstacleRoot, 1);
        ObstacleEntered?.Invoke(obstacleRoot);
    }

    private void UnregisterObstacleContact(Collider other)
    {
        if (!TryGetObstacleRoot(other, out Transform obstacleRoot) ||
            !activeObstacleContacts.TryGetValue(obstacleRoot, out int contactCount))
        {
            return;
        }

        if (contactCount <= 1)
        {
            activeObstacleContacts.Remove(obstacleRoot);
            return;
        }

        activeObstacleContacts[obstacleRoot] = contactCount - 1;
    }

    private bool TryGetObstacleRoot(Collider other, out Transform obstacleRoot)
    {
        obstacleRoot = null;

        if (other == null)
        {
            return false;
        }

        Transform current = other.transform;

        while (current != null)
        {
            if (current.tag == ObstacleTag)
            {
                obstacleRoot = current;
                return true;
            }

            current = current.parent;
        }

        current = other.transform;

        while (current != null)
        {
            string name = current.name;

            if (name.IndexOf(ObstacleNameFallback, StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf(ObstacleNameLegacyFallback, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                obstacleRoot = current;
                return true;
            }

            current = current.parent;
        }

        return false;
    }
}
