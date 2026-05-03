using UnityEngine;

public class ObjectSelectKidOnlyAnimation : MonoBehaviour
{
    public float moveAmount = 10f;
    public float moveSpeed = 2f;

    private Vector3 startPos;

    private void Start()
    {
        startPos = transform.localPosition;
    }

    private void Update()
    {
        float offset = Mathf.Sin(Time.time * moveSpeed) * moveAmount;
        transform.localPosition = startPos + new Vector3(0f, offset, 0f);
    }
}