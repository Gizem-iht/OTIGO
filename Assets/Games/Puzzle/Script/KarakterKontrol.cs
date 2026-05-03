using UnityEngine;

public class KarakterKontrol : MonoBehaviour
{
    public float hareketHizi = 6f; 
    public string yatayEksenAdi = "Horizontal"; 

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // Eğer Start'ta Rigidbody bulunamazsa, bu hata oluşmaz, ama çalışmaz.
        // Bu yüzden Rigidbody'nin karaktere EKLİ olduğundan emin olun!
    }

    void FixedUpdate()
    {
        if (rb == null) return; // Rigidbody yoksa kodu çalıştırma (hata vermemek için geçici önlem)

        float yatayGiris = Input.GetAxis(yatayEksenAdi);
        Vector2 yeniHiz = new Vector2(yatayGiris * hareketHizi, 0f);
        rb.linearVelocity = yeniHiz;
    }
}