using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shadow Match sahne genel ses seviyesi. Tüm leveller aynı PlayerPrefs değerini paylaşır.
/// Eski "mute" butonu bağlıysa <see cref="ToggleSound"/> sessize / son kayıtlı seviyeye döner.
/// </summary>
public class ShadowMatchSoundToggle : MonoBehaviour
{
    internal const string VolumeKey = "ShadowMatch_GameVolume";
    private const string LegacyMuteKey = "SoundMuted";
    private const string LastNonZeroKey = "ShadowMatch_LastNonZeroVol";

    [Tooltip("Tüm Shadow sahnelerinde aynı anahtar kullanılır; değer paylaşımlı kalır.")]
    [SerializeField]
    private Slider volumeSlider;

    /// <summary>
    /// Slider Start çalışmadan önce (Awake sırasında) doğru Listener hacmini uygular.
    /// Shadow level sahnelerinde <see cref="ShadowMatchLevelController"/> Awake ile çağrılır.
    /// </summary>
    public static void SyncAudioListenerFromPlayerPrefs()
    {
        EnsureVolumeMigratedFromLegacyMute();
        ApplyGlobalVolume(Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f)));
    }

    private void Start()
    {
        EnsureVolumeMigratedFromLegacyMute();

        if (volumeSlider == null)
        {
            Debug.LogWarning(
                "[ShadowMatchSoundToggle] Volume Slider atanmadı. Inspector'dan UI Slider'ı bağlayın.",
                this);
            return;
        }

        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
        volumeSlider.wholeNumbers = false;

        float saved = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));

        volumeSlider.onValueChanged.RemoveListener(OnVolumeSliderChanged);
        volumeSlider.value = saved;
        ApplyGlobalVolume(saved);
        volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
    }

    private void OnDestroy()
    {
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(OnVolumeSliderChanged);
    }

    internal static void EnsureVolumeMigratedFromLegacyMute()
    {
        // Eski sadece mute (0/1) kaydı vardı; ilk açılışta float volume'a taşı.
        if (PlayerPrefs.HasKey(VolumeKey))
            return;

        float v = PlayerPrefs.GetInt(LegacyMuteKey, 0) == 1 ? 0f : 1f;
        PlayerPrefs.SetFloat(VolumeKey, v);
        PlayerPrefs.Save();
    }

    private void OnVolumeSliderChanged(float value)
    {
        float clamped = Mathf.Clamp01(value);
        ApplyGlobalVolume(clamped);
        PlayerPrefs.SetFloat(VolumeKey, clamped);
        PlayerPrefs.Save();

        if (clamped > 0.01f)
            PlayerPrefs.SetFloat(LastNonZeroKey, clamped);
    }

    private static void ApplyGlobalVolume(float value)
    {
        AudioListener.volume = Mathf.Clamp01(value);
    }

    /// <summary>
    /// Eski Button OnClick'inde bağlı kalabilir: tam sessize / geri yükleme.
    /// </summary>
    public void ToggleSound()
    {
        float current = Mathf.Clamp01(AudioListener.volume);
        float next;

        if (current > 0.01f)
        {
            PlayerPrefs.SetFloat(LastNonZeroKey, current);
            next = 0f;
        }
        else
        {
            float restore =
                Mathf.Clamp01(PlayerPrefs.GetFloat(LastNonZeroKey, 1f));
            if (restore < 0.05f)
                restore = 1f;
            next = restore;
        }

        ApplyGlobalVolume(next);
        PlayerPrefs.SetFloat(VolumeKey, next);
        PlayerPrefs.Save();

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(OnVolumeSliderChanged);
            volumeSlider.value = next;
            volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
        }
    }
}
