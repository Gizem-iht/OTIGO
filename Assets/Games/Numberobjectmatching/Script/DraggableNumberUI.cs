using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class DraggableNumberUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int numberValue = 4;
    public Canvas rootCanvas;

    private RectTransform rt;
    private CanvasGroup cg;

    private Vector2 originalAnchoredPos;
    private Transform originalParent;

    private bool isLockedForever = false;
    private bool inputEnabled = true;
    private bool isDragging = false;

    public bool IsLockedForever => isLockedForever;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();

        if (cg == null)
            cg = gameObject.AddComponent<CanvasGroup>();

        cg.blocksRaycasts = true;

        originalParent = transform.parent;
        originalAnchoredPos = rt.anchoredPosition;
    }

    private void Start()
    {
        if (originalParent == null)
            originalParent = transform.parent;

        originalAnchoredPos = rt.anchoredPosition;
    }

    private void Update()
    {
        if (ParentModeManager.Instance != null && ParentModeManager.Instance.IsPatternPanelOpen)
        {
            if (isDragging)
                CancelDragAndReset();
        }
    }

    public void SetInputEnabled(bool enabled)
    {
        if (isLockedForever) return;

        inputEnabled = enabled;
        cg.blocksRaycasts = enabled;
    }

    public void LockInPlace()
    {
        isLockedForever = true;
        inputEnabled = false;
        isDragging = false;
        cg.blocksRaycasts = false;
    }

    public void ResetToStart()
    {
        if (isLockedForever) return;

        if (originalParent != null)
            transform.SetParent(originalParent, false);

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = originalAnchoredPos;

        cg.blocksRaycasts = inputEnabled;
        isDragging = false;
    }

    public void ForcePlaceIntoDropZone(Transform zoneTransform)
    {
        if (zoneTransform == null) return;

        transform.SetParent(zoneTransform, false);
        transform.SetAsLastSibling();

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;

        LockInPlace();
    }

    public void ForceUnlockToStart()
    {
        isLockedForever = false;
        inputEnabled = true;
        isDragging = false;

        if (originalParent != null)
            transform.SetParent(originalParent, false);

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = originalAnchoredPos;
        rt.localScale = Vector3.one;

        cg.blocksRaycasts = true;
    }

    private void CancelDragAndReset()
    {
        if (isLockedForever) return;

        isDragging = false;
        ResetToStart();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isLockedForever || !inputEnabled) return;

        if (ParentModeManager.Instance != null && ParentModeManager.Instance.IsPatternPanelOpen)
            return;

        isDragging = true;
        cg.blocksRaycasts = false;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isLockedForever || !inputEnabled) return;
        if (!isDragging) return;

        if (ParentModeManager.Instance != null && ParentModeManager.Instance.IsPatternPanelOpen)
        {
            CancelDragAndReset();
            return;
        }

        if (rootCanvas == null) return;

        rt.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isLockedForever || !inputEnabled) return;
        if (!isDragging) return;

        isDragging = false;
        cg.blocksRaycasts = true;

        if (eventData.pointerEnter == null ||
            eventData.pointerEnter.GetComponentInParent<DropZoneUI>() == null)
        {
            ResetToStart();
        }
    }
}