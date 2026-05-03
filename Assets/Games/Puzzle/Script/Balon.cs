using UnityEngine;

public class Balon : MonoBehaviour
{
    public float yukariCikisHizi = 2f; 
    public float kaybolmaYKonumu = 6f; 

    void Update()
    {
        // Balonu yukarı hareket ettir
        transform.Translate(Vector2.up * yukariCikisHizi * Time.deltaTime);

        // Ekran dışına çıkarsa yok et
        if (transform.position.y > kaybolmaYKonumu)
        {
            Destroy(gameObject);
        }
    }

    // Karakter ile temas algılandığında
    void OnTriggerEnter2D(Collider2D other)
    {
        // Çarpan nesne "Karakter" Tag'i taşıyor mu?
        if (other.CompareTag("Karakter")) 
        {
            // BURAYA SES EFEKTİ VE PUAN KODU GELECEK
            Destroy(gameObject);
        }
    }
}