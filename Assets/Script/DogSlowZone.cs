using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class DogSlowZone : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Range(0f, 1f)]
    private float speedMultiplier = 0.3f;

    [Header("Future Audio Hooks")]
    [SerializeField]
    private UnityEvent onDogEntered = new UnityEvent();

    [SerializeField]
    private UnityEvent onDogExited = new UnityEvent();

    private readonly Dictionary<DogMovement, HashSet<Collider>> occupants =
        new Dictionary<DogMovement, HashSet<Collider>>();

    public event Action<DogSlowZone, DogMovement> DogEntered;
    public event Action<DogSlowZone, DogMovement> DogExited;

    public float SpeedMultiplier => speedMultiplier;

    private void Reset()
    {
        if (TryGetComponent(out Collider attachedCollider))
        {
            attachedCollider.isTrigger = true;
        }
    }

    private void Awake()
    {
        if (!TryGetComponent(out Collider attachedCollider))
        {
            Debug.LogError("DogSlowZone requires a Collider.", this);
            enabled = false;
            return;
        }

        if (!attachedCollider.isTrigger)
        {
            Debug.LogError("DogSlowZone Collider must have Is Trigger enabled.", this);
            enabled = false;
        }
    }

    private void OnValidate()
    {
        speedMultiplier = Mathf.Clamp01(speedMultiplier);
    }

    private void OnTriggerEnter(Collider other)
    {
        DogMovement dog = other.GetComponentInParent<DogMovement>();

        if (dog == null)
        {
            return;
        }

        if (!occupants.TryGetValue(dog, out HashSet<Collider> dogColliders))
        {
            dogColliders = new HashSet<Collider>();
            occupants.Add(dog, dogColliders);
        }

        bool wasOutside = dogColliders.Count == 0;

        if (!dogColliders.Add(other) || !wasOutside)
        {
            return;
        }

        dog.SetMovementSpeedModifier(this, speedMultiplier);
        Debug.Log($"Dog entered slow zone: {name} (speed {speedMultiplier:P0})", this);
        DogEntered?.Invoke(this, dog);
        onDogEntered.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        DogMovement dog = other.GetComponentInParent<DogMovement>();

        if (dog == null ||
            !occupants.TryGetValue(dog, out HashSet<Collider> dogColliders) ||
            !dogColliders.Remove(other) ||
            dogColliders.Count > 0)
        {
            return;
        }

        occupants.Remove(dog);
        RemoveEffect(dog);
    }

    private void OnDisable()
    {
        foreach (DogMovement dog in occupants.Keys)
        {
            if (dog != null)
            {
                RemoveEffect(dog);
            }
        }

        occupants.Clear();
    }

    private void RemoveEffect(DogMovement dog)
    {
        dog.RemoveMovementSpeedModifier(this);
        Debug.Log($"Dog left slow zone: {name}", this);
        DogExited?.Invoke(this, dog);
        onDogExited.Invoke();
    }
}
