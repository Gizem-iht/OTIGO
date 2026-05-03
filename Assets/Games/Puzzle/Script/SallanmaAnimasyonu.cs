using UnityEngine;

public class SallanmaAnimasyonu : MonoBehaviour
{
    // Inspector'da ayarlanacaklar
    [Header("Salınım Ayarları")]
    public float maksimumAçı = 5f; // Objenin merkezden sağa veya sola ne kadar döneceği (derece)
    public float salınımHızı = 1.5f; // Sallanmanın hızı (Ne kadar hızlı ileri geri gideceği)

    // Not: Bu animasyon Rotasyonu (Dönüşü) değiştirecektir.

    void Update()
    {
        // 1. Sinüs Değerini Hesaplama
        // Sinüs fonksiyonu (Mathf.Sin), Time.time * salınımHızı sayesinde 
        // sürekli olarak -1 ile +1 arasında akıcı bir değer üretir.
        float sinValue = Mathf.Sin(Time.time * salınımHızı);
        
        // 2. Açıyı Hesaplama
        // Bu sinüs değerini, belirlediğimiz maksimum açı ile çarparak 
        // objenin dönüş açısını (-maksimumAçı ile +maksimumAçı arasında) elde ederiz.
        float açı = sinValue * maksimumAçı;

        // 3. Rotasyonu Uygulama
        // Quaternion.Euler kullanarak bu açıyı Z eksenine (2D'de dönme ekseni) uygularız.
        transform.rotation = Quaternion.Euler(0f, 0f, açı);
    }
}