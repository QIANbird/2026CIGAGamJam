using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class OwnerHandUI : MonoBehaviour
{
    [Header("References")]
    public Transform owner;

    public Camera worldCamera;

    public RectTransform handRect;

    public LevelManager levelManager;

    [Header("Hand Layout")]
    public Vector2 handSize = new Vector2(240f, 240f);

    public float bottomOffset;

    public float horizontalOffset;

    [Header("UI Visibility")]
    public int canvasSortingOrder = -100;

    [Header("Owner Appearance")]
    public bool hideOwnerRenderers = true;

    private RectTransform handParent;
    private Canvas rootCanvas;
    private CanvasGroup handCanvasGroup;
    private Renderer[] ownerRenderers;
    private bool initialized;
    private bool handVisible;

    private void Start()
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        if (levelManager == null)
        {
            levelManager = FindObjectOfType<LevelManager>();
        }

        if (!TryInitialize())
        {
            enabled = false;
            return;
        }

        ApplyHandLayout();
        ConfigureHandCanvas();
        DisableHandRaycasts();
        handRect.gameObject.SetActive(true);

        if (hideOwnerRenderers)
        {
            SetOwnerRenderersVisible(false);
        }

        initialized = true;
        UpdateHandVisibility();

        if (handVisible)
        {
            UpdateHandPosition();
        }
    }

    private void OnEnable()
    {
        if (initialized && handRect != null)
        {
            handRect.gameObject.SetActive(true);
            UpdateHandVisibility();
        }

        if (initialized && hideOwnerRenderers)
        {
            SetOwnerRenderersVisible(false);
        }
    }

    private void OnValidate()
    {
        handSize.x = Mathf.Max(0f, handSize.x);
        handSize.y = Mathf.Max(0f, handSize.y);
    }

    private void LateUpdate()
    {
        UpdateHandVisibility();

        if (handVisible)
        {
            UpdateHandPosition();
        }
    }

    private bool TryInitialize()
    {
        if (owner == null)
        {
            Debug.LogError("OwnerHandUI requires the Owner root Transform.", this);
            return false;
        }

        if (worldCamera == null)
        {
            Debug.LogError("OwnerHandUI requires a world Camera or a camera tagged MainCamera.", this);
            return false;
        }

        if (handRect == null)
        {
            Debug.LogError("OwnerHandUI requires the hand Image RectTransform.", this);
            return false;
        }

        handParent = handRect.parent as RectTransform;

        if (handParent == null)
        {
            Debug.LogError("OwnerHandUI requires the hand Image to have a RectTransform parent.", this);
            return false;
        }

        rootCanvas = handRect.GetComponentInParent<Canvas>();

        if (rootCanvas == null)
        {
            Debug.LogError("OwnerHandUI requires the hand Image to be inside a Canvas.", this);
            return false;
        }

        if (levelManager == null)
        {
            Debug.LogError("OwnerHandUI requires a LevelManager to determine gameplay visibility.", this);
            return false;
        }

        CacheOwnerRendererStates();
        return true;
    }

    private void ApplyHandLayout()
    {
        Vector2 bottomCenter = new Vector2(0.5f, 0f);
        handRect.anchorMin = bottomCenter;
        handRect.anchorMax = bottomCenter;
        handRect.pivot = bottomCenter;
        handRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, handSize.x);
        handRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, handSize.y);
    }

    private void ConfigureHandCanvas()
    {
        rootCanvas.overrideSorting = true;
        rootCanvas.sortingOrder = canvasSortingOrder;
    }

    private void DisableHandRaycasts()
    {
        Graphic[] handGraphics = handRect.GetComponentsInChildren<Graphic>(true);

        foreach (Graphic graphic in handGraphics)
        {
            graphic.raycastTarget = false;
        }

        handCanvasGroup = handRect.GetComponent<CanvasGroup>();

        if (handCanvasGroup == null)
        {
            handCanvasGroup = handRect.gameObject.AddComponent<CanvasGroup>();
        }

        handCanvasGroup.interactable = false;
        handCanvasGroup.blocksRaycasts = false;
    }

    private void UpdateHandVisibility()
    {
        bool shouldBeVisible = !levelManager.IsAwaitingPlayerName &&
                               !levelManager.IsLevelComplete;

        if (handVisible == shouldBeVisible &&
            Mathf.Approximately(handCanvasGroup.alpha, shouldBeVisible ? 1f : 0f))
        {
            return;
        }

        handVisible = shouldBeVisible;
        handCanvasGroup.alpha = handVisible ? 1f : 0f;
    }

    private void UpdateHandPosition()
    {
        Vector3 screenPoint = worldCamera.WorldToScreenPoint(owner.position);

        // Points behind the camera are mirrored by projection. Mirror X back so
        // the hand still represents the owner's actual left/right direction.
        if (screenPoint.z < 0f)
        {
            screenPoint.x = Screen.width - screenPoint.x;
        }

        Camera canvasCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : rootCanvas.worldCamera != null
                ? rootCanvas.worldCamera
                : worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                handParent,
                screenPoint,
                canvasCamera,
                out Vector2 parentLocalPoint))
        {
            return;
        }

        float anchorX = Mathf.Lerp(
            handParent.rect.xMin,
            handParent.rect.xMax,
            handRect.anchorMin.x);

        handRect.anchoredPosition = new Vector2(
            parentLocalPoint.x - anchorX + horizontalOffset,
            bottomOffset);
    }

    private void CacheOwnerRendererStates()
    {
        ownerRenderers = owner.GetComponentsInChildren<Renderer>(true);
    }

    private void SetOwnerRenderersVisible(bool visible)
    {
        for (int index = 0; index < ownerRenderers.Length; index++)
        {
            if (ownerRenderers[index] != null)
            {
                ownerRenderers[index].enabled = visible;
            }
        }
    }

}
