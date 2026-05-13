using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// <see cref="Puzzle_MainMenu"/> gibi PuzzleGameManager olmayan sahnelerde ses tercihi ve slider.
/// <see cref="PuzzleGameManager.VolumePrefsKey"/> ile leveller ortak kullanır.
/// </summary>
public class PuzzleMainMenuVolume : MonoBehaviour
{
    [SerializeField]
    private Slider volumeSlider;

    private void Start()
    {
        PuzzleGameManager.SyncAudioListenerFromPlayerPrefs();

        if (volumeSlider == null)
            return;

        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
        volumeSlider.wholeNumbers = false;

        float saved =
            Mathf.Clamp01(
                PlayerPrefs.GetFloat(PuzzleGameManager.VolumePrefsKey, PuzzleGameManager.DefaultVolumeLevel));

        volumeSlider.onValueChanged.RemoveListener(OnSliderChanged);
        volumeSlider.SetValueWithoutNotify(saved);
        PuzzleGameManager.SyncAudioListenerFromPlayerPrefs();
        volumeSlider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnDestroy()
    {
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(OnSliderChanged);
    }

    private void OnSliderChanged(float value)
    {
        PuzzleGameManager.ApplyAndPersistGlobalVolume(value);
    }
}
