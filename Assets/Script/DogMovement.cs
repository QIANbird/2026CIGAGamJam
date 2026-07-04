using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class DogMovement : MonoBehaviour
{
    [SerializeField, Min(0f)]
    private float moveSpeed = 4.5f;

    [Header("Leash Limit")]
    [SerializeField]
    private Transform owner;

    [SerializeField, Min(0.01f)]
    private float maxLeashDistance = 3.5f;

    [SerializeField, Min(0f)]
    private float slowdownDistance = 1f;

    private readonly HashSet<DogAttractor> activeAttractors = new HashSet<DogAttractor>();
    private Rigidbody dogRigidbody;
    private Rigidbody ownerRigidbody;
    private Vector2 moveInput;

    private void Awake()
    {
        if (!TryGetComponent(out dogRigidbody))
        {
            Debug.LogError("DogMovement requires a Rigidbody.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        if (owner == null)
        {
            Debug.LogError("DogMovement requires the Owner root Transform.", this);
            enabled = false;
            return;
        }

        owner.TryGetComponent(out ownerRigidbody);
    }

    private void OnValidate()
    {
        maxLeashDistance = Mathf.Max(0.01f, maxLeashDistance);
        slowdownDistance = Mathf.Clamp(slowdownDistance, 0f, maxLeashDistance);
    }

    private void Update()
    {
        float horizontal = 0f;

        if (Input.GetKey(KeyCode.A))
        {
            horizontal -= 1f;
        }

        if (Input.GetKey(KeyCode.D))
        {
            horizontal += 1f;
        }

        float forward = Input.GetKey(KeyCode.W) ? 1f : 0f;
        moveInput = Vector2.ClampMagnitude(new Vector2(horizontal, forward), 1f);
    }

    private void FixedUpdate()
    {
        Vector3 steering = new Vector3(moveInput.x, 0f, moveInput.y);
        DogAttractor attractor = GetClosestActiveAttractor();

        if (attractor != null)
        {
            Vector3 toAttractor = attractor.AttractionPosition - dogRigidbody.position;
            toAttractor.y = 0f;

            if (toAttractor.sqrMagnitude > Mathf.Epsilon)
            {
                steering += toAttractor.normalized * attractor.AttractionStrength;
            }
        }

        float steeringMagnitude = steering.magnitude;

        if (steeringMagnitude <= Mathf.Epsilon)
        {
            StopHorizontalMovement();
            return;
        }

        Vector3 moveDirection = steering / steeringMagnitude;
        float inputStrength = Mathf.Clamp01(steeringMagnitude);
        Vector3 desiredVelocity = moveDirection * (moveSpeed * inputStrength);
        Vector3 limitedVelocity = LimitVelocityByLeash(desiredVelocity);

        Vector3 velocity = dogRigidbody.velocity;
        velocity.x = limitedVelocity.x;
        velocity.z = limitedVelocity.z;
        dogRigidbody.velocity = velocity;
    }

    internal void RegisterAttractor(DogAttractor attractor)
    {
        if (attractor != null)
        {
            activeAttractors.Add(attractor);
        }
    }

    internal void UnregisterAttractor(DogAttractor attractor)
    {
        if (attractor != null)
        {
            activeAttractors.Remove(attractor);
        }
    }

    private void OnDisable()
    {
        if (dogRigidbody == null)
        {
            return;
        }

        StopHorizontalMovement();
    }

    private DogAttractor GetClosestActiveAttractor()
    {
        DogAttractor closest = null;
        float closestDistanceSquared = float.PositiveInfinity;

        foreach (DogAttractor attractor in activeAttractors)
        {
            if (attractor == null || !attractor.isActiveAndEnabled)
            {
                continue;
            }

            Vector3 offset = attractor.AttractionPosition - dogRigidbody.position;
            offset.y = 0f;
            float distanceSquared = offset.sqrMagnitude;

            if (distanceSquared >= closestDistanceSquared)
            {
                continue;
            }

            closest = attractor;
            closestDistanceSquared = distanceSquared;
        }

        return closest;
    }

    private Vector3 LimitVelocityByLeash(Vector3 desiredDogVelocity)
    {
        Vector3 ownerOffset = dogRigidbody.position - owner.position;
        ownerOffset.y = 0f;

        float currentDistance = ownerOffset.magnitude;

        if (currentDistance <= Mathf.Epsilon)
        {
            return desiredDogVelocity;
        }

        Vector3 ownerVelocity = Vector3.zero;

        if (ownerRigidbody != null)
        {
            ownerVelocity = ownerRigidbody.velocity;
            ownerVelocity.y = 0f;
        }

        Vector3 leashDirection = ownerOffset / currentDistance;
        Vector3 relativeVelocity = desiredDogVelocity - ownerVelocity;
        float outwardRelativeSpeed = Vector3.Dot(relativeVelocity, leashDirection);

        if (outwardRelativeSpeed > 0f)
        {
            float slowdownStart = maxLeashDistance - slowdownDistance;
            float stretch = slowdownDistance > 0f
                ? Mathf.InverseLerp(slowdownStart, maxLeashDistance, currentDistance)
                : currentDistance >= maxLeashDistance ? 1f : 0f;
            float slowdown = Mathf.SmoothStep(0f, 1f, stretch);
            relativeVelocity -= leashDirection * (outwardRelativeSpeed * slowdown);
        }

        float allowedDistance = Mathf.Max(maxLeashDistance, currentDistance);
        Vector3 predictedOffset = ownerOffset + relativeVelocity * Time.fixedDeltaTime;

        if (predictedOffset.sqrMagnitude > allowedDistance * allowedDistance)
        {
            predictedOffset = predictedOffset.normalized * allowedDistance;
            relativeVelocity = (predictedOffset - ownerOffset) / Time.fixedDeltaTime;
        }

        return ownerVelocity + relativeVelocity;
    }

    private void StopHorizontalMovement()
    {
        Vector3 velocity = dogRigidbody.velocity;
        velocity.x = 0f;
        velocity.z = 0f;
        dogRigidbody.velocity = velocity;
    }
}
