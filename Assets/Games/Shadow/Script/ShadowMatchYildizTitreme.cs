using UnityEngine;

public class ShadowMatchYildizTitreme : MonoBehaviour
{
    public float minSaydamlik = 0.5f;
    public float maxSaydamlik = 1.0f;
    public float titremeHizi = 1f;

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer bulunamadı. Lütfen yıldız nesnesine SpriteRenderer ekleyin.", this);
            enabled = false;
        }
    }

    void Update()
    {
        float saydamlikDegeri = Mathf.Lerp(
            minSaydamlik,
            maxSaydamlik,
            (Mathf.Sin(Time.time * titremeHizi) + 1f) / 2f
        );

        Color renk = spriteRenderer.color;
        renk.a = saydamlikDegeri;
        spriteRenderer.color = renk;
    }
}