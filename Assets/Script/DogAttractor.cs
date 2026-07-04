using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DogAttractor : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)]
    private float attractionStrength = 0.55f;

    [SerializeField]
    private Transform attractionPoint;

    [SerializeField, Min(0f)]
    private float arrivalDistance = 0.3f;

    private readonly Dictionary<DogMovement, HashSet<Collider>> attractionOccupants =
        new Dictionary<DogMovement, HashSet<Collider>>();

    private readonly Dictionary<DogMovement, HashSet<Collider>> contactOccupants =
        new Dictionary<DogMovement, HashSet<Collider>>();

    public event Action<DogAttractor, DogMovement> DogContacted;

    public float AttractionStrength => attractionStrength;

    public float ArrivalDistance => arrivalDistance;

    public Vector3 AttractionPosition => attractionPoint != null
        ? attractionPoint.position
        : transform.position;

    private void OnValidate()
    {
        attractionStrength = Mathf.Clamp01(attractionStrength);
        arrivalDistance = Mathf.Max(0f, arrivalDistance);
    }

    internal void HandleZoneEnter(
        DogAttractorZone.ZoneKind zoneKind,
        DogMovement dog,
        Collider dogCollider)
    {
        if (dog == null || dogCollider == null)
        {
            return;
        }

        Dictionary<DogMovement, HashSet<Collider>> occupants = GetOccupants(zoneKind);

        if (!occupants.TryGetValue(dog, out HashSet<Collider> colliders))
        {
            colliders = new HashSet<Collider>();
            occupants.Add(dog, colliders);
        }

        bool wasOutside = colliders.Count == 0;

        if (!colliders.Add(dogCollider) || !wasOutside)
        {
            return;
        }

        if (zoneKind == DogAttractorZone.ZoneKind.Attraction)
        {
            dog.RegisterAttractor(this);
            Debug.Log($"Dog entered attraction range: {name}", this);
            return;
        }

        Debug.Log($"Dog contacted attractor: {name}", this);
        DogContacted?.Invoke(this, dog);
    }

    internal void HandleZoneExit(
        DogAttractorZone.ZoneKind zoneKind,
        DogMovement dog,
        Collider dogCollider)
    {
        if (dog == null || dogCollider == null)
        {
            return;
        }

        Dictionary<DogMovement, HashSet<Collider>> occupants = GetOccupants(zoneKind);

        if (!occupants.TryGetValue(dog, out HashSet<Collider> colliders) ||
            !colliders.Remove(dogCollider) ||
            colliders.Count > 0)
        {
            return;
        }

        occupants.Remove(dog);

        if (zoneKind == DogAttractorZone.ZoneKind.Attraction)
        {
            dog.UnregisterAttractor(this);
            Debug.Log($"Dog left attraction range: {name}", this);
        }
    }

    private void OnDisable()
    {
        foreach (DogMovement dog in attractionOccupants.Keys)
        {
            if (dog == null)
            {
                continue;
            }

            dog.UnregisterAttractor(this);
            Debug.Log($"Dog left attraction range: {name}", this);
        }

        attractionOccupants.Clear();
        contactOccupants.Clear();
    }

    private Dictionary<DogMovement, HashSet<Collider>> GetOccupants(
        DogAttractorZone.ZoneKind zoneKind)
    {
        return zoneKind == DogAttractorZone.ZoneKind.Attraction
            ? attractionOccupants
            : contactOccupants;
    }
}
