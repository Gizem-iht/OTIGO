using UnityEngine;
using System.Collections; // Coroutine kullanmak için gerekli

public class YildizEfekti : MonoBehaviour
{
    [Header("Parlaklık Ayarları")]
    [Range(0.0f, 1.0f)]
    public float minSaydamlik = 0.5f; 
    [Range(0.0f, 1.0f)]
    public float maxSaydamlik = 1.0f; 

    public float titremeHizi = 1f; // Titreme hızı (daha yüksek değer, daha hızlı titreme)

    private SpriteRenderer spriteRenderer;
    private float mevcutZaman;

    void Start()
    {
        // SpriteRenderer bileşenini al
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer bileşeni bulunamadı! Script devre dışı.", this);
            enabled = false; 
            return;
        }

        // Coroutine'i başlat
        StartCoroutine(TitremeDongusu());
    }

    // Bu, kendi özel döngümüzdür. Unity'nin Update'inden bağımsız çalışır.
    IEnumerator TitremeDongusu()
    {
        // Sonsuz döngü
        while (true)
        {
            // Sinüs dalgası kullanarak 0 ile 1 arasında yumuşak bir değer üretir.
            // Bu değeri yumuşak bir geçiş için kullanacağız.
            float sinValue = (Mathf.Sin(mevcutZaman * titremeHizi) + 1f) / 2f;
            
            // Lerp ile saydamlık aralığında bir değer bulur.
            float saydamlikDegeri = Mathf.Lerp(minSaydamlik, maxSaydamlik, sinValue);

            // Saydamlığı uygula
            Color renk = spriteRenderer.color;
            renk.a = saydamlikDegeri; 
            spriteRenderer.color = renk;

            // Zamanı ilerlet
            mevcutZaman += Time.deltaTime;

            // Bir sonraki karede devam et
            yield return null; 
        }
    }
}