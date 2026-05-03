using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SchoolGoal : MonoBehaviour
{
    public MazeLevel5Controller controller;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (controller == null) return;

        // kız objesi GirlDrag taşıyorsa win
        if (other.GetComponent<GirlDrag>() != null)
        {
            controller.OnReachedGoal();
        }
    }
}