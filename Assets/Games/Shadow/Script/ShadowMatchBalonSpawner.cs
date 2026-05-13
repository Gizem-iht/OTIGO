using UnityEngine;

public class ShadowMatchBalonSpawner : MonoBehaviour
{
    [Header("Balon Uretim Ayarlari")]
    public GameObject[] balonPrefablar;
    public bool useFallbackBalloonsWhenEmpty = true;

    public float minSpawnTime = 0.5f;
    public float maxSpawnTime = 1.5f;

    public float minXSpawn = -7f;
    public float maxXSpawn = 7f;
    public float spawnYPosition = -6f;

    [Header("Ses Kontrolu")]
    public AudioSource arkaPlanMuzigiSource;
    public AudioClip girisKonusmaSesi;

    public float konusmaOncesiMuzikSesi = 0.2f;
    public float normalMuzikSesi = 0.5f;

    private static Sprite fallbackBalloonSprite;
    private float sonrakiSpawnZamani;
    private AudioSource konusmaSource;

    private readonly Color[] fallbackColors =
    {
        new Color(1f, 0.25f, 0.25f),
        new Color(0.2f, 0.65f, 1f),
        new Color(1f, 0.85f, 0.2f),
        new Color(0.35f, 0.9f, 0.45f),
        new Color(1f, 0.45f, 0.9f),
        new Color(0.75f, 0.45f, 1f)
    };

    private void Start()
    {
        konusmaSource = gameObject.AddComponent<AudioSource>();

        BaslangicSesleriniAyarla();
        sonrakiSpawnZamani = Time.time + Random.Range(minSpawnTime, maxSpawnTime);
    }

    private void BaslangicSesleriniAyarla()
    {
        if (arkaPlanMuzigiSource != null)
            arkaPlanMuzigiSource.volume = konusmaOncesiMuzikSesi;

        if (girisKonusmaSesi != null && konusmaSource != null)
        {
            konusmaSource.PlayOneShot(girisKonusmaSesi);
            Invoke(nameof(MuzikYukselt), girisKonusmaSesi.length);
        }
        else if (arkaPlanMuzigiSource != null)
        {
            arkaPlanMuzigiSource.volume = normalMuzikSesi;
        }
    }

    private void MuzikYukselt()
    {
        if (arkaPlanMuzigiSource != null)
            arkaPlanMuzigiSource.volume = normalMuzikSesi;
    }

    private void Update()
    {
        if (Time.time < sonrakiSpawnZamani)
            return;

        SpawnBalon();
        sonrakiSpawnZamani = Time.time + Random.Range(minSpawnTime, maxSpawnTime);
    }

    private void SpawnBalon()
    {
        GameObject secilenBalon = GetRandomAssignedBalloonPrefab();

        if (secilenBalon == null)
        {
            if (useFallbackBalloonsWhenEmpty)
                SpawnFallbackBalloon();

            return;
        }

        float rastgeleX = Random.Range(minXSpawn, maxXSpawn);
        Vector3 spawnKonumu = new Vector3(rastgeleX, spawnYPosition, 0f);

        Instantiate(secilenBalon, spawnKonumu, Quaternion.identity);
    }

    private GameObject GetRandomAssignedBalloonPrefab()
    {
        if (balonPrefablar == null || balonPrefablar.Length == 0)
            return null;

        int validCount = 0;
        for (int i = 0; i < balonPrefablar.Length; i++)
        {
            if (balonPrefablar[i] != null)
                validCount++;
        }

        if (validCount == 0)
            return null;

        int selectedValidIndex = Random.Range(0, validCount);
        int currentValidIndex = 0;

        for (int i = 0; i < balonPrefablar.Length; i++)
        {
            if (balonPrefablar[i] == null)
                continue;

            if (currentValidIndex == selectedValidIndex)
                return balonPrefablar[i];

            currentValidIndex++;
        }

        return null;
    }

    private void SpawnFallbackBalloon()
    {
        float rastgeleX = Random.Range(minXSpawn, maxXSpawn);
        Vector3 spawnKonumu = new Vector3(rastgeleX, spawnYPosition, 0f);

        GameObject balloon = new GameObject("FallbackBalloon");
        balloon.transform.position = spawnKonumu;
        balloon.transform.localScale = Vector3.one * Random.Range(0.35f, 0.65f);

        SpriteRenderer renderer = balloon.AddComponent<SpriteRenderer>();
        renderer.sprite = GetFallbackBalloonSprite();
        renderer.color = fallbackColors[Random.Range(0, fallbackColors.Length)];
        renderer.sortingOrder = 20;

        FallbackBalloonMotion motion = balloon.AddComponent<FallbackBalloonMotion>();
        motion.speed = Random.Range(1.2f, 2.2f);
        motion.swayAmount = Random.Range(0.2f, 0.55f);
        motion.lifeSeconds = Random.Range(5f, 7f);
    }

    private Sprite GetFallbackBalloonSprite()
    {
        if (fallbackBalloonSprite != null)
            return fallbackBalloonSprite;

        Texture2D texture = new Texture2D(64, 64, TextureFormat.ARGB32, false);
        Color clear = new Color(1f, 1f, 1f, 0f);
        Vector2 center = new Vector2(32f, 34f);

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                Vector2 p = new Vector2(x, y);
                Vector2 normalized = new Vector2((p.x - center.x) / 23f, (p.y - center.y) / 27f);
                bool insideBalloon = normalized.sqrMagnitude <= 1f;
                bool insideTie = y >= 5 && y <= 13 && Mathf.Abs(x - 32) <= (13 - y);

                texture.SetPixel(x, y, insideBalloon || insideTie ? Color.white : clear);
            }
        }

        texture.Apply();
        fallbackBalloonSprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64f);
        return fallbackBalloonSprite;
    }
}

public class FallbackBalloonMotion : MonoBehaviour
{
    public float speed = 1.5f;
    public float swayAmount = 0.35f;
    public float lifeSeconds = 6f;

    private float startX;
    private float startTime;
    private float swaySeed;

    private void Start()
    {
        startX = transform.position.x;
        startTime = Time.time;
        swaySeed = Random.Range(0f, 10f);
    }

    private void Update()
    {
        float age = Time.time - startTime;
        Vector3 position = transform.position;
        position.y += speed * Time.deltaTime;
        position.x = startX + Mathf.Sin((age * 2.2f) + swaySeed) * swayAmount;
        transform.position = position;

        if (age >= lifeSeconds)
            Destroy(gameObject);
    }
}
