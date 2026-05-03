using UnityEngine;

public class ShadowMatchBalon : MonoBehaviour
{
    public float yukariCikisHizi = 2f;
    public float kaybolmaYKonumu = 6f;

    void Update()
    {
        transform.Translate(Vector2.up * yukariCikisHizi * Time.deltaTime);

        if (transform.position.y > kaybolmaYKonumu)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Karakter"))
        {
            Destroy(gameObject);
        }
    }
}