using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(LineRenderer))]
public sealed class LeashVisual : MonoBehaviour
{
    [Header("Anchors")]
    [SerializeField]
    private Transform dogAnchor;

    [SerializeField]
    private Transform ownerAnchor;

    private LineRenderer lineRenderer;

    private void Awake()
    {
        if (!TryGetComponent(out lineRenderer))
        {
            Debug.LogError("LeashVisual requires a LineRenderer.", this);
            enabled = false;
            return;
        }

        ConfigureLineRenderer();
    }

    private void Start()
    {
        if (dogAnchor == null || ownerAnchor == null)
        {
            Debug.LogError("LeashVisual requires both dog and owner LeashAnchor Transforms.", this);
            lineRenderer.enabled = false;
            enabled = false;
            return;
        }

        lineRenderer.enabled = true;
        UpdateLeashPositions();
    }

    private void LateUpdate()
    {
        UpdateLeashPositions();
    }

    private void OnValidate()
    {
        if (TryGetComponent(out LineRenderer renderer))
        {
            lineRenderer = renderer;
            ConfigureLineRenderer();
        }
    }

    private void ConfigureLineRenderer()
    {
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = false;
    }

    private void UpdateLeashPositions()
    {
        lineRenderer.SetPosition(0, dogAnchor.position);
        lineRenderer.SetPosition(1, ownerAnchor.position);
    }
}
