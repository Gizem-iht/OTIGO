using UnityEngine;
using System.Collections;

public class ShadowMatchYildizEfekti : MonoBehaviour
{
    [Header("Parlaklık Ayarları")]
    [Range(0.0f, 1.0f)] public float minSaydamlik = 0.5f;
    [Range(0.0f, 1.0f)] public float maxSaydamlik = 1.0f;
    public float titremeHizi = 1f;

    private SpriteRenderer spriteRenderer;
    private float mevcutZaman;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer bileşeni bulunamadı! Script devre dışı.", this);
            enabled = false;
            return;
        }

        StartCoroutine(TitremeDongusu());
    }

    IEnumerator TitremeDongusu()
    {
        while (true)
        {
            float sinValue = (Mathf.Sin(mevcutZaman * titremeHizi) + 1f) / 2f;
            float saydamlikDegeri = Mathf.Lerp(minSaydamlik, maxSaydamlik, sinValue);

            Color renk = spriteRenderer.color;
            renk.a = saydamlikDegeri;
            spriteRenderer.color = renk;

            mevcutZaman += Time.deltaTime;
            yield return null;
        }
    }
}