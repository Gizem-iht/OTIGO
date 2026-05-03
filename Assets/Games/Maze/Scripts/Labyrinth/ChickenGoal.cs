using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ChickenGoal : MonoBehaviour
{
    public MazeLevel1Controller controller;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (controller == null) return;

        ChickDrag chick = other.GetComponent<ChickDrag>();
        if (chick != null)
        {
            controller.OnReachedChicken();
        }
    }
}