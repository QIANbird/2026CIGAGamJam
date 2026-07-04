using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class CameraFollow : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField]
    private Transform dog;

    [SerializeField]
    private Transform owner;

    [SerializeField, Range(0f, 1f)]
    private float dogBias = 0.65f;

    [Header("Framing")]
    [SerializeField]
    private Vector3 baseOffset = new Vector3(0f, 8f, -8f);

    [SerializeField]
    private float focusHeight = 0.8f;

    [SerializeField, Min(0f)]
    private float zoomStartDistance = 2.5f;

    [SerializeField, Min(0f)]
    private float zoomPerMeter = 0.8f;

    [SerializeField, Min(0f)]
    private float maximumExtraDistance = 3f;

    [Header("Smoothing")]
    [SerializeField, Min(0.01f)]
    private float followSmoothTime = 0.18f;

    [SerializeField]
    private bool snapOnStart = true;

    private Vector3 followVelocity;
    private Vector3 offsetDirection;
    private float baseDistance;
    private Quaternion fixedRotation;

    private void Awake()
    {
        RecalculateOffset();
    }

    private void Start()
    {
        if (dog == null || owner == null)
        {
            Debug.LogError("CameraFollow requires both PlayerDog and Owner root Transforms.", this);
            enabled = false;
            return;
        }

        if (snapOnStart)
        {
            transform.position = CalculateDesiredPosition();
        }

        transform.rotation = fixedRotation;
    }

    private void OnValidate()
    {
        dogBias = Mathf.Clamp01(dogBias);
        zoomStartDistance = Mathf.Max(0f, zoomStartDistance);
        zoomPerMeter = Mathf.Max(0f, zoomPerMeter);
        maximumExtraDistance = Mathf.Max(0f, maximumExtraDistance);
        followSmoothTime = Mathf.Max(0.01f, followSmoothTime);

        RecalculateOffset();
    }

    private void LateUpdate()
    {
        Vector3 desiredPosition = CalculateDesiredPosition();
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref followVelocity,
            followSmoothTime);

        transform.rotation = fixedRotation;
    }

    private Vector3 CalculateDesiredPosition()
    {
        Vector3 focusPoint = Vector3.Lerp(owner.position, dog.position, dogBias);
        focusPoint.y += focusHeight;

        Vector3 separation = dog.position - owner.position;
        separation.y = 0f;

        float extraDistance = Mathf.Clamp(
            (separation.magnitude - zoomStartDistance) * zoomPerMeter,
            0f,
            maximumExtraDistance);

        return focusPoint + offsetDirection * (baseDistance + extraDistance);
    }

    private void RecalculateOffset()
    {
        baseDistance = baseOffset.magnitude;

        if (baseDistance <= Mathf.Epsilon)
        {
            baseOffset = new Vector3(0f, 8f, -8f);
            baseDistance = baseOffset.magnitude;
        }

        offsetDirection = baseOffset / baseDistance;
        fixedRotation = Quaternion.LookRotation(-offsetDirection, Vector3.up);
    }
}
