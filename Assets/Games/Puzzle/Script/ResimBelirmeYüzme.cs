using UnityEngine;
using System.Collections; // IEnumerator için

public class ResimBelirmeYuzme : MonoBehaviour
{
    // Inspector'da ayarlanacaklar
    [Header("Belirme Ayarları")]
    public float belirmeSuresi = 1.0f;     // Resmin tamamen belirmesi kaç saniye sürecek
    public float beklemeSuresi = 0.5f;     // Belirmeden sonra yüzmeye başlamadan önceki bekleme süresi

    [Header("Yüzme Ayarları")]
    public float yuzmeMiktari = 0.1f;      // Yukarı/aşağı yüzme mesafesi (Unity birimi)
    public float yuzmeHizi = 1.0f;         // Yüzme hızı
    public bool yatayYuzmeDeVar = false;   // Yana doğru da yüzsün mü?

    private SpriteRenderer spriteRenderer;
    private Vector3 baslangicPozisyonu;
    private Color baslangicRenk;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("ResimBelirmeYuzme script'i SpriteRenderer bileşeni üzerinde değil!");
            enabled = false;
            return;
        }

        baslangicPozisyonu = transform.position;
        baslangicRenk = spriteRenderer.color;

        // Başlangıçta resmi tamamen şeffaf yap
        baslangicRenk.a = 0f;
        spriteRenderer.color = baslangicRenk;

        // Belirme animasyonunu başlat
        StartCoroutine(BelirmeVeYuzmeRutini());
    }

    IEnumerator BelirmeVeYuzmeRutini()
    {
        // 1. Belirme (Fade-In) Kısmı
        float gecenSure = 0f;
        while (gecenSure < belirmeSuresi)
        {
            gecenSure += Time.deltaTime;
            float t = gecenSure / belirmeSuresi;
            Color currentAlpha = baslangicRenk;
            currentAlpha.a = Mathf.Lerp(0f, 1f, t);
            spriteRenderer.color = currentAlpha;
            yield return null; // Bir sonraki kareye kadar bekle
        }
        // Tamamen belirdiğinde opaklığı garantile
        spriteRenderer.color = new Color(baslangicRenk.r, baslangicRenk.g, baslangicRenk.b, 1f);

        // 2. Bekleme Kısmı
        yield return new WaitForSeconds(beklemeSuresi);

        // 3. Yüzme Kısmı (Update'de devam edecek)
        // Artık Update metodunda yüzme animasyonu çalışacak.
        // Bu korutin burada sona erer.
    }

    void Update()
    {
        // Sadece belirme tamamlandıktan sonra yüzmeye başla
        if (spriteRenderer.color.a >= 1f)
        {
            float dikeyOffset = Mathf.Sin(Time.time * yuzmeHizi) * yuzmeMiktari;
            float yatayOffset = 0f;
            if (yatayYuzmeDeVar)
            {
                // Yatay yüzme için farklı bir sinüs döngüsü veya faz farkı kullanabiliriz
                yatayOffset = Mathf.Cos(Time.time * yuzmeHizi * 0.7f) * yuzmeMiktari * 0.5f; 
            }

            transform.position = baslangicPozisyonu + new Vector3(yatayOffset, dikeyOffset, 0f);
        }
    }
}