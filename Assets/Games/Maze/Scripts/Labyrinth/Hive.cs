using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HideZone : MonoBehaviour
{
    public MazeLevel3Controller controller;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (controller == null) return;

        // Bee objesi mi?
        BeeDrag bee = other.GetComponent<BeeDrag>();
        if (bee != null)
        {
            controller.OnReachedHide();
        }
    }
}