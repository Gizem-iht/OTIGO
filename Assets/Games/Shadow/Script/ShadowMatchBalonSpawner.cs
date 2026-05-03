using UnityEngine;

public class ShadowMatchBalonSpawner : MonoBehaviour
{
    [Header("Balon Üretim Ayarları")]
    public GameObject[] balonPrefablar;

    public float minSpawnTime = 0.5f;
    public float maxSpawnTime = 1.5f;

    public float minXSpawn = -7f;
    public float maxXSpawn = 7f;
    public float spawnYPosition = -6f;

    [Header("Ses Kontrolü")]
    public AudioSource arkaPlanMuzigiSource;
    public AudioClip girisKonusmaSesi;

    public float konusmaOncesiMuzikSesi = 0.2f;
    public float normalMuzikSesi = 0.5f;

    private float sonrakiSpawnZamani;
    private AudioSource konusmaSource;

    void Start()
    {
        konusmaSource = gameObject.AddComponent<AudioSource>();

        BaslangicSesleriniAyarla();
        sonrakiSpawnZamani = Time.time + Random.Range(minSpawnTime, maxSpawnTime);
    }

    void BaslangicSesleriniAyarla()
    {
        if (arkaPlanMuzigiSource != null)
        {
            arkaPlanMuzigiSource.volume = konusmaOncesiMuzikSesi;
        }

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

    void MuzikYukselt()
    {
        if (arkaPlanMuzigiSource != null)
        {
            arkaPlanMuzigiSource.volume = normalMuzikSesi;
        }
    }

    void Update()
    {
        if (Time.time >= sonrakiSpawnZamani)
        {
            SpawnBalon();
            sonrakiSpawnZamani = Time.time + Random.Range(minSpawnTime, maxSpawnTime);
        }
    }

    void SpawnBalon()
    {
        if (balonPrefablar == null || balonPrefablar.Length == 0) return;

        int rastgeleBalonIndex = Random.Range(0, balonPrefablar.Length);
        GameObject secilenBalon = balonPrefablar[rastgeleBalonIndex];

        float rastgeleX = Random.Range(minXSpawn, maxXSpawn);
        Vector3 spawnKonumu = new Vector3(rastgeleX, spawnYPosition, 0f);

        Instantiate(secilenBalon, spawnKonumu, Quaternion.identity);
    }
}