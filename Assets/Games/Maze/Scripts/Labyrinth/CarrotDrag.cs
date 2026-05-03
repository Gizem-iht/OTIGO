using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class CarrotDrag : MonoBehaviour
{
    [Header("Controller")]
    public MazeLevel4Controller controller;

    private Rigidbody2D rb;
    private Camera cam;

    private Vector3 offset;
    private bool isDragging = false;
    private bool locked = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cam = Camera.main;

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void OnMouseDown()
    {
        if (locked) return;

        isDragging = true;

        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        offset = transform.position - mouseWorld;
    }

    void OnMouseUp()
    {
        isDragging = false;
    }

    void FixedUpdate()
    {
        if (!isDragging || locked) return;

        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        Vector2 targetPos = (Vector2)(mouseWorld + offset);
        rb.MovePosition(targetPos);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (controller == null) return;

        if (collision.collider.CompareTag("Wall"))
        {
            controller.OnHitWall();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (controller == null) return;

        if (other.CompareTag("Rabbit"))
        {
            Lock();
            controller.OnReachedGoal(transform);
        }
        else if (other.CompareTag("Camel"))
        {
            controller.OnHitCamel();
        }
    }

    public void Lock()
    {
        locked = true;
        isDragging = false;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    public void Unlock()
    {
        locked = false;
    }
}