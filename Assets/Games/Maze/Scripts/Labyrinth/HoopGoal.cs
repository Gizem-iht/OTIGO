using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HoopGoal : MonoBehaviour
{
    public MazeLevel6Controller controller;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (controller == null) return;

        if (other.GetComponent<BallDrag>() != null)
        {
            controller.OnReachedHoop();
        }
    }
}