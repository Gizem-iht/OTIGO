using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WrongGoal : MonoBehaviour
{
    public enum WrongType
    {
        Eye,
        Ear
    }

    public WrongType wrongType;
    public MazeLevel7Controller controller;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (controller == null) return;

        ToothbrushDrag brush = other.GetComponent<ToothbrushDrag>();
        if (brush == null) return;

        if (wrongType == WrongType.Eye)
            controller.OnEyeWrong();
        else
            controller.OnEarWrong();
    }
}