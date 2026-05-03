using UnityEngine;
using System.Collections; 

public class BalonSpawner : MonoBehaviour
{
    // Balon Üretim Ayarları
    public GameObject[] balonPrefablar; 
    
    // Balonların çıkış hızı (daha sık çıkması için düşük değerler)
    public float minSpawnTime = 0.5f; 
    public float maxSpawnTime = 1.5f; 
    
    // Balonların Yatay Yayılım Alanı (Daha geniş bir alana yayılmaları için)
    public float minXSpawn = -7f;   
    public float maxXSpawn = 7f;    
    public float spawnYPosition = -6f; // Balonların ekrana girdiği Y konumu

    // Ses Kontrol Ayarları
    [Header("Ses Kontrolü")]
    public AudioSource arkaPlanMuzigiSource; // Inspector'dan atanacak Müzik Kaynağı
    public AudioClip girisKonusmaSesi;      // Inspector'dan atanacak Konuşma Klibi
    
    public float konusmaOncesiMuzikSesi = 0.2f; // Konuşma sırasında müziğin kısık sesi
    public float normalMuzikSesi = 0.5f;       // Konuşma bittikten sonraki normal sesi
    
    private float sonrakiSpawnZamani;
    private AudioSource konusmaSource; 

    void Start()
    {
        // Konuşma için yeni bir AudioSource bileşeni ekle
        konusmaSource = gameObject.AddComponent<AudioSource>();

        BaslangicSesleriniAyarla();
        
        // İlk balonun üretim zamanını ayarla
        sonrakiSpawnZamani = Time.time + Random.Range(minSpawnTime, maxSpawnTime);
    }
    
    void BaslangicSesleriniAyarla()
    {
        if (arkaPlanMuzigiSource != null)
        {
            // Müziği kısık ses seviyesinde başlat
            arkaPlanMuzigiSource.volume = konusmaOncesiMuzikSesi;
        }

        if (girisKonusmaSesi != null && konusmaSource != null)
        {
            // Konuşma sesini çal
            konusmaSource.PlayOneShot(girisKonusmaSesi);

            // Konuşma bittikten sonra müziği yükseltmek için bekle
            Invoke("MuzikYukselt", girisKonusmaSesi.length); 
        }
        else if (arkaPlanMuzigiSource != null)
        {
            // Eğer konuşma sesi yoksa, müziği hemen normal seviyede başlat
            arkaPlanMuzigiSource.volume = normalMuzikSesi;
        }
    }

    void MuzikYukselt()
    {
        if (arkaPlanMuzigiSource != null)
        {
            // Müziğin sesini normal seviyesine getir
            arkaPlanMuzigiSource.volume = normalMuzikSesi; 
        }
    }

    void Update()
    {
        if (Time.time >= sonrakiSpawnZamani)
        {
            SpawnBalon();
            // Yeni bekleme süresini ayarla
            sonrakiSpawnZamani = Time.time + Random.Range(minSpawnTime, maxSpawnTime); 
        }
    }

    void SpawnBalon()
    {
        if (balonPrefablar.Length == 0) return;

        // Rastgele balon seç
        int rastgeleBalonIndex = Random.Range(0, balonPrefablar.Length);
        GameObject secilenBalon = balonPrefablar[rastgeleBalonIndex];

        // Geniş alanda rastgele X konumu seç (-7f ile 7f arasında)
        float rastgeleX = Random.Range(minXSpawn, maxXSpawn); 
        Vector3 spawnKonumu = new Vector3(rastgeleX, spawnYPosition, 0f); 

        Instantiate(secilenBalon, spawnKonumu, Quaternion.identity);
    }
}