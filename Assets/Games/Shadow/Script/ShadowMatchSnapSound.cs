using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ShadowMatchSnapSound : MonoBehaviour
{
    [Header("Buraya SES DOSYASINI sürükle")]
    public AudioClip snapClip;

    private AudioSource source;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = 1f;
        source.mute = false;
    }

    public void PlaySnap()
    {
        Debug.Log("PlaySnap ÇAĞRILDI");

        if (snapClip != null)
            source.PlayOneShot(snapClip);
        else
            Debug.LogWarning("ShadowMatchSnapSound: snapClip atanmadı!");
    }
}