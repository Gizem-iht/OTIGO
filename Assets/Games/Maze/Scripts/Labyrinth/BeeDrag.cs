using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class BeeDrag : MonoBehaviour
{
    public MazeLevel3Controller controller;

    [Header("Drag")]
    public float followSpeed = 25f;

    [Header("Wall Detect")]
    public LayerMask wallLayers; // Inspector'dan Wall layer seç

    private Rigidbody2D rb;
    private Collider2D col;
    private Camera cam;

    private bool dragging = false;
    private bool locked = false;
    private Vector2 grabOffset;

    // Overlap için buffer (GC olmasın diye)
    private readonly Collider2D[] overlapResults = new Collider2D[8];

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        cam = Camera.main;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        col.isTrigger = false;
    }

    void Update()
    {
        if (locked) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mouseWorld = GetMouseWorld();
            RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero);

            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                dragging = true;
                grabOffset = (Vector2)transform.position - mouseWorld;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            dragging = false;
            rb.linearVelocity = Vector2.zero;
        }
    }

    void FixedUpdate()
    {
        if (locked) return;

        // Drag hareket
        if (dragging)
        {
            Vector2 mouseWorld = GetMouseWorld();
            Vector2 target = mouseWorld + grabOffset;

            Vector2 newPos = Vector2.Lerp(rb.position, target, Time.fixedDeltaTime * followSpeed);
            rb.MovePosition(newPos);
        }

        // ✅ Duvara değiyor mu? (Overlap ile %100 kontrol)
        if (controller != null && TouchingWallByOverlap())
        {
            controller.OnHitWall(); // controller içinde inPenalty var, spam’i engeller
        }
    }

    bool TouchingWallByOverlap()
    {
        // ContactFilter: sadece wallLayers
        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = wallLayers;
        filter.useTriggers = false;

        int count = col.Overlap(filter, overlapResults);
        return count > 0;
    }

    Vector2 GetMouseWorld()
    {
        if (cam == null) cam = Camera.main;
        Vector3 w = cam.ScreenToWorldPoint(Input.mousePosition);
        return new Vector2(w.x, w.y);
    }

    public void Lock()
    {
        locked = true;
        dragging = false;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    public void Unlock()
    {
        locked = false;
    }
}