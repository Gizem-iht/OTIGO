using UnityEngine;

public class KidOnlyAnimation : MonoBehaviour
{
    [Header("Zıplama")]
    public float jumpHeight = 15f;
    public float jumpSpeed = 2f;

    [Header("Sağa-Sola Eğilme")]
    public float rotateAngle = 5f;
    public float rotateSpeed = 1.5f;

    private Vector3 startPos;
    private Quaternion startRot;
    private float offset;

    void Start()
    {
        startPos = transform.localPosition;
        startRot = transform.localRotation;

        // Her çocuk aynı anda hareket etmesin
        offset = Random.Range(0f, 2f);
    }

    void Update()
    {
        // Zıplama
        float y = Mathf.Sin((Time.time + offset) * jumpSpeed) * jumpHeight;
        transform.localPosition = startPos + new Vector3(0, y, 0);

        // Hafif sağ-sol dönme
        float z = Mathf.Sin((Time.time + offset) * rotateSpeed) * rotateAngle;
        transform.localRotation = startRot * Quaternion.Euler(0, 0, z);
    }
}
