using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform dog;

    [Header("Framing")]
    [Min(0f)]
    public float followDistance = 3f;

    [Min(0f)]
    public float cameraHeight = 0.8f;

    [Min(0f)]
    public float lookHeight = 0.6f;

    [Header("Smoothing")]
    [Min(0.01f)]
    public float followSmoothTime = 0.18f;

    [Min(0.01f)]
    public float rotationSharpness = 12f;

    public bool snapOnStart = true;

    private Vector3 followVelocity;

    private void Start()
    {
        if (dog == null)
        {
            Debug.LogError("CameraFollow requires the PlayerDog root Transform.", this);
            enabled = false;
            return;
        }

        if (snapOnStart)
        {
            transform.position = CalculateDesiredPosition();
            transform.rotation = CalculateDesiredRotation();
        }
    }

    private void OnValidate()
    {
        followDistance = Mathf.Max(0f, followDistance);
        cameraHeight = Mathf.Max(0f, cameraHeight);
        lookHeight = Mathf.Max(0f, lookHeight);
        followSmoothTime = Mathf.Max(0.01f, followSmoothTime);
        rotationSharpness = Mathf.Max(0.01f, rotationSharpness);
    }

    private void LateUpdate()
    {
        Vector3 desiredPosition = CalculateDesiredPosition();
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref followVelocity,
            followSmoothTime);

        Quaternion desiredRotation = CalculateDesiredRotation();
        float rotationBlend = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            rotationBlend);
    }

    private Vector3 CalculateDesiredPosition()
    {
        Vector3 forward = GetPlanarDogForward();
        return dog.position - forward * followDistance + Vector3.up * cameraHeight;
    }

    private Quaternion CalculateDesiredRotation()
    {
        Vector3 lookPoint = dog.position + Vector3.up * lookHeight;
        Vector3 lookDirection = lookPoint - transform.position;

        if (lookDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            return transform.rotation;
        }

        return Quaternion.LookRotation(lookDirection, Vector3.up);
    }

    private Vector3 GetPlanarDogForward()
    {
        Vector3 forward = Vector3.ProjectOnPlane(dog.forward, Vector3.up);

        if (forward.sqrMagnitude <= Mathf.Epsilon)
        {
            forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        }

        return forward.sqrMagnitude > Mathf.Epsilon
            ? forward.normalized
            : Vector3.forward;
    }
}
