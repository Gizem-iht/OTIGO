using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(AudioSource))]
public class DraggableMazeItem : MonoBehaviour
{
    [Header("Item")]
    public string itemId = "icecream";
    public Transform startPoint;

    [Header("Controller")]
    public MazeLevel2Controller controller;

    [Header("Drag")]
    public float followSpeed = 18f;

    [Header("State")]
    public bool isLocked = false;

    [Header("SFX (Only for maze walls tagged 'Wall')")]
    public AudioClip wallHitClip;
    [Range(0f, 1f)] public float wallHitVolume = 1f;

    private Camera cam;
    private Rigidbody2D rb;
    private Collider2D myCol;
    private AudioSource audioSource;

    private bool dragging;
    private Vector3 offset;
    private Vector2 desiredPos;

    private void Awake()
    {
        cam = Camera.main;
        rb = GetComponent<Rigidbody2D>();
        myCol = GetComponent<Collider2D>();
        audioSource = GetComponent<AudioSource>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        audioSource.playOnAwake = false;
    }

    private void Start()
    {
        ResetToStart();
        desiredPos = rb.position;
    }

    private void Update()
    {
        if (isLocked) return;

        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);

            if (t.phase == TouchPhase.Began) TryBeginDrag(t.position);
            if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary) ContinueDrag(t.position);

            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                dragging = false;
        }
        else
        {
            if (Input.GetMouseButtonDown(0)) TryBeginDrag(Input.mousePosition);
            if (Input.GetMouseButton(0)) ContinueDrag(Input.mousePosition);

            if (Input.GetMouseButtonUp(0))
                dragging = false;
        }
    }

    private void FixedUpdate()
    {
        if (isLocked) return;
        if (!dragging) return;

        Vector2 next = Vector2.Lerp(rb.position, desiredPos, followSpeed * Time.fixedDeltaTime);
        rb.MovePosition(next);
    }

    private void TryBeginDrag(Vector2 screenPos)
    {
        if (isLocked) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector3 world = ScreenToWorld(screenPos);
        RaycastHit2D hit = Physics2D.Raycast(world, Vector2.zero);

        if (hit.collider != null && hit.collider == myCol)
        {
            dragging = true;
            offset = (Vector3)rb.position - world;
        }
    }

    private void ContinueDrag(Vector2 screenPos)
    {
        if (!dragging || isLocked) return;

        Vector3 world = ScreenToWorld(screenPos);
        Vector3 target = world + offset;
        target.z = 0f;

        desiredPos = target;
    }

    private Vector3 ScreenToWorld(Vector2 screenPos)
    {
        if (cam == null) cam = Camera.main;

        Vector3 p = new Vector3(screenPos.x, screenPos.y, Mathf.Abs(cam.transform.position.z));
        Vector3 world = cam.ScreenToWorldPoint(p);
        world.z = 0f;
        return world;
    }

    public void ResetToStart()
    {
        dragging = false;

        if (startPoint != null)
        {
            rb.position = startPoint.position;
            desiredPos = rb.position;
        }
    }

    public void Lock()
    {
        isLocked = true;
        dragging = false;
    }

    public void Unlock()
    {
        isLocked = false;
    }

    private void OnCollisionEnter2D(Collision2D c)
    {
        if (isLocked) return;
        if (c.collider.CompareTag("Wall")) HitWall();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isLocked) return;
        if (other.CompareTag("Wall")) HitWall();
    }

    private void HitWall()
    {
        Debug.Log("Level2 draggable wall hit detected");

        if (controller == null)
        {
            Debug.LogError("DraggableMazeItem controller NULL!");

            if (wallHitClip != null)
                audioSource.PlayOneShot(wallHitClip, wallHitVolume);

            ResetToStart();
            return;
        }

        Debug.Log("Calling MazeLevel2Controller.OnHitWall()");
        controller.OnHitWall();
    }
}