using UnityEngine;

public class TargetZone : MonoBehaviour
{
    public string acceptsItemId = "icecream";
    public MazeLevel2Controller levelController;

    [Header("Snap")]
    public Transform snapPoint;
    public bool parentToTarget = true;

    [Header("Precision")]
    public float snapDistance = 0.35f;

    private void OnTriggerStay2D(Collider2D other)
    {
        DraggableMazeItem item = other.GetComponent<DraggableMazeItem>();
        if (item == null) return;
        if (item.isLocked) return;
        if (item.itemId != acceptsItemId) return;

        Vector3 pos = snapPoint != null ? snapPoint.position : transform.position;
        float d = Vector2.Distance(item.transform.position, pos);

        if (d > snapDistance) return;

        item.isLocked = true;

        Collider2D col = item.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        item.transform.position = new Vector3(pos.x, pos.y, 0f);

        if (parentToTarget)
            item.transform.SetParent(transform);

        if (levelController != null)
            levelController.OnCombinedWin(transform);
    }
}