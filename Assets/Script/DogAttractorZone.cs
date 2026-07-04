using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class DogAttractorZone : MonoBehaviour
{
    public enum ZoneKind
    {
        Attraction,
        Contact
    }

    [SerializeField]
    private DogAttractor attractor;

    [SerializeField]
    private ZoneKind zoneKind;

    private readonly HashSet<Collider> dogCollidersInside = new HashSet<Collider>();
    private Collider zoneCollider;

    private void Reset()
    {
        attractor = GetComponentInParent<DogAttractor>();

        if (TryGetComponent(out Collider attachedCollider))
        {
            attachedCollider.isTrigger = true;
        }
    }

    private void Awake()
    {
        if (!TryGetComponent(out zoneCollider))
        {
            Debug.LogError("DogAttractorZone requires a Collider.", this);
            enabled = false;
            return;
        }

        if (!zoneCollider.isTrigger)
        {
            Debug.LogError("DogAttractorZone Collider must have Is Trigger enabled.", this);
            enabled = false;
            return;
        }

        if (attractor == null)
        {
            attractor = GetComponentInParent<DogAttractor>();
        }

        if (attractor == null)
        {
            Debug.LogError("DogAttractorZone requires a parent DogAttractor.", this);
            enabled = false;
        }
    }

    private void OnValidate()
    {
        if (attractor == null)
        {
            attractor = GetComponentInParent<DogAttractor>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        DogMovement dog = other.GetComponentInParent<DogMovement>();

        if (dog == null || !dogCollidersInside.Add(other))
        {
            return;
        }

        attractor.HandleZoneEnter(zoneKind, dog, other);
    }

    private void OnTriggerExit(Collider other)
    {
        DogMovement dog = other.GetComponentInParent<DogMovement>();

        if (dog == null || !dogCollidersInside.Remove(other))
        {
            return;
        }

        attractor.HandleZoneExit(zoneKind, dog, other);
    }

    private void OnDisable()
    {
        if (attractor != null)
        {
            foreach (Collider dogCollider in dogCollidersInside)
            {
                if (dogCollider == null)
                {
                    continue;
                }

                DogMovement dog = dogCollider.GetComponentInParent<DogMovement>();

                if (dog != null)
                {
                    attractor.HandleZoneExit(zoneKind, dog, dogCollider);
                }
            }
        }

        dogCollidersInside.Clear();
    }
}
