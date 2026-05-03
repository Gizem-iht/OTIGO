using UnityEngine;

public class CamelTrigger : MonoBehaviour
{
    public MazeLevel4Controller controller;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (controller == null) return;

        if (other.CompareTag("Carrot"))
        {
            controller.OnHitCamel();
        }
    }
}