using UnityEngine;

public class RabbitGoalTrigger : MonoBehaviour
{
    public MazeLevel4Controller controller;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (controller == null) return;

        if (other.CompareTag("Carrot"))
        {
            controller.OnReachedGoal(other.transform);
        }
    }
}