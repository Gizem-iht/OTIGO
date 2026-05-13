using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ödev tamamlandığında oyun üstünde panel (çocuk modu).
/// Yıldız: önce Resources/<see cref="StarIconResourcePath"/>; yoksa kodla çizilen altın beş köşeli yıldız + gölge + hale.
/// Ses: <see cref="CelebrationClipResourcePath"/>.
/// </summary>
public class OtigoHomeworkCompletionPanel : MonoBehaviour
{
    public const string CelebrationClipResourcePath = "OtigoHomework/Celebration";
    public const string StarIconResourcePath = "OtigoHomework/StarIcon";

    private const string ChildHomeworkInstruction =
        "Aferin! Sana verilen ödevleri tamamladın.\nDevam etmek için gülücüğe bas.";

    private const string SmileyButtonChar = "\U0001F60A";

    private static OtigoHomeworkCompletionPanel _instance;

    private RectTransform _panelRect;
    private RectTransform _starBadgeRow;
    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _subtitleText;
    private TextMeshProUGUI _instructionText;
    private Button _dismissButton;
    private Button _dimButton;
    private TextMeshProUGUI _dismissLabel;
    private AudioSource _audioSource;

    private static Sprite _cachedStarFromResources;
    private static Sprite _cachedProceduralGoldStar;
    private static Sprite _cachedSoftGlowCircle;
    private static Sprite _cachedGradientPanelBg;

    private static Sprite _whiteSprite;

    public static void ShowForChild(int assignmentLevelTarget)
    {
        EnsureBuilt();
        _instance.ApplyChildCelebration(assignmentLevelTarget);
    }

    public static void Show(string title, string subtitle)
    {
        EnsureBuilt();
        _instance.ApplyAdultStyle(title, subtitle);
    }

    private static void EnsureBuilt()
    {
        if (_instance != null)
            return;

        var go = new GameObject("OtigoHomeworkCompletionPanel");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<OtigoHomeworkCompletionPanel>();
        _instance.BuildUi();
    }

    private void BuildUi()
    {
        gameObject.AddComponent<Canvas>();
        var cv = GetComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 32700;
        cv.overrideSorting = true;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f;

        var rootRt = gameObject.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        var dimGo = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
        dimGo.transform.SetParent(transform, false);
        var dimRt = dimGo.GetComponent<RectTransform>();
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero;
        dimRt.offsetMax = Vector2.zero;
        var dimImg = dimGo.GetComponent<Image>();
        dimImg.color = new Color(0.04f, 0.06f, 0.14f, 0.62f);
        dimImg.raycastTarget = true;
        _dimButton = dimGo.GetComponent<Button>();
        _dimButton.transition = Selectable.Transition.None;
        _dimButton.onClick.AddListener(Hide);

        // Kart gölgesi (panelin altında)
        var shadowGo = new GameObject("CardDropShadow", typeof(RectTransform), typeof(Image));
        shadowGo.transform.SetParent(transform, false);
        var shRt = shadowGo.GetComponent<RectTransform>();
        shRt.anchorMin = shRt.anchorMax = new Vector2(0.5f, 0.5f);
        shRt.pivot = new Vector2(0.5f, 0.5f);
        shRt.sizeDelta = new Vector2(880f, 640f);
        shRt.anchoredPosition = new Vector2(8f, -10f);
        var shImg = shadowGo.GetComponent<Image>();
        shImg.sprite = WhiteSprite();
        shImg.color = new Color(0f, 0f, 0f, 0.28f);
        shImg.raycastTarget = false;

        var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(transform, false);
        _panelRect = panelGo.GetComponent<RectTransform>();
        _panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        _panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        _panelRect.pivot = new Vector2(0.5f, 0.5f);
        _panelRect.sizeDelta = new Vector2(840f, 620f);
        var panelImage = panelGo.GetComponent<Image>();
        panelImage.sprite = GetOrCreatePanelGradientSprite();
        panelImage.type = Image.Type.Simple;
        panelImage.color = Color.white;
        panelImage.raycastTarget = true;
        var panelSh = panelGo.AddComponent<Shadow>();
        panelSh.effectColor = new Color(0.2f, 0.12f, 0.35f, 0.55f);
        panelSh.effectDistance = new Vector2(10f, -8f);
        panelSh.useGraphicAlpha = true;

        var vlg = panelGo.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(36, 36, 44, 36);
        vlg.spacing = 12f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        TMP_FontAsset font = ResolveTmpFont();

        var starRowGo = new GameObject("StarBadgeRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        starRowGo.transform.SetParent(panelGo.transform, false);
        _starBadgeRow = starRowGo.GetComponent<RectTransform>();
        var starH = starRowGo.GetComponent<HorizontalLayoutGroup>();
        starH.childAlignment = TextAnchor.MiddleCenter;
        starH.childControlWidth = false;
        starH.childControlHeight = true;
        starH.childForceExpandWidth = false;
        starH.childForceExpandHeight = false;
        starH.spacing = 10f;
        starH.padding = new RectOffset(4, 4, 8, 8);
        var starRowLe = starRowGo.AddComponent<LayoutElement>();
        starRowLe.minHeight = 200f;
        starRowLe.preferredHeight = 200f;

        _titleText = CreateTmpText(panelGo.transform, "Title", 40f, FontStyles.Bold, new Color(0.15f, 0.18f, 0.22f), font);
        _titleText.alignment = TextAlignmentOptions.Center;
        var titleLe = _titleText.gameObject.AddComponent<LayoutElement>();
        titleLe.minHeight = 48f;
        titleLe.preferredHeight = 48f;

        _subtitleText = CreateTmpText(panelGo.transform, "Cheer", 72f, FontStyles.Bold, new Color(0.98f, 0.55f, 0.18f), font);
        _subtitleText.alignment = TextAlignmentOptions.Center;
        _subtitleText.enableWordWrapping = true;
        _subtitleText.characterSpacing = 2f;
        _subtitleText.fontStyle = FontStyles.Bold;
        ApplyCheerTextStyle(_subtitleText, childMode: false);
        var subLe = _subtitleText.gameObject.AddComponent<LayoutElement>();
        subLe.minHeight = 96f;
        subLe.preferredHeight = 96f;

        _instructionText = CreateTmpText(panelGo.transform, "Instruction", 30f, FontStyles.Normal, new Color(0.28f, 0.3f, 0.36f), font);
        _instructionText.alignment = TextAlignmentOptions.Center;
        _instructionText.enableWordWrapping = true;
        _instructionText.lineSpacing = 4f;
        _instructionText.margin = new Vector4(12f, 0f, 12f, 8f);
        var insLe = _instructionText.gameObject.AddComponent<LayoutElement>();
        insLe.minHeight = 100f;
        insLe.preferredHeight = 100f;
        insLe.flexibleHeight = 0f;
        _instructionText.gameObject.SetActive(false);

        var btnRow = new GameObject("ButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        btnRow.transform.SetParent(panelGo.transform, false);
        var h = btnRow.GetComponent<HorizontalLayoutGroup>();
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = false;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;
        h.spacing = 12f;
        var rowLe = btnRow.AddComponent<LayoutElement>();
        rowLe.minHeight = 140f;
        rowLe.preferredHeight = 140f;

        _dismissButton = CreatePrimaryButton(btnRow.transform, SmileyButtonChar, 72f, font);
        _dismissButton.onClick.AddListener(Hide);
        _dismissLabel = _dismissButton.GetComponentInChildren<TextMeshProUGUI>();

        gameObject.SetActive(false);
    }

    private static void ApplyCheerTextStyle(TextMeshProUGUI tmp, bool childMode)
    {
        if (tmp == null)
            return;

        if (childMode)
        {
            tmp.fontSize = 86f;
            tmp.color = new Color(1f, 0.72f, 0.12f);
            tmp.outlineWidth = 0.22f;
            tmp.outlineColor = new Color(0.55f, 0.22f, 0.05f, 1f);
        }
        else
        {
            tmp.fontSize = 26f;
            tmp.color = new Color(0.32f, 0.35f, 0.4f);
            tmp.outlineWidth = 0f;
        }
    }

    private void ApplyChildCelebration(int assignmentLevelTarget)
    {
        int n = Mathf.Clamp(assignmentLevelTarget, 1, 8);

        _starBadgeRow.gameObject.SetActive(true);
        PopulateStarBadges(n);

        _titleText.gameObject.SetActive(false);

        _subtitleText.text = "Aferin!";
        ApplyCheerTextStyle(_subtitleText, childMode: true);

        if (_instructionText != null)
        {
            _instructionText.gameObject.SetActive(true);
            _instructionText.text = ChildHomeworkInstruction;
        }

        if (_dismissLabel != null)
        {
            _dismissLabel.text = SmileyButtonChar;
            _dismissLabel.fontSize = 72f;
        }

        gameObject.SetActive(true);
        TryPlayCelebrationClip();
        StopAllCoroutines();
        StartCoroutine(PulsePanel());
        StartCoroutine(StaggerStarReveal());
    }

    private void ApplyAdultStyle(string title, string subtitle)
    {
        ClearStarBadges();
        _starBadgeRow.gameObject.SetActive(false);

        _titleText.gameObject.SetActive(true);
        _titleText.fontSize = 40f;
        _titleText.color = new Color(0.15f, 0.18f, 0.22f);
        _titleText.text = title ?? "";

        _subtitleText.text = subtitle ?? "";
        ApplyCheerTextStyle(_subtitleText, childMode: false);

        if (_instructionText != null)
        {
            _instructionText.gameObject.SetActive(false);
            _instructionText.text = "";
        }

        if (_dismissLabel != null)
        {
            _dismissLabel.text = "Tamam";
            _dismissLabel.fontSize = 30f;
        }

        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(PulsePanel());
    }

    private IEnumerator StaggerStarReveal()
    {
        yield return null;

        int count = _starBadgeRow.childCount;
        var list = new List<RectTransform>(count);
        for (int i = 0; i < count; i++)
        {
            var rt = _starBadgeRow.GetChild(i) as RectTransform;
            if (rt != null)
            {
                list.Add(rt);
                rt.localScale = Vector3.zero;
            }
        }

        for (int i = 0; i < list.Count; i++)
        {
            RectTransform rt = list[i];
            float up = 0.22f;
            float t = 0f;
            while (t < up)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / up);
                rt.localScale = Vector3.one * Mathf.Lerp(0f, 1.18f, k);
                yield return null;
            }

            t = 0f;
            float settle = 0.14f;
            while (t < settle)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / settle);
                rt.localScale = Vector3.one * Mathf.Lerp(1.18f, 1f, k);
                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.05f);
        }
    }

    private void ClearStarBadges()
    {
        if (_starBadgeRow == null)
            return;

        for (int i = _starBadgeRow.childCount - 1; i >= 0; i--)
            Destroy(_starBadgeRow.GetChild(i).gameObject);
    }

    private void PopulateStarBadges(int count)
    {
        ClearStarBadges();

        Sprite starSprite = GetStarSpriteForBadges();
        Sprite glow = GetOrCreateSoftGlowSprite();

        for (int i = 0; i < count; i++)
        {
            var slot = new GameObject("StarSlot_" + i, typeof(RectTransform));
            slot.transform.SetParent(_starBadgeRow, false);
            var slotRt = slot.GetComponent<RectTransform>();
            slotRt.sizeDelta = new Vector2(148f, 168f);

            var le = slot.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = 148f;
            le.minHeight = le.preferredHeight = 168f;

            var glowGo = new GameObject("Glow", typeof(RectTransform), typeof(Image));
            glowGo.transform.SetParent(slot.transform, false);
            var gRt = glowGo.GetComponent<RectTransform>();
            gRt.anchorMin = gRt.anchorMax = new Vector2(0.5f, 0.55f);
            gRt.sizeDelta = new Vector2(160f, 160f);
            var gImg = glowGo.GetComponent<Image>();
            gImg.sprite = glow;
            gImg.color = new Color(1f, 0.92f, 0.35f, 0.45f);
            gImg.raycastTarget = false;

            var shGo = new GameObject("Drop", typeof(RectTransform), typeof(Image));
            shGo.transform.SetParent(slot.transform, false);
            var sRt = shGo.GetComponent<RectTransform>();
            sRt.anchorMin = sRt.anchorMax = new Vector2(0.5f, 0.52f);
            sRt.anchoredPosition = new Vector2(6f, -8f);
            sRt.sizeDelta = new Vector2(128f, 128f);
            var sImg = shGo.GetComponent<Image>();
            sImg.sprite = starSprite;
            sImg.color = new Color(0f, 0f, 0f, 0.35f);
            sImg.preserveAspect = true;
            sImg.raycastTarget = false;

            var starGo = new GameObject("Star", typeof(RectTransform), typeof(Image));
            starGo.transform.SetParent(slot.transform, false);
            var stRt = starGo.GetComponent<RectTransform>();
            stRt.anchorMin = stRt.anchorMax = new Vector2(0.5f, 0.55f);
            stRt.sizeDelta = new Vector2(128f, 128f);
            var stImg = starGo.GetComponent<Image>();
            stImg.sprite = starSprite;
            stImg.color = Color.white;
            stImg.preserveAspect = true;
            stImg.raycastTarget = false;
        }
    }

    private static Sprite GetStarSpriteForBadges()
    {
        if (_cachedStarFromResources == null)
            _cachedStarFromResources = Resources.Load<Sprite>(StarIconResourcePath);

        if (_cachedStarFromResources != null)
            return _cachedStarFromResources;

        if (_cachedProceduralGoldStar == null)
            _cachedProceduralGoldStar = CreateFivePointStarSprite(160);

        return _cachedProceduralGoldStar;
    }

    private static Sprite GetOrCreateSoftGlowSprite()
    {
        if (_cachedSoftGlowCircle != null)
            return _cachedSoftGlowCircle;

        int size = 128;
        float cx = (size - 1) * 0.5f;
        float cy = (size - 1) * 0.5f;
        float r = size * 0.48f;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                float a = Mathf.Clamp01(1f - d / r);
                a = Mathf.Pow(a, 2.2f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply();
        _cachedSoftGlowCircle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _cachedSoftGlowCircle;
    }

    private static Sprite GetOrCreatePanelGradientSprite()
    {
        if (_cachedGradientPanelBg != null)
            return _cachedGradientPanelBg;

        int w = 16;
        int h = 256;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < h; y++)
        {
            float t = y / (float)(h - 1);
            Color top = new Color(1f, 0.99f, 0.94f, 1f);
            Color mid = new Color(1f, 0.94f, 0.82f, 1f);
            Color bot = new Color(0.98f, 0.88f, 0.98f, 1f);
            Color c = t > 0.5f ? Color.Lerp(mid, top, (t - 0.5f) * 2f) : Color.Lerp(bot, mid, t * 2f);
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, c);
        }

        tex.Apply();
        _cachedGradientPanelBg = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        return _cachedGradientPanelBg;
    }

    private static Sprite CreateFivePointStarSprite(int size)
    {
        float cx = (size - 1) * 0.5f;
        float cy = (size - 1) * 0.5f;
        float outer = size * 0.46f;
        float inner = outer * 0.38f;

        var verts = new List<Vector2>(10);
        for (int i = 0; i < 5; i++)
        {
            float aOut = Mathf.Deg2Rad * (-90f - i * 72f);
            verts.Add(new Vector2(cx + Mathf.Cos(aOut) * outer, cy + Mathf.Sin(aOut) * outer));
            float aIn = Mathf.Deg2Rad * (-90f - 36f - i * 72f);
            verts.Add(new Vector2(cx + Mathf.Cos(aIn) * inner, cy + Mathf.Sin(aIn) * inner));
        }

        Vector2[] poly = verts.ToArray();

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Trilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color core = new Color(1f, 0.9f, 0.25f, 1f);
        Color edge = new Color(0.92f, 0.48f, 0.08f, 1f);
        Color hi = new Color(1f, 0.98f, 0.75f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (!PointInPolygon(x + 0.5f, y + 0.5f, poly))
                {
                    tex.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));
                    continue;
                }

                float distEdge = DistanceToPolygonEdges(x + 0.5f, y + 0.5f, poly);
                float radial = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) / (outer + 2f);
                float edgeMix = Mathf.Clamp01(distEdge / (size * 0.08f + 0.01f));
                float hiMix = Mathf.Clamp01(1f - radial * 1.35f) * Mathf.Clamp01(distEdge / (size * 0.15f));

                Color c = Color.Lerp(edge, core, edgeMix);
                c = Color.Lerp(c, hi, hiMix * 0.55f);
                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static bool PointInPolygon(float x, float y, Vector2[] poly)
    {
        bool inside = false;
        int n = poly.Length;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            if (((poly[i].y > y) != (poly[j].y > y)) &&
                (x < (poly[j].x - poly[i].x) * (y - poly[i].y) / ((poly[j].y - poly[i].y) + 1e-5f) + poly[i].x))
                inside = !inside;
        }

        return inside;
    }

    private static float DistanceToPolygonEdges(float x, float y, Vector2[] poly)
    {
        float min = float.MaxValue;
        int n = poly.Length;
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            min = Mathf.Min(min, PointToSegmentDistance(new Vector2(x, y), poly[i], poly[j]));
        }

        return min;
    }

    private static float PointToSegmentDistance(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Vector2.Dot(p - a, ab) / (ab.sqrMagnitude + 1e-6f);
        t = Mathf.Clamp01(t);
        Vector2 proj = a + ab * t;
        return Vector2.Distance(p, proj);
    }

    private IEnumerator PulsePanel()
    {
        if (_panelRect == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < 0.55f)
        {
            elapsed += Time.unscaledDeltaTime;
            float w = Mathf.Sin(elapsed * 11f);
            float s = 1f + 0.05f * w;
            _panelRect.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        _panelRect.localScale = Vector3.one;
    }

    private void TryPlayCelebrationClip()
    {
        AudioClip clip = Resources.Load<AudioClip>(CelebrationClipResourcePath);
        if (clip == null)
        {
            Debug.LogWarning("[OtigoHomework] Ses bulunamadı. Resources.Load: " + CelebrationClipResourcePath);
            return;
        }

        if (_audioSource != null && gameObject.activeInHierarchy)
        {
            _audioSource.volume = 1f;
            _audioSource.mute = false;
            _audioSource.spatialBlend = 0f;
            _audioSource.PlayOneShot(clip, 1f);
            return;
        }

        AudioSource.PlayClipAtPoint(clip, Vector3.zero, 1f);
    }

    private static TextMeshProUGUI CreateTmpText(Transform parent, string name, float fontSize, FontStyles style, Color color, TMP_FontAsset font)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = "";
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.raycastTarget = false;
        if (font != null)
            tmp.font = font;
        return tmp;
    }

    private static Button CreatePrimaryButton(Transform parent, string label, float fontSize, TMP_FontAsset font)
    {
        var go = new GameObject("DismissButton", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(260f, 128f);
        var img = go.GetComponent<Image>();
        img.color = new Color(0.18f, 0.68f, 0.52f, 1f);
        img.sprite = WhiteSprite();
        img.type = Image.Type.Simple;
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.28f, 0.78f, 0.6f, 1f);
        colors.pressedColor = new Color(0.12f, 0.52f, 0.42f, 1f);
        btn.colors = colors;
        var bsh = go.AddComponent<Shadow>();
        bsh.effectColor = new Color(0f, 0f, 0f, 0.25f);
        bsh.effectDistance = new Vector2(4f, -4f);

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(go.transform, false);
        var lrt = labelGo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        var tmp = labelGo.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        if (font != null)
            tmp.font = font;

        var le = go.AddComponent<LayoutElement>();
        le.minWidth = 260f;
        le.preferredWidth = 260f;
        le.minHeight = 128f;
        le.preferredHeight = 128f;

        return btn;
    }

    private static Sprite WhiteSprite()
    {
        if (_whiteSprite != null)
            return _whiteSprite;

        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
        return _whiteSprite;
    }

    private static TMP_FontAsset ResolveTmpFont()
    {
        if (TMP_Settings.defaultFontAsset != null)
            return TMP_Settings.defaultFontAsset;

        TMP_FontAsset f = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF - Fallback");
        if (f != null)
            return f;

        return Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }
}
