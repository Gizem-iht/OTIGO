using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class ShadowMatchAllSnappedUnlockAuto : MonoBehaviour
{
    [Header("UI")]
    public Button nextLevelButton;
    public bool hideNextCompletely = false;
    public TextMeshProUGUI winTextTMP;

    [Header("Next Scene")]
    public string nextSceneName;

    [Header("Restart")]
    public Button restartLevelButton;
    public bool hideRestartCompletely = true;
    public bool requireCompletedToRestart = true;

    [Header("Audio (optional)")]
    public AudioSource audioSource;
    public AudioClip clapClip;

    [Header("FX (optional)")]
    public ParticleSystem balloonConfettiFX;

    [Header("Pieces")]
    public bool autoFindOnStart = true;
    public ShadowMatchDragSnap[] pieces;

    private bool done;
    private ShadowMatchLevelController levelController;
    private Image restartBtnImage;
    private Image nextBtnImage;

    void Awake()
    {
        levelController = FindObjectOfType<ShadowMatchLevelController>();

        if (restartLevelButton) restartLevelButton.onClick.AddListener(RestartLevel);
        if (nextLevelButton) nextLevelButton.onClick.AddListener(LoadNextLevel);

        if (restartLevelButton) restartBtnImage = restartLevelButton.GetComponent<Image>();
        if (nextLevelButton) nextBtnImage = nextLevelButton.GetComponent<Image>();
    }

    void Start()
    {
        if (autoFindOnStart)
        {
#if UNITY_2020_1_OR_NEWER
            pieces = FindObjectsOfType<ShadowMatchDragSnap>(includeInactive: false);
#else
            pieces = FindObjectsOfType<ShadowMatchDragSnap>();
#endif
        }

        if (nextLevelButton)
        {
            if (hideNextCompletely) nextLevelButton.gameObject.SetActive(false);
            else
            {
                nextLevelButton.interactable = false;
                if (nextBtnImage) nextBtnImage.raycastTarget = false;
            }
        }

        if (restartLevelButton)
        {
            if (hideRestartCompletely) restartLevelButton.gameObject.SetActive(false);
            else
            {
                restartLevelButton.interactable = false;
                if (restartBtnImage) restartBtnImage.raycastTarget = false;
            }
        }

        if (winTextTMP)
        {
            winTextTMP.text = "";
            winTextTMP.gameObject.SetActive(false);
        }

        if (balloonConfettiFX) balloonConfettiFX.Stop();

        done = false;
    }

    void Update()
    {
        if (done) return;
        if (pieces == null || pieces.Length == 0) return;
        if (levelController == null) return;

        int total = pieces.Length;
        bool allCorrect = (levelController.correctMatches == total);
        if (!allCorrect) return;

        done = true;

        if (balloonConfettiFX)
        {
            if (!balloonConfettiFX.gameObject.activeInHierarchy)
                balloonConfettiFX.gameObject.SetActive(true);

            balloonConfettiFX.Play(true);
        }

        if (audioSource && clapClip)
            audioSource.PlayOneShot(clapClip);

        if (winTextTMP)
        {
            winTextTMP.gameObject.SetActive(true);
            winTextTMP.alpha = 1f;
            winTextTMP.text = "<b>TEBRİKLER!</b>";
        }

        ShowNext(true);
        ShowRestart(true);
    }

    private void ShowRestart(bool on)
    {
        if (!restartLevelButton) return;

        if (hideRestartCompletely)
            restartLevelButton.gameObject.SetActive(on);
        else
        {
            restartLevelButton.interactable = on;
            if (restartBtnImage) restartBtnImage.raycastTarget = on;
        }

        if (on) restartLevelButton.transform.SetAsLastSibling();
    }

    private void ShowNext(bool on)
    {
        if (!nextLevelButton) return;

        if (hideNextCompletely)
            nextLevelButton.gameObject.SetActive(on);
        else
        {
            nextLevelButton.interactable = on;
            if (nextBtnImage) nextBtnImage.raycastTarget = on;
        }

        if (on) nextLevelButton.transform.SetAsLastSibling();
    }

    public void RestartLevel()
    {
        if (requireCompletedToRestart && !done)
        {
            Debug.Log("[ShadowMatchAllSnappedUnlockAuto] Restart reddedildi: henüz tamamlanmadı.");
            return;
        }

        if (levelController) levelController.RegisterRetry();

        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    public void LoadNextLevel()
    {
        if (!done)
        {
            Debug.Log("[ShadowMatchAllSnappedUnlockAuto] Next reddedildi: henüz tamamlanmadı.");
            return;
        }

        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning("[ShadowMatchAllSnappedUnlockAuto] nextSceneName boş bırakılmış.");
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }
}