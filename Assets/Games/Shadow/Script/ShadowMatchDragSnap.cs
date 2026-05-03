using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ShadowMatchDragSnap : MonoBehaviour
{
    [Header("Unique ID")]
    public string pieceId;

    [Header("Target & UI")]
    public Transform target;

    [Header("Behaviour")]
    public float snapRadius = 0.8f;
    public bool lockOnSnap = true;
    public bool returnIfMiss = false;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip clapClip;
    public AudioClip wrongClip;

    private Camera cam;
    private SpriteRenderer sr;
    private Collider2D col;

    private int startOrder;
    private Vector3 startPos;
    private Vector3 offset;

    private bool dragging = false;
    private bool locked = false;

    private ShadowMatchLevelController levelController;

    private void Awake()
    {
        cam = Camera.main;
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        startPos = transform.position;
        startOrder = sr != null ? sr.sortingOrder : 0;

        if (string.IsNullOrEmpty(pieceId))
            pieceId = gameObject.name;

        levelController = FindObjectOfType<ShadowMatchLevelController>();

        if (cam == null)
            Debug.LogError("[ShadowMatchDragSnap] Main Camera bulunamadı!");

        if (levelController == null)
            Debug.LogError("[ShadowMatchDragSnap] ShadowMatchLevelController sahnede yok!");
    }

    private void Update()
    {
        if (ParentModeManager.Instance != null &&
            ParentModeManager.Instance.IsPatternPanelOpen)
        {
            if (dragging)
            {
                dragging = false;

                if (sr != null)
                    sr.sortingOrder = startOrder;
            }
        }
    }

    private void OnMouseDown()
    {
        if (locked || cam == null) return;

        if (ParentModeManager.Instance != null &&
            ParentModeManager.Instance.IsPatternPanelOpen)
        {
            return;
        }

        Vector3 m = cam.ScreenToWorldPoint(Input.mousePosition);
        m.z = 0f;

        Collider2D hit = Physics2D.OverlapPoint(m);

        if (hit != null && hit.transform != transform)
            return;

        dragging = true;
        offset = transform.position - m;

        if (sr != null)
            sr.sortingOrder = 10;
    }

    private void OnMouseDrag()
    {
        if (!dragging || locked || cam == null) return;

        if (ParentModeManager.Instance != null &&
            ParentModeManager.Instance.IsPatternPanelOpen)
        {
            return;
        }

        Vector3 m = cam.ScreenToWorldPoint(Input.mousePosition);
        m.z = 0f;

        transform.position = m + offset;
    }

    private void OnMouseUp()
    {
        if (locked) return;

        if (ParentModeManager.Instance != null &&
            ParentModeManager.Instance.IsPatternPanelOpen)
        {
            dragging = false;

            if (sr != null)
                sr.sortingOrder = startOrder;

            return;
        }

        dragging = false;

        if (sr != null)
            sr.sortingOrder = startOrder;

        if (levelController != null)
            levelController.CheckPlacement(this, transform.position);
    }

    public void ResetPiece()
    {
        ForceResetToStart();
    }

    public void LockPiece(bool isLocked)
    {
        locked = isLocked;

        if (isLocked)
        {
            dragging = false;

            if (sr != null)
                sr.sortingOrder = startOrder;
        }
    }

    public void ForcePlaceToTarget()
    {
        if (target == null) return;

        transform.position = target.position;
        locked = true;
        dragging = false;

        if (sr != null)
            sr.sortingOrder = startOrder;

        if (col != null)
            col.enabled = true;
    }

    public void ForceResetToStart()
    {
        locked = false;
        dragging = false;

        transform.position = startPos;

        if (sr != null)
            sr.sortingOrder = startOrder;

        if (col != null)
            col.enabled = true;
    }

    public string GetSaveId()
    {
        if (string.IsNullOrEmpty(pieceId))
            pieceId = gameObject.name;

        return pieceId;
    }

    public Vector3 GetStartPos()
    {
        return startPos;
    }

    public bool IsLocked
    {
        get { return locked; }
    }
}