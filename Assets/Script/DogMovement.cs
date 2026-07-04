using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class DogMovement : MonoBehaviour
{
    public enum LeashState
    {
        Slack,
        Normal,
        Warning,
        Taut
    }

    [Header("Movement")]
    [SerializeField, Min(0f)]
    private float baseForwardSpeed = 2.7f;

    [FormerlySerializedAs("moveSpeed")]
    [SerializeField, Min(0f)]
    private float maxForwardSpeed = 4.5f;

    [SerializeField, Min(0f)]
    private float lateralSpeed = 4.5f;

    [SerializeField, Min(0f)]
    private float forwardAcceleration = 4.5f;

    [SerializeField, Min(0f)]
    private float forwardDeceleration = 7f;

    [SerializeField, Min(0f)]
    private float forwardRecovery = 4.5f;

    [Header("Attraction")]
    [Tooltip("AttractionStrength 为 1 时产生的最大吸引速度。")]
    [SerializeField, Min(0f)]
    private float attractionSpeedAtFullStrength = 6f;

    [Header("Leash Limit")]
    [SerializeField]
    private Transform owner;

    [SerializeField, Min(0.01f)]
    private float maxLeashDistance = 8f;

    [Tooltip("狗狗接近最大绳长时，逐渐移除向外速度的距离。")]
    [SerializeField, Min(0f)]
    private float slowdownDistance = 1f;

    [Header("Leash State Distance")]
    [SerializeField, Min(0f)]
    private float slackDistance = 1.6f;

    [SerializeField, Min(0f)]
    private float normalMaxDistance = 4.8f;

    [SerializeField, Min(0f)]
    private float warningMaxDistance = 6.8f;

    [Header("Debug")]
    [SerializeField]
    private bool logLeashStateChanges = true;

    private readonly HashSet<DogAttractor> activeAttractors = new HashSet<DogAttractor>();
    private Rigidbody dogRigidbody;
    private Rigidbody ownerRigidbody;
    private Vector2 moveInput;
    private float currentForwardSpeed;
    private bool leashStateInitialized;

    public LeashState CurrentLeashState { get; private set; }

    public float CurrentLeashDistance
    {
        get
        {
            if (owner == null || dogRigidbody == null)
            {
                return 0f;
            }

            Vector3 offset = dogRigidbody.position - owner.position;
            offset.y = 0f;
            return offset.magnitude;
        }
    }

    private void Awake()
    {
        if (!TryGetComponent(out dogRigidbody))
        {
            Debug.LogError("DogMovement requires a Rigidbody.", this);
            enabled = false;
            return;
        }

        currentForwardSpeed = baseForwardSpeed;
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
        UpdateLeashState();
    }

    private void OnValidate()
    {
        baseForwardSpeed = Mathf.Max(0f, baseForwardSpeed);
        maxForwardSpeed = Mathf.Max(baseForwardSpeed, maxForwardSpeed);
        lateralSpeed = Mathf.Max(0f, lateralSpeed);
        forwardAcceleration = Mathf.Max(0f, forwardAcceleration);
        forwardDeceleration = Mathf.Max(0f, forwardDeceleration);
        forwardRecovery = Mathf.Max(0f, forwardRecovery);
        attractionSpeedAtFullStrength = Mathf.Max(0f, attractionSpeedAtFullStrength);

        maxLeashDistance = Mathf.Max(0.01f, maxLeashDistance);
        slowdownDistance = Mathf.Clamp(slowdownDistance, 0f, maxLeashDistance);
        slackDistance = Mathf.Clamp(slackDistance, 0f, maxLeashDistance);
        normalMaxDistance = Mathf.Clamp(normalMaxDistance, slackDistance, maxLeashDistance);
        warningMaxDistance = Mathf.Clamp(warningMaxDistance, normalMaxDistance, maxLeashDistance);
    }

    private void Update()
    {
        float horizontal = 0f;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            horizontal -= 1f;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            horizontal += 1f;
        }

        float vertical = 0f;

        if (Input.GetKey(KeyCode.W))
        {
            vertical += 1f;
        }

        if (Input.GetKey(KeyCode.S))
        {
            vertical -= 1f;
        }

        moveInput = new Vector2(
            Mathf.Clamp(horizontal, -1f, 1f),
            Mathf.Clamp(vertical, -1f, 1f));
    }

    private void FixedUpdate()
    {
        UpdateForwardSpeed();
        UpdateLeashState();

        Vector3 desiredVelocity = new Vector3(
            moveInput.x * lateralSpeed,
            0f,
            currentForwardSpeed);

        DogAttractor attractor = GetClosestActiveAttractor();

        if (attractor != null)
        {
            Vector3 toAttractor = attractor.AttractionPosition - dogRigidbody.position;
            toAttractor.y = 0f;

            bool hasPlayerInput = moveInput.sqrMagnitude > Mathf.Epsilon;

            if (!hasPlayerInput &&
                toAttractor.sqrMagnitude <= attractor.ArrivalDistance * attractor.ArrivalDistance)
            {
                desiredVelocity = Vector3.zero;
            }
            else if (toAttractor.sqrMagnitude > Mathf.Epsilon)
            {
                float attractionSpeed =
                    attractor.AttractionStrength * attractionSpeedAtFullStrength;
                desiredVelocity += toAttractor.normalized * attractionSpeed;
            }
        }

        Vector3 limitedVelocity = LimitVelocityByLeash(desiredVelocity);
        SetPlanarVelocity(limitedVelocity);
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

        SetPlanarVelocity(Vector3.zero);
    }

    private void UpdateForwardSpeed()
    {
        float targetSpeed;
        float response;

        if (moveInput.y > 0f)
        {
            targetSpeed = maxForwardSpeed;
            response = forwardAcceleration;
        }
        else if (moveInput.y < 0f)
        {
            targetSpeed = 0f;
            response = forwardDeceleration;
        }
        else
        {
            targetSpeed = baseForwardSpeed;
            response = forwardRecovery;
        }

        currentForwardSpeed = Mathf.MoveTowards(
            currentForwardSpeed,
            targetSpeed,
            response * Time.fixedDeltaTime);
    }

    private void UpdateLeashState()
    {
        float distance = CurrentLeashDistance;
        LeashState nextState;

        if (distance < slackDistance)
        {
            nextState = LeashState.Slack;
        }
        else if (distance < normalMaxDistance)
        {
            nextState = LeashState.Normal;
        }
        else if (distance < warningMaxDistance)
        {
            nextState = LeashState.Warning;
        }
        else
        {
            nextState = LeashState.Taut;
        }

        if (leashStateInitialized && nextState == CurrentLeashState)
        {
            return;
        }

        CurrentLeashState = nextState;
        leashStateInitialized = true;

        if (logLeashStateChanges)
        {
            Debug.Log(
                $"牵引绳当前状态：{GetLeashStateLabel(nextState)}（距离 {distance:F2}）",
                this);
        }
    }

    private static string GetLeashStateLabel(LeashState state)
    {
        switch (state)
        {
            case LeashState.Slack:
                return "绳子松开";
            case LeashState.Normal:
                return "牵引中";
            case LeashState.Warning:
                return "绳子拉紧";
            case LeashState.Taut:
                return "过度拉紧";
            default:
                return state.ToString();
        }
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
        Vector3 ownerPosition = ownerRigidbody != null
            ? ownerRigidbody.position
            : owner.position;
        Vector3 ownerOffset = dogRigidbody.position - ownerPosition;
        ownerOffset.y = 0f;

        float currentDistance = ownerOffset.magnitude;

        if (currentDistance <= Mathf.Epsilon)
        {
            return desiredDogVelocity;
        }

        if (currentDistance > maxLeashDistance)
        {
            ownerOffset = ownerOffset / currentDistance * maxLeashDistance;
            currentDistance = maxLeashDistance;

            Vector3 clampedPosition = ownerPosition + ownerOffset;
            clampedPosition.y = dogRigidbody.position.y;
            dogRigidbody.position = clampedPosition;
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

        Vector3 predictedOffset = ownerOffset + relativeVelocity * Time.fixedDeltaTime;

        if (predictedOffset.sqrMagnitude > maxLeashDistance * maxLeashDistance)
        {
            predictedOffset = predictedOffset.normalized * maxLeashDistance;
            relativeVelocity = (predictedOffset - ownerOffset) / Time.fixedDeltaTime;
        }

        return ownerVelocity + relativeVelocity;
    }

    private void SetPlanarVelocity(Vector3 planarVelocity)
    {
        Vector3 velocity = dogRigidbody.velocity;
        velocity.x = planarVelocity.x;
        velocity.z = planarVelocity.z;
        dogRigidbody.velocity = velocity;
    }
}
