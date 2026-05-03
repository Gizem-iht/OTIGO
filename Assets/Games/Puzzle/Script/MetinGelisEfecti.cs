using UnityEngine;
using TMPro;

public class MetinGelisEfecti : MonoBehaviour
{
    // Inspector'da ayarlanacaklar
    [Header("Animasyon Ayarları")]
    public float süre = 1.0f;           // Animasyonun toplam süresi
    public float yukariKaymaMiktari = 50f; // Aşağıdan yukarı ne kadar kayarak geleceği (piksel/Unity birimi)
    public AnimationCurve hizEgrisi;     // Animasyonun hızlanma/yavaşlama eğrisi (Sıçrama hissi için)

    private TextMeshProUGUI textMeshPro;
    private Vector3 baslangicPozisyonu;
    private Color hedefRenk;
    private float gecenSure = 0f;

    void Start()
    {
        textMeshPro = GetComponent<TextMeshProUGUI>();
        if (textMeshPro == null)
        {
            Debug.LogError("MetinGelisEfecti script'i TextMeshProUGUI bileşeni üzerinde değil!");
            enabled = false;
            return;
        }

        // 1. Başlangıç Değerlerini Kaydet
        baslangicPozisyonu = transform.position;
        hedefRenk = textMeshPro.color;

        // 2. Başlangıç Durumunu Ayarla (Gizle ve Aşağı Kaydır)
        // Başlangıçta tam şeffaf yap
        Color baslangicRenk = hedefRenk;
        baslangicRenk.a = 0f;
        textMeshPro.color = baslangicRenk;
        
        // Başlangıç pozisyonunu, yukarı kayma miktarı kadar aşağı it
        transform.position = baslangicPozisyonu + Vector3.down * yukariKaymaMiktari;
    }

    void Update()
    {
        gecenSure += Time.deltaTime;

        // Animasyon süresini 0 ile 1 arasına normalleştir
        float t = gecenSure / süre;

        // Sürenin dolduğunu kontrol et ve bitir
        if (t >= 1f)
        {
            // Son konumu garantile
            textMeshPro.color = hedefRenk;
            transform.position = baslangicPozisyonu; 
            enabled = false; 
            return;
        }
        
        // Animasyon Eğrisini Uygula (Yumuşak veya zıplayan bir hareket sağlar)
        // Eğer Inspector'da bir eğri atarsanız, t değeri ona göre değişir.
        float curveValue = hizEgrisi.Evaluate(t); 

        // 1. Pozisyon Animasyonu (Aşağıdan Yukarı Kayma)
        // Vector3.Lerp: Başlangıç (aşağıda) ile Hedef (yukarıda) arasında ilerle
        transform.position = Vector3.Lerp(
            baslangicPozisyonu + Vector3.down * yukariKaymaMiktari, // Başlangıç (Aşağıda)
            baslangicPozisyonu,                                     // Hedef (Orijinal Pozisyon)
            curveValue // Animasyon eğrisini kullan
        );
        
        // 2. Şeffaflık Animasyonu (Belirme)
        // Mathf.Lerp: Şeffaflığı 0'dan 1'e doğru artır
        Color yeniRenk = hedefRenk;
        yeniRenk.a = Mathf.Lerp(0f, hedefRenk.a, curveValue);
        
        textMeshPro.color = yeniRenk;
    }
}