using UnityEngine;
using UnityEngine.UI;

public class ShadowMatchSoundToggle : MonoBehaviour
{
    private const string SoundMutedKey = "SoundMuted";

    public Sprite soundOnSprite;
    public Sprite soundOffSprite;

    private Image buttonImage;
    private bool isMuted = false;

    void Start()
    {
        buttonImage = GetComponent<Image>();

        isMuted = PlayerPrefs.GetInt(SoundMutedKey, 0) == 1;
        AudioListener.volume = isMuted ? 0f : 1f;

        UpdateIcon();
    }

    public void ToggleSound()
    {
        isMuted = !isMuted;
        AudioListener.volume = isMuted ? 0f : 1f;

        PlayerPrefs.SetInt(SoundMutedKey, isMuted ? 1 : 0);
        PlayerPrefs.Save();

        UpdateIcon();
        Debug.Log("Sound " + (isMuted ? "Muted" : "Unmuted"));
    }

    void UpdateIcon()
    {
        if (buttonImage == null) return;

        if (isMuted && soundOffSprite != null)
            buttonImage.sprite = soundOffSprite;
        else if (!isMuted && soundOnSprite != null)
            buttonImage.sprite = soundOnSprite;
    }
}