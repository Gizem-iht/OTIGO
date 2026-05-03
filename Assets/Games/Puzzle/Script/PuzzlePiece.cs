using UnityEngine;

public class PuzzlePiece : MonoBehaviour
{
    [Header("Unique ID")]
    public string pieceId;

    [Header("Hedef & Ayarlar")]
    public Transform targetPosition;
    public float snapDistance = 0.5f;
    public AudioClip snapSound;

    [Header("Yanlışta Davranış")]
    public bool returnToStartOnWrong = true;
    public AudioClip wrongSound;

    private AudioSource audioSource;
    private PuzzleGameManager manager;

    private bool isPlaced = false;
    private bool isDragging = false;

    private int defaultSortingOrder;
    private Vector3 startPos;
    private SpriteRenderer sr;

    private Rigidbody2D rb;
    private Collider2D col;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;

        sr = GetComponent<SpriteRenderer>();

        if (sr != null)
            defaultSortingOrder = sr.sortingOrder;

        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        startPos = transform.position;

        if (string.IsNullOrEmpty(pieceId))
            pieceId = gameObject.name;
    }

    private void Start()
    {
        if (manager == null)
            manager = FindObjectOfType<PuzzleGameManager>();

        if (manager == null)
            Debug.LogError(name + ": PuzzleGameManager sahnede bulunamadı!");
    }

    private void Update()
    {
        if (ParentModeManager.Instance != null && ParentModeManager.Instance.IsPatternPanelOpen)
        {
            if (isDragging)
            {
                isDragging = false;

                if (sr != null)
                    sr.sortingOrder = defaultSortingOrder;
            }
        }
    }

    public void SetManager(PuzzleGameManager newManager)
    {
        manager = newManager;
    }

    public string GetSaveId()
    {
        if (string.IsNullOrEmpty(pieceId))
            pieceId = gameObject.name;

        return pieceId;
    }

    private void OnMouseDown()
    {
        if (isPlaced) return;

        if (ParentModeManager.Instance != null && ParentModeManager.Instance.IsPatternPanelOpen)
            return;

        isDragging = true;

        if (sr != null)
            sr.sortingOrder = 100;
    }

    private void OnMouseDrag()
    {
        if (isPlaced) return;
        if (!isDragging) return;

        if (ParentModeManager.Instance != null && ParentModeManager.Instance.IsPatternPanelOpen)
            return;

        if (Camera.main == null) return;

        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = transform.position.z;
        transform.position = mousePosition;
    }

    private void OnMouseUp()
    {
        if (isPlaced) return;

        if (ParentModeManager.Instance != null && ParentModeManager.Instance.IsPatternPanelOpen)
        {
            isDragging = false;

            if (sr != null)
                sr.sortingOrder = defaultSortingOrder;

            return;
        }

        isDragging = false;

        if (sr != null)
            sr.sortingOrder = defaultSortingOrder;

        if (targetPosition == null)
        {
            Debug.LogWarning(name + ": targetPosition atanmadı!");
            return;
        }

        float distance = Vector3.Distance(transform.position, targetPosition.position);

        if (distance <= snapDistance)
        {
            ForcePlaceToTarget();

            if (audioSource != null && snapSound != null)
                audioSource.PlayOneShot(snapSound);

            if (manager != null)
                manager.RegisterCorrect(this);
        }
        else
        {
            if (returnToStartOnWrong)
                transform.position = startPos;

            if (audioSource != null && wrongSound != null)
                audioSource.PlayOneShot(wrongSound);

            if (manager != null)
                manager.RegisterWrong();
        }
    }

    public void ForcePlaceToTarget()
    {
        if (targetPosition == null) return;

        transform.position = targetPosition.position;
        isPlaced = true;
        isDragging = false;

        if (sr != null)
            sr.sortingOrder = defaultSortingOrder;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }

        if (col != null)
            col.enabled = false;
    }

    public void ForceResetToStart()
    {
        isPlaced = false;
        isDragging = false;

        transform.position = startPos;

        if (sr != null)
            sr.sortingOrder = defaultSortingOrder;

        if (rb != null)
            rb.simulated = true;

        if (col != null)
            col.enabled = true;
    }
}