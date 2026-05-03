using UnityEngine;
using UnityEngine.EventSystems;

public class DropZoneUI : MonoBehaviour, IDropHandler
{
    [Header("Identity")]
    public string dropZoneId = "zone_1";

    public int correctNumber = 4;

    [Header("Controller")]
    public NumberMatchBaseController controller;

    [Header("Target Scale")]
    public RectTransform targetImageToScale;
    public float scaleUpFactor = 1.15f;

    private bool completed = false;
    private Vector3 startScale = Vector3.one;

    public bool IsCompleted => completed;

    private void Awake()
    {
        if (targetImageToScale != null)
            startScale = targetImageToScale.localScale;
    }

    private void Start()
    {
        if (targetImageToScale != null)
            startScale = targetImageToScale.localScale;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (completed) return;

        GameObject dragged = eventData.pointerDrag;
        if (dragged == null) return;

        DraggableNumberUI dn = dragged.GetComponent<DraggableNumberUI>();
        if (dn == null) return;

        if (dn.numberValue == correctNumber)
        {
            CompleteWithNumber(dn, true);
        }
        else
        {
            controller?.OnWrong();
            dn.ResetToStart();
        }
    }

    public void CompleteWithNumber(DraggableNumberUI dn, bool notifyController)
    {
        if (completed) return;
        if (dn == null) return;

        completed = true;

        dn.ForcePlaceIntoDropZone(transform);

        if (targetImageToScale != null)
            targetImageToScale.localScale = startScale * scaleUpFactor;

        if (notifyController)
            controller?.OnCorrectDrop(this, dn);
    }

    public void RestoreCompleted(DraggableNumberUI dn)
    {
        CompleteWithNumber(dn, false);
    }

    public void ResetZone()
    {
        completed = false;

        if (targetImageToScale != null)
            targetImageToScale.localScale = startScale;
    }
}