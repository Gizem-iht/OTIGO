using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ColoringBaseController : MonoBehaviour
{
    [Header("Level Info")]
    public int levelNumber = 1;
    public string nextSceneName;
    public bool isLastLevel = false;
    public string tebrikSceneName = "Coloring_Tebrik";

    [Header("Tasks")]
    public List<ColoringTask> tasks = new List<ColoringTask>();

    [Header("UI")]
    public TextMeshProUGUI taskText;
    public Button replayButton;
    public Button nextButton;
    public Button exitButton;
    public Slider volumeSlider;

    [Header("Brush")]
    public BrushCursor brushCursor;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip introClip;
    public AudioClip selectPaintFirstClip;
    public AudioClip correctClip;
    public AudioClip wrongClip;
    public AudioClip finishClip;

    [Header("Colors")]
    public Color selectedColor = Color.clear;

    [Header("Scene Names")]
    public string mainMenuSceneName = "Coloring_MainMenu";

    [Header("Progress Key")]
    public string lastLevelKey = "Coloring_LastLevel";
    public string savedTaskProgressKeysKey = "Coloring_TaskProgressKeys";

    private static readonly string[] allTaskProgressKeys =
    {
        "Coloring_Level_1_Task_ball_Completed",
        "Coloring_Level_2_Task_pear_Completed",
        "Coloring_Level_2_Task_orange_Completed",
        "Coloring_Level_3_Task_Bot_Completed",
        "Coloring_Level_3_Task_Glass_Completed",
        "Coloring_Level_4_Task_hediye_Completed",
        "Coloring_Level_4_Task_kutu_Completed",
        "Coloring_Level_4_Task_koltuk_Completed",
        "Coloring_Level_5_Task_Flag_Completed",
        "Coloring_Level_5_Task_Jumper_Completed",
        "Coloring_Level_5_Task_Car_Completed",
        "Coloring_Level_5_Task_Flower_Completed",
        "Coloring_Level_6_Task_Headphone_Completed",
        "Coloring_Level_6_Task_Lamp_Completed",
        "Coloring_Level_6_Task_Bag_Completed"
    };

    private float activePlayTime = 0f;
    private int mistakesMade = 0;
    private bool levelFinished = false;
    private bool inputLocked = false;

    protected virtual void Start()
    {
        if (levelNumber == 1 && !PlayerPrefs.HasKey(lastLevelKey))
            ClearAllColoringProgress();

        SaveLastLevel();

        selectedColor = Color.clear;

        if (brushCursor != null)
            brushCursor.ShowBrush(false);

        if (replayButton != null)
        {
            replayButton.gameObject.SetActive(false);
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(ReplayLevel);
        }

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(false);
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(GoNextLevel);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(ExitToMenu);
        }

        if (volumeSlider != null && audioSource != null)
        {
            volumeSlider.value = audioSource.volume;
            volumeSlider.onValueChanged.RemoveAllListeners();
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        LoadTaskProgress();
        ApplyCompletedObjectsVisual();

        if (AllTasksCompleted())
        {
            FinishLevel(false);
        }
        else
        {
            PlayIntroOrShowCurrentTask();
        }
    }

    private void Update()
    {
        bool parentModeActive =
            ParentModeManager.Instance != null &&
            ParentModeManager.Instance.IsParentModeActive;

        if (!levelFinished && !parentModeActive && !IsInstructionAudioPlaying())
            activePlayTime += Time.deltaTime;
    }

    private bool IsInstructionAudioPlaying()
    {
        return audioSource != null && audioSource.isPlaying;
    }

    private void SaveLastLevel()
    {
        PlayerPrefs.SetString(lastLevelKey, SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();
    }

    private string GetTaskProgressKey(string objectId)
    {
        return "Coloring_Level_" + levelNumber + "_Task_" + objectId + "_Completed";
    }

    private void LoadTaskProgress()
    {
        foreach (ColoringTask task in tasks)
        {
            string key = GetTaskProgressKey(task.objectId);
            task.completed = PlayerPrefs.GetInt(key, 0) == 1;

            Debug.Log("LOAD TASK: " + key + " = " + task.completed);
        }
    }

    private void SaveTaskCompleted(ColoringTask task)
    {
        string key = GetTaskProgressKey(task.objectId);
        PlayerPrefs.SetInt(key, 1);
        RememberTaskProgressKey(key);
        PlayerPrefs.Save();

        Debug.Log("KAYDEDİLDİ: " + key);
    }

    private void RememberTaskProgressKey(string key)
    {
        string savedKeys = PlayerPrefs.GetString(savedTaskProgressKeysKey, "");
        string wrappedKeys = "|" + savedKeys + "|";

        if (wrappedKeys.Contains("|" + key + "|"))
            return;

        if (string.IsNullOrEmpty(savedKeys))
            PlayerPrefs.SetString(savedTaskProgressKeysKey, key);
        else
            PlayerPrefs.SetString(savedTaskProgressKeysKey, savedKeys + "|" + key);
    }

    private void ApplyCompletedObjectsVisual()
    {
        Debug.Log("APPLY BAŞLADI");

        ColoringObjectUI[] objects = FindObjectsOfType<ColoringObjectUI>(true);

        foreach (ColoringTask task in tasks)
        {
            Debug.Log("Task: " + task.objectId + " Completed: " + task.completed);

            if (!task.completed) continue;

            foreach (ColoringObjectUI obj in objects)
            {
                Debug.Log("Obj: " + obj.objectId);

                if (obj.objectId == task.objectId)
                {
                    Debug.Log("EŞLEŞTİ -> BOYANIYOR: " + obj.objectId);
                    obj.SetPaintedInstant(task.correctColor);
                }
            }
        }
    }

    private void ClearCurrentLevelProgress()
    {
        foreach (ColoringTask task in tasks)
        {
            string key = GetTaskProgressKey(task.objectId);
            PlayerPrefs.DeleteKey(key);
            task.completed = false;
        }

        PlayerPrefs.Save();
    }

    private void ClearAllColoringProgress()
    {
        string savedKeys = PlayerPrefs.GetString(savedTaskProgressKeysKey, "");
        string[] keys = savedKeys.Split('|');

        foreach (string key in keys)
        {
            if (!string.IsNullOrEmpty(key))
                PlayerPrefs.DeleteKey(key);
        }

        foreach (string key in allTaskProgressKeys)
        {
            PlayerPrefs.DeleteKey(key);
        }

        PlayerPrefs.DeleteKey(savedTaskProgressKeysKey);
        PlayerPrefs.Save();
    }

    public void SelectColor(Color color)
    {
        if (levelFinished || inputLocked)
            return;

        selectedColor = color;

        if (brushCursor != null)
        {
            brushCursor.ShowBrush(true);
            brushCursor.SetBrushColor(color);
        }
    }

    public void OnColoringObjectClickedUI(ColoringObjectUI obj)
    {
        if (obj == null) return;

        if (levelFinished || inputLocked)
            return;

        if (selectedColor == Color.clear)
        {
            PlaySelectPaintFirst();
            obj.Shake();
            return;
        }

        ColoringTask task = GetCurrentTask();

        if (task == null)
            return;

        bool correctObject = obj.objectId == task.objectId;
        bool correctColor = ColorsAreSimilar(selectedColor, task.correctColor);

        if (correctObject && correctColor)
        {
            inputLocked = true;

            obj.Paint(task.correctColor);
            task.completed = true;
            SaveTaskCompleted(task);

            if (audioSource != null && correctClip != null)
                audioSource.PlayOneShot(correctClip);

            StartCoroutine(AfterCorrectPaint());
        }
        else
        {
            mistakesMade++;
            PlayWrong();
            obj.Shake();
        }
    }

    private IEnumerator AfterCorrectPaint()
    {
        yield return new WaitForSeconds(0.8f);

        inputLocked = false;

        if (AllTasksCompleted())
        {
            FinishLevel(true);
        }
        else
        {
            selectedColor = Color.clear;

            if (brushCursor != null)
                brushCursor.ShowBrush(false);

            ShowCurrentTask();
        }
    }

    private void ShowCurrentTask()
    {
        ColoringTask task = GetCurrentTask();

        if (task == null)
            return;

        if (taskText != null)
            taskText.text = task.taskText;

        if (audioSource != null && task.taskVoice != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(task.taskVoice);
        }
    }

    private void PlayIntroOrShowCurrentTask()
    {
        if (audioSource != null && introClip != null)
        {
            StartCoroutine(PlayIntroThenShowCurrentTask());
            return;
        }

        ShowCurrentTask();
    }

    private IEnumerator PlayIntroThenShowCurrentTask()
    {
        inputLocked = true;

        audioSource.Stop();
        audioSource.PlayOneShot(introClip);

        yield return new WaitForSeconds(introClip.length);

        inputLocked = false;
        ShowCurrentTask();
    }

    private ColoringTask GetCurrentTask()
    {
        foreach (ColoringTask task in tasks)
        {
            if (!task.completed)
                return task;
        }

        return null;
    }

    private bool AllTasksCompleted()
    {
        foreach (ColoringTask task in tasks)
        {
            if (!task.completed)
                return false;
        }

        return true;
    }

    private void FinishLevel(bool playSound)
    {
        levelFinished = true;

        int duration = Mathf.RoundToInt(activePlayTime);

        if (ColoringSessionTracker.Instance != null)
        {
            ColoringSessionTracker.Instance.AddOrUpdateLevelResult(
                levelNumber,
                duration,
                mistakesMade
            );
        }

        if (playSound && audioSource != null && finishClip != null)
            audioSource.PlayOneShot(finishClip);

        if (replayButton != null)
            replayButton.gameObject.SetActive(true);

        if (nextButton != null)
            nextButton.gameObject.SetActive(true);
    }

    private void PlayWrong()
    {
        if (audioSource != null && wrongClip != null)
            audioSource.PlayOneShot(wrongClip);
    }

    private void PlaySelectPaintFirst()
    {
        if (audioSource != null && selectPaintFirstClip != null)
        {
            audioSource.PlayOneShot(selectPaintFirstClip);
            return;
        }

        PlayWrong();
    }

    private bool ColorsAreSimilar(Color a, Color b)
    {
        float tolerance = 0.05f;

        return Mathf.Abs(a.r - b.r) < tolerance &&
               Mathf.Abs(a.g - b.g) < tolerance &&
               Mathf.Abs(a.b - b.b) < tolerance;
    }

    private void ReplayLevel()
    {
        ClearCurrentLevelProgress();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void GoNextLevel()
    {
        if (isLastLevel)
        {
            if (ColoringSessionTracker.Instance != null)
                ColoringSessionTracker.Instance.SendFinalResult();

            SceneManager.LoadScene(tebrikSceneName);
        }
        else
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private void ExitToMenu()
    {
        SaveLastLevel();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void SetVolume(float value)
    {
        if (audioSource != null)
            audioSource.volume = value;
    }

    public void SetInputLocked(bool locked)
    {
        inputLocked = locked;
    }
}
