using UnityEngine;

public class ShadowMatchKarakterKontrol : MonoBehaviour
{
    public float hareketHizi = 6f;
    public string yatayEksenAdi = "Horizontal";

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        float yatayGiris = Input.GetAxis(yatayEksenAdi);
        Vector2 yeniHiz = new Vector2(yatayGiris * hareketHizi, 0f);
        rb.linearVelocity = yeniHiz;
    }
}