using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class OppositeFaceConfig
{
    public string conceptId;
    public Sprite frontSprite;
    public AudioClip cardVoiceClip;
}

[Serializable]
public class OppositePairConfig
{
    public OppositeFaceConfig sideOne;
    public OppositeFaceConfig sideTwo;
}

public class OppositesMemoryLevelManager : MonoBehaviour
{
    public static OppositesMemoryLevelManager Instance;

    private const string VolumeKey = "OppositesVolume";
    private const string LAST_LEVEL_KEY = "Opposites_LastLevelSceneName";

    [Header("Level Info")]
    public int activityId = 10;
    public int levelNumber = 1;
    public string nextSceneName = "";
    public bool isLastLevel = false;

    [Header("Cards")]
    public List<OppositesCard> cards = new List<OppositesCard>();

    [Header("Random opposite deal")]
    public bool randomizeOppositePairsFromPool = true;
    [Tooltip("Boşsa oyun kartlardan otomatik doldurur. OTIGO → Opposites menüsünden Inspector’a yazabilirsin.")]
    public List<OppositePairConfig> oppositePairPool = new List<OppositePairConfig>();

    [Tooltip("Buna daha fazla zıt çift ekle: havuz slot sayısından BÜYÜK olursa her restart’ta farklı çiftler seçilir. Sadece board’daki çift sayısı kadar havuz varsa yalnızca yer değişir (aynı görseller).")]
    public List<OppositePairConfig> extraOppositePairPool = new List<OppositePairConfig>();

    [Header("Preview")]
    public float previewSeconds = 5f;

    [Header("Buttons")]
    public Button replayButton;
    public Button nextButton;
    public Button menuButton;

    [Header("UI")]
    public Slider volumeSlider;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip introClip;
    public AudioClip startClip;
    public AudioClip correctClip;
    public AudioClip wrongClip;
    public AudioClip completeClip;
    public AudioClip flipClip;

    private OppositesCard firstSelected;
    private OppositesCard secondSelected;

    private int mistakesMade = 0;
    private int matchedPairCount = 0;
    private int totalPairs = 0;

    private bool isBusy = false;
    private bool levelCompleted = false;
    private bool resultSaved = false;
    private bool gameplayStarted = false;

    private float activePlayTime = 0f;

    private List<List<OppositesCard>> cachedPairSlots;

    private string StatePrefix
    {
        get { return "Opposites_State_" + SceneManager.GetActiveScene().name + "_"; }
    }

    private void Awake()
    {
        Instance = this;
        CachePairSlotsFromInspector();
        EnsureOppositePairPoolFromInspectorsIfNeeded();
    }

    private void Start()
    {
        SaveCurrentLevel();

        if (levelNumber == 1 && !HasSavedState())
            OppositesOtigoSessionTracker.BeginSession(activityId);
        else if (!OppositesOtigoSessionTracker.HasSession())
            OppositesOtigoSessionTracker.BeginSession(activityId);

        mistakesMade = 0;
        matchedPairCount = 0;
        totalPairs = cards.Count / 2;

        isBusy = false;
        levelCompleted = false;
        resultSaved = false;
        gameplayStarted = false;
        activePlayTime = 0f;

        firstSelected = null;
        secondSelected = null;

        SetupButtons();
        SetupSlider();

        StartCoroutine(DelayedStartRestore());
    }

    private IEnumerator DelayedStartRestore()
    {
        yield return null;

        bool reopenedAfterRestartTap = TryConsumeFreshRestartDeal();

        if (HasSavedState() && !reopenedAfterRestartTap)
        {
            if (ShouldApplySavedDeal())
            {
                int seed = PlayerPrefs.GetInt(StatePrefix + "DealSeed");
                ApplyRandomDealFromPool(seed);
            }

            LoadState();
            RestoreMatchedCards();

            if (levelCompleted)
                RestoreCompletedLevel();
            else
                gameplayStarted = true;
        }
        else
        {
            if (CanApplyRandomDealFromPool())
            {
                int seed = MakeNewDealSeed();
                PlayerPrefs.SetInt(StatePrefix + "DealSeed", seed);
                PlayerPrefs.Save();
                ApplyRandomDealFromPool(seed);
            }

            StartCoroutine(IntroThenPreviewRoutine());
        }
    }

    private IEnumerator IntroThenPreviewRoutine()
    {
        if (sfxSource != null && introClip != null)
        {
            gameplayStarted = false;
            isBusy = true;

            sfxSource.PlayOneShot(introClip);
            yield return new WaitForSeconds(introClip.length);
        }

        yield return StartCoroutine(PreviewThenStartRoutine());
    }

    private IEnumerator PreviewThenStartRoutine()
    {
        gameplayStarted = false;
        isBusy = true;

        foreach (OppositesCard card in cards)
        {
            if (card == null) continue;
            if (card.IsMatched) continue;

            card.OpenCard();
        }

        yield return new WaitForSeconds(previewSeconds);

        foreach (OppositesCard card in cards)
        {
            if (card == null) continue;
            if (card.IsMatched) continue;

            card.CloseCard();
        }

        isBusy = false;

        StartCoroutine(StartVoiceRoutine());
    }

    private void Update()
    {
        bool parentModeActive =
            ParentModeManager.Instance != null &&
            ParentModeManager.Instance.IsParentModeActive;

        if (gameplayStarted && !levelCompleted && !parentModeActive && !IsInstructionAudioPlaying())
            activePlayTime += Time.deltaTime;
    }

    private bool IsInstructionAudioPlaying()
    {
        return sfxSource != null && sfxSource.isPlaying;
    }

    private IEnumerator StartVoiceRoutine()
    {
        gameplayStarted = false;

        if (sfxSource != null && startClip != null)
        {
            sfxSource.PlayOneShot(startClip);
            yield return new WaitForSeconds(startClip.length);
        }

        gameplayStarted = true;
        SaveState();
    }

    private void SetupButtons()
    {
        if (replayButton != null)
        {
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(RestartLevel);
            replayButton.gameObject.SetActive(false);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(GoNext);
            nextButton.gameObject.SetActive(false);
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(GoMenu);
        }
    }

    private void SetupSlider()
    {
        if (volumeSlider == null) return;

        float savedVolume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        volumeSlider.value = savedVolume;
        ApplyVolume(savedVolume);

        volumeSlider.onValueChanged.RemoveAllListeners();
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    private void OnVolumeChanged(float value)
    {
        ApplyVolume(value);
        PlayerPrefs.SetFloat(VolumeKey, value);
        PlayerPrefs.Save();
    }

    private void ApplyVolume(float value)
    {
        AudioListener.volume = value;

        if (sfxSource != null)
            sfxSource.volume = value;
    }

    public void OnCardSelected(OppositesCard card)
    {
        if (!gameplayStarted) return;
        if (isBusy) return;
        if (levelCompleted) return;
        if (card == null) return;
        if (card == firstSelected) return;
        if (card.IsMatched) return;
        if (card.IsOpen) return;

        card.OpenCard();

        if (sfxSource != null && flipClip != null)
            sfxSource.PlayOneShot(flipClip);

        if (sfxSource != null && card.cardVoiceClip != null)
            sfxSource.PlayOneShot(card.cardVoiceClip);

        if (firstSelected == null)
        {
            firstSelected = card;
            return;
        }

        secondSelected = card;
        StartCoroutine(CheckMatchRoutine());
    }

    private IEnumerator CheckMatchRoutine()
    {
        isBusy = true;

        yield return new WaitForSeconds(0.8f);

        bool isCorrectMatch =
            firstSelected != null &&
            secondSelected != null &&
            firstSelected.pairId == secondSelected.pairId &&
            firstSelected.conceptId != secondSelected.conceptId;

        if (isCorrectMatch)
        {
            firstSelected.SetMatched();
            secondSelected.SetMatched();

            matchedPairCount++;

            SaveMatchedPair(firstSelected, secondSelected);

            if (sfxSource != null && correctClip != null)
                sfxSource.PlayOneShot(correctClip);

            if (ParentModeManager.Instance != null &&
                ParentModeManager.Instance.IsParentModeActive)
            {
                OppositesOtigoSessionTracker.AddParentHelpForLevel(levelNumber);
            }

            SaveState();

            if (matchedPairCount >= totalPairs)
            {
                levelCompleted = true;
                gameplayStarted = false;
                SaveState();
                yield return StartCoroutine(CompleteRoutine());
            }
        }
        else
        {
            mistakesMade++;

            if (sfxSource != null && wrongClip != null)
                sfxSource.PlayOneShot(wrongClip);

            yield return new WaitForSeconds(0.5f);

            if (firstSelected != null) firstSelected.CloseCard();
            if (secondSelected != null) secondSelected.CloseCard();

            SaveState();
        }

        firstSelected = null;
        secondSelected = null;
        isBusy = false;
    }

    private IEnumerator CompleteRoutine()
    {
        SaveLevelResult();

        yield return new WaitForSeconds(0.5f);

        if (sfxSource != null && completeClip != null)
            sfxSource.PlayOneShot(completeClip);

        if (replayButton != null)
            replayButton.gameObject.SetActive(true);

        if (nextButton != null)
            nextButton.gameObject.SetActive(true);

        SaveState();

        if (isLastLevel)
            OppositesOtigoSessionTracker.SendFinalResultIfPossible();
    }

    private void SaveLevelResult()
    {
        if (resultSaved) return;

        resultSaved = true;

        int durationSeconds = Mathf.RoundToInt(activePlayTime);

        OppositesOtigoSessionTracker.AddOrUpdateLevelResult(
            levelNumber,
            durationSeconds,
            mistakesMade
        );

        SaveState();
    }

    private void RestoreCompletedLevel()
    {
        gameplayStarted = false;

        if (replayButton != null)
            replayButton.gameObject.SetActive(true);

        if (nextButton != null)
            nextButton.gameObject.SetActive(true);
    }

    public void RestartLevel()
    {
        ClearState();
        PlayerPrefs.SetInt(StatePrefix + "FreshRestart", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoNext()
    {
        ClearState();

        if (isLastLevel)
        {
            OtigoGameProgress.ClearGame("Opposites");
            SceneManager.LoadScene("Opposites_TebrikScene");
        }
        else if (!string.IsNullOrEmpty(nextSceneName))
        {
            OtigoGameProgress.SaveLevel(LAST_LEVEL_KEY, nextSceneName);
            SceneManager.LoadScene(nextSceneName);
        }
    }

    public void GoMenu()
    {
        SaveState();
        SaveCurrentLevel();
        SceneManager.LoadScene("Opposites_MainMenu");
    }

    private void SaveCurrentLevel()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetString(LAST_LEVEL_KEY, currentSceneName);
        PlayerPrefs.Save();
    }

    private void SaveMatchedPair(OppositesCard a, OppositesCard b)
    {
        if (a != null && !string.IsNullOrEmpty(a.cardId))
            PlayerPrefs.SetInt(StatePrefix + "Matched_" + a.cardId, 1);

        if (b != null && !string.IsNullOrEmpty(b.cardId))
            PlayerPrefs.SetInt(StatePrefix + "Matched_" + b.cardId, 1);

        PlayerPrefs.Save();
    }

    private void RestoreMatchedCards()
    {
        HashSet<string> donePairs = new HashSet<string>();

        foreach (OppositesCard card in cards)
        {
            if (card == null) continue;
            if (string.IsNullOrEmpty(card.cardId)) continue;

            bool matched = PlayerPrefs.GetInt(StatePrefix + "Matched_" + card.cardId, 0) == 1;

            if (matched)
            {
                card.SetMatched();

                if (!string.IsNullOrEmpty(card.pairId))
                    donePairs.Add(card.pairId);
            }
        }

        matchedPairCount = donePairs.Count;
    }

    private void SaveState()
    {
        PlayerPrefs.SetInt(StatePrefix + "HasState", 1);
        PlayerPrefs.SetInt(StatePrefix + "MistakesMade", mistakesMade);
        PlayerPrefs.SetInt(StatePrefix + "MatchedPairCount", matchedPairCount);
        PlayerPrefs.SetInt(StatePrefix + "LevelCompleted", levelCompleted ? 1 : 0);
        PlayerPrefs.SetInt(StatePrefix + "ResultSaved", resultSaved ? 1 : 0);
        PlayerPrefs.SetFloat(StatePrefix + "ActivePlayTime", activePlayTime);
        PlayerPrefs.Save();
    }

    private void LoadState()
    {
        mistakesMade = PlayerPrefs.GetInt(StatePrefix + "MistakesMade", 0);
        matchedPairCount = PlayerPrefs.GetInt(StatePrefix + "MatchedPairCount", 0);
        levelCompleted = PlayerPrefs.GetInt(StatePrefix + "LevelCompleted", 0) == 1;
        resultSaved = PlayerPrefs.GetInt(StatePrefix + "ResultSaved", 0) == 1;
        activePlayTime = PlayerPrefs.GetFloat(StatePrefix + "ActivePlayTime", 0f);
    }

    private bool HasSavedState()
    {
        return PlayerPrefs.GetInt(StatePrefix + "HasState", 0) == 1;
    }

    /// <returns>Replay/Re-temiz kart dağılım bayrağı tüketildiyse true (yeniden sahne yüklendi).</returns>
    private bool TryConsumeFreshRestartDeal()
    {
        if (PlayerPrefs.GetInt(StatePrefix + "FreshRestart", 0) != 1)
            return false;

        PlayerPrefs.DeleteKey(StatePrefix + "FreshRestart");
        ClearState();
        PlayerPrefs.Save();
        return true;
    }

    private static int MakeNewDealSeed()
    {
        unchecked
        {
            return Guid.NewGuid().GetHashCode() ^
                   Environment.TickCount ^
                   UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        }
    }

    private void ClearState()
    {
        PlayerPrefs.DeleteKey(StatePrefix + "HasState");
        PlayerPrefs.DeleteKey(StatePrefix + "MistakesMade");
        PlayerPrefs.DeleteKey(StatePrefix + "MatchedPairCount");
        PlayerPrefs.DeleteKey(StatePrefix + "LevelCompleted");
        PlayerPrefs.DeleteKey(StatePrefix + "ResultSaved");
        PlayerPrefs.DeleteKey(StatePrefix + "ActivePlayTime");
        PlayerPrefs.DeleteKey(StatePrefix + "DealSeed");

        foreach (OppositesCard card in cards)
        {
            if (card == null) continue;
            if (string.IsNullOrEmpty(card.cardId)) continue;

            PlayerPrefs.DeleteKey(StatePrefix + "Matched_" + card.cardId);
        }

        PlayerPrefs.Save();
    }

    private void CachePairSlotsFromInspector()
    {
        cachedPairSlots = new List<List<OppositesCard>>();
        var pairIdFirstIndex = new Dictionary<string, int>();

        foreach (OppositesCard card in cards)
        {
            if (card == null) continue;
            string pid = card.pairId;
            if (string.IsNullOrEmpty(pid))
            {
                Debug.LogError("[Opposites] Card without pairId in list: " + card.name, card);
                continue;
            }

            if (!pairIdFirstIndex.ContainsKey(pid))
            {
                pairIdFirstIndex[pid] = cachedPairSlots.Count;
                cachedPairSlots.Add(new List<OppositesCard>());
            }

            int slot = pairIdFirstIndex[pid];
            cachedPairSlots[slot].Add(card);
        }

        for (int i = 0; i < cachedPairSlots.Count; i++)
        {
            if (cachedPairSlots[i].Count != 2)
            {
                Debug.LogError(
                    "[Opposites] Pair slot " + i + " must have exactly 2 cards (pairId group). Count=" +
                    cachedPairSlots[i].Count);
            }
        }
    }

    private List<OppositePairConfig> GetEffectiveOppositePool()
    {
        var list = new List<OppositePairConfig>();
        if (oppositePairPool != null && oppositePairPool.Count > 0)
            list.AddRange(oppositePairPool);
        if (extraOppositePairPool != null && extraOppositePairPool.Count > 0)
            list.AddRange(extraOppositePairPool);
        return list;
    }

    private bool CanApplyRandomDealFromPool()
    {
        if (!randomizeOppositePairsFromPool)
            return false;
        int slots = PairSlotCountSafe();
        List<OppositePairConfig> pool = GetEffectiveOppositePool();
        return slots > 0 && pool.Count >= slots;
    }

    private bool ShouldApplySavedDeal()
    {
        return CanApplyRandomDealFromPool() && PlayerPrefs.HasKey(StatePrefix + "DealSeed");
    }

    private int PairSlotCountSafe()
    {
        return cachedPairSlots != null ? cachedPairSlots.Count : 0;
    }

    private void ApplyRandomDealFromPool(int seed)
    {
        int n = PairSlotCountSafe();
        List<OppositePairConfig> deck = GetEffectiveOppositePool();
        if (deck == null || deck.Count < n || n <= 0)
            return;

        System.Random rng = new System.Random(seed);

        List<int> poolPick = new List<int>(deck.Count);
        for (int i = 0; i < deck.Count; i++)
            poolPick.Add(i);

        ShuffleList(poolPick, rng);

        List<int> slotOrder = new List<int>(n);
        for (int i = 0; i < n; i++)
            slotOrder.Add(i);

        ShuffleList(slotOrder, rng);

        for (int i = 0; i < n; i++)
        {
            int poolIdx = poolPick[i];
            int slotIdx = slotOrder[i];
            OppositePairConfig cfg = deck[poolIdx];
            if (cfg.sideOne == null || cfg.sideTwo == null)
            {
                Debug.LogError("[Opposites] Pool entry " + poolIdx + " missing sideOne/sideTwo.");
                continue;
            }

            OppositeFaceConfig a = cfg.sideOne;
            OppositeFaceConfig b = cfg.sideTwo;
            if (rng.Next(2) == 1)
            {
                OppositeFaceConfig t = a;
                a = b;
                b = t;
            }

            List<OppositesCard> slot = cachedPairSlots[slotIdx];
            if (slot == null || slot.Count != 2)
                continue;

            OppositesCard c0 = slot[0];
            OppositesCard c1 = slot[1];
            if (rng.Next(2) == 1)
            {
                OppositesCard t = c0;
                c0 = c1;
                c1 = t;
            }

            string runtimePairId = "deal_" + seed + "_" + slotIdx;
            c0.AssignRuntimeOpposite(runtimePairId, a.conceptId, a.frontSprite, a.cardVoiceClip);
            c1.AssignRuntimeOpposite(runtimePairId, b.conceptId, b.frontSprite, b.cardVoiceClip);
        }
    }

    private static void ShuffleList<T>(IList<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            T tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }

    private void EnsureOppositePairPoolFromInspectorsIfNeeded()
    {
        if (!randomizeOppositePairsFromPool)
            return;

        if (oppositePairPool != null && oppositePairPool.Count > 0)
            return;

        oppositePairPool = BuildPairPoolConfigsFromCachedSlots();
    }

    /// <summary>
    /// Kart listesinden çift havuzunu üretir (Editor menüsünde ve araç için).
    /// </summary>
    public void RebuildOppositePairPoolFromInspectors()
    {
        CachePairSlotsFromInspector();
        oppositePairPool = BuildPairPoolConfigsFromCachedSlots();
    }

    private List<OppositePairConfig> BuildPairPoolConfigsFromCachedSlots()
    {
        var pool = new List<OppositePairConfig>();
        if (cachedPairSlots == null)
            return pool;

        foreach (List<OppositesCard> slot in cachedPairSlots)
        {
            if (slot == null || slot.Count != 2)
                continue;

            pool.Add(new OppositePairConfig
            {
                sideOne = FaceConfigFromCard(slot[0]),
                sideTwo = FaceConfigFromCard(slot[1])
            });
        }

        return pool;
    }

    private static OppositeFaceConfig FaceConfigFromCard(OppositesCard c)
    {
        if (c == null)
            return new OppositeFaceConfig();

        return new OppositeFaceConfig
        {
            conceptId = c.conceptId,
            frontSprite = c.frontSprite,
            cardVoiceClip = c.cardVoiceClip
        };
    }
}
