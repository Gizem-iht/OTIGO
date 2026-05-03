using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MouthGoal : MonoBehaviour
{
    public MazeLevel7Controller controller;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (controller == null) return;

        ToothbrushDrag brush = other.GetComponent<ToothbrushDrag>();
        if (brush != null)
        {
            controller.OnReachedMouth();
        }
    }
}