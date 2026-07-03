using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class OwnerFollower : MonoBehaviour
{
    [Header("Target")]
    [SerializeField]
    private Transform dog;

    [Header("Following")]
    [SerializeField, Min(0f)]
    private float maxSpeed = 2.7f;

    [SerializeField, Min(0f)]
    private float stopDistance = 1.2f;

    [SerializeField, Min(0f)]
    private float resumeDistance = 1.6f;

    [SerializeField, Min(0f)]
    private float fullSpeedDistance = 3.5f;

    [Header("Response")]
    [SerializeField, Min(0f)]
    private float acceleration = 4.5f;

    [SerializeField, Min(0f)]
    private float deceleration = 7f;

    private Rigidbody ownerRigidbody;
    private bool isFollowing;
    private Vector3 smoothedHorizontalVelocity;

    private void Awake()
    {
        if (!TryGetComponent(out ownerRigidbody))
        {
            Debug.LogError("OwnerFollower requires a Rigidbody.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        if (dog == null)
        {
            Debug.LogError("OwnerFollower requires the PlayerDog root Transform.", this);
            enabled = false;
            return;
        }

        Vector3 initialVelocity = ownerRigidbody.velocity;
        smoothedHorizontalVelocity = new Vector3(initialVelocity.x, 0f, initialVelocity.z);
        isFollowing = GetPlanarDistanceToDog() >= resumeDistance;
    }

    private void OnValidate()
    {
        maxSpeed = Mathf.Max(0f, maxSpeed);
        stopDistance = Mathf.Max(0f, stopDistance);
        resumeDistance = Mathf.Max(stopDistance, resumeDistance);
        fullSpeedDistance = Mathf.Max(resumeDistance, fullSpeedDistance);
        acceleration = Mathf.Max(0f, acceleration);
        deceleration = Mathf.Max(0f, deceleration);
    }

    private void FixedUpdate()
    {
        Vector3 toDog = dog.position - ownerRigidbody.position;
        toDog.y = 0f;

        float distance = toDog.magnitude;

        if (isFollowing)
        {
            if (distance <= stopDistance)
            {
                isFollowing = false;
            }
        }
        else if (distance >= resumeDistance)
        {
            isFollowing = true;
        }

        Vector3 targetVelocity = Vector3.zero;

        if (isFollowing && distance > Mathf.Epsilon)
        {
            float stretch = Mathf.InverseLerp(stopDistance, fullSpeedDistance, distance);
            float targetSpeed = maxSpeed * Mathf.SmoothStep(0f, 1f, stretch);

            float remainingDistance = Mathf.Max(0f, distance - stopDistance);
            float speedWithoutOvershoot = remainingDistance / Time.fixedDeltaTime;
            targetSpeed = Mathf.Min(targetSpeed, speedWithoutOvershoot);

            targetVelocity = toDog / distance * targetSpeed;
        }

        bool isAccelerating = targetVelocity.sqrMagnitude > smoothedHorizontalVelocity.sqrMagnitude;
        float response = isAccelerating ? acceleration : deceleration;
        Vector3 nextHorizontalVelocity = Vector3.MoveTowards(
            smoothedHorizontalVelocity,
            targetVelocity,
            response * Time.fixedDeltaTime);

        nextHorizontalVelocity = LimitVelocityToStopDistance(toDog, nextHorizontalVelocity);
        smoothedHorizontalVelocity = nextHorizontalVelocity;

        Vector3 currentVelocity = ownerRigidbody.velocity;
        currentVelocity.x = nextHorizontalVelocity.x;
        currentVelocity.z = nextHorizontalVelocity.z;
        ownerRigidbody.velocity = currentVelocity;
    }

    private void OnDisable()
    {
        if (ownerRigidbody == null)
        {
            return;
        }

        Vector3 velocity = ownerRigidbody.velocity;
        velocity.x = 0f;
        velocity.z = 0f;
        ownerRigidbody.velocity = velocity;
        smoothedHorizontalVelocity = Vector3.zero;
    }

    private float GetPlanarDistanceToDog()
    {
        Vector3 offset = dog.position - ownerRigidbody.position;
        offset.y = 0f;
        return offset.magnitude;
    }

    private Vector3 LimitVelocityToStopDistance(Vector3 toDog, Vector3 horizontalVelocity)
    {
        float speed = horizontalVelocity.magnitude;

        if (speed <= Mathf.Epsilon)
        {
            return Vector3.zero;
        }

        Vector3 moveDirection = horizontalVelocity / speed;
        float intendedStep = speed * Time.fixedDeltaTime;
        Vector3 predictedOffset = toDog - moveDirection * intendedStep;
        float stopDistanceSquared = stopDistance * stopDistance;

        if (predictedOffset.sqrMagnitude >= stopDistanceSquared)
        {
            return horizontalVelocity;
        }

        float projection = Vector3.Dot(toDog, moveDirection);
        float distanceFromBoundary = toDog.sqrMagnitude - stopDistanceSquared;
        float discriminant = projection * projection - distanceFromBoundary;

        if (projection <= 0f || discriminant <= 0f)
        {
            return horizontalVelocity;
        }

        float allowedStep = Mathf.Max(0f, projection - Mathf.Sqrt(discriminant));
        return moveDirection * (allowedStep / Time.fixedDeltaTime);
    }
}
