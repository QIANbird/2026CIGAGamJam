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

    private Rigidbody dogRigidbody;
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
        if (owner != null)
        {
            return;
        }

        Debug.LogError("DogMovement requires the Owner root Transform.", this);
        enabled = false;
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
        Vector3 input = new Vector3(moveInput.x, 0f, moveInput.y);
        float inputStrength = Mathf.Clamp01(input.magnitude);

        if (inputStrength <= Mathf.Epsilon)
        {
            StopHorizontalMovement();
            return;
        }

        Vector3 moveDirection = input / inputStrength;
        float desiredStep = moveSpeed * inputStrength * Time.fixedDeltaTime;

        Vector3 ownerOffset = dogRigidbody.position - owner.position;
        ownerOffset.y = 0f;

        float currentDistanceSquared = ownerOffset.sqrMagnitude;
        float maxDistanceSquared = maxLeashDistance * maxLeashDistance;
        Vector3 unrestrictedOffset = ownerOffset + moveDirection * desiredStep;
        bool increasesDistance = unrestrictedOffset.sqrMagnitude > currentDistanceSquared;

        if (currentDistanceSquared >= maxDistanceSquared)
        {
            if (increasesDistance)
            {
                desiredStep = 0f;
            }
        }
        else if (increasesDistance && slowdownDistance > 0f)
        {
            float currentDistance = Mathf.Sqrt(currentDistanceSquared);
            float slowdownStart = maxLeashDistance - slowdownDistance;
            float stretch = Mathf.InverseLerp(slowdownStart, maxLeashDistance, currentDistance);
            float speedMultiplier = 1f - Mathf.SmoothStep(0f, 1f, stretch);
            desiredStep *= speedMultiplier;
        }

        Vector3 limitedOffset = ownerOffset + moveDirection * desiredStep;

        if (currentDistanceSquared < maxDistanceSquared && limitedOffset.sqrMagnitude > maxDistanceSquared)
        {
            float remainingStep = GetDistanceToBoundary(ownerOffset, moveDirection);
            desiredStep = Mathf.Min(desiredStep, remainingStep);
        }

        float limitedSpeed = desiredStep / Time.fixedDeltaTime;
        Vector3 velocity = dogRigidbody.velocity;
        velocity.x = moveDirection.x * limitedSpeed;
        velocity.z = moveDirection.z * limitedSpeed;
        dogRigidbody.velocity = velocity;
    }

    private void OnDisable()
    {
        if (dogRigidbody == null)
        {
            return;
        }

        StopHorizontalMovement();
    }

    private float GetDistanceToBoundary(Vector3 ownerOffset, Vector3 moveDirection)
    {
        float projection = Vector3.Dot(ownerOffset, moveDirection);
        float distanceFromBoundary = ownerOffset.sqrMagnitude - maxLeashDistance * maxLeashDistance;
        float discriminant = projection * projection - distanceFromBoundary;

        if (discriminant <= 0f)
        {
            return 0f;
        }

        return Mathf.Max(0f, -projection + Mathf.Sqrt(discriminant));
    }

    private void StopHorizontalMovement()
    {
        Vector3 velocity = dogRigidbody.velocity;
        velocity.x = 0f;
        velocity.z = 0f;
        dogRigidbody.velocity = velocity;
    }
}
