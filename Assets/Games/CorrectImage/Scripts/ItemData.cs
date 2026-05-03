using UnityEngine;

[System.Serializable]
public class ItemData
{
    public string id;            // "watermelon" vs (opsiyonel)
    public string displayName;   // sadece editör için
    public Sprite sprite;
    public AudioClip voice;      // "karpuzu seç" sesi
}
