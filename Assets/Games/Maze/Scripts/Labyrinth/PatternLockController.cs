using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PatternLockController : MonoBehaviour
{
    [Header("Nodes 1-9")]
    public PatternNode[] nodes;

    [Header("Timing")]
    public float resetDelay = 0.8f;

    [Header("Wrong Pattern Effect")]
    public Color wrongColor = Color.red;
    public float wrongFlashDuration = 0.2f;
    public float shakeDuration = 0.2f;
    public float shakeStrength = 12f;

    private List<int> selectedPattern = new List<int>();
    private bool inputLocked = false;

    public Action OnPatternSuccess;
    public Action OnPatternFail;

    private string correctPattern = "1,2,3,6,9";
    private RectTransform rectTransform;
    private Vector2 originalAnchoredPos;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
            originalAnchoredPos = rectTransform.anchoredPosition;
    }

    private void Start()
    {
        if (ParentPatternProvider.Instance != null)
            correctPattern = ParentPatternProvider.Instance.CurrentPattern;

        Debug.Log("Kullanılacak parent pattern: " + correctPattern);
        ResetPattern();
    }

    public void AddNode(int nodeId)
    {
        if (inputLocked) return;

        if (selectedPattern.Contains(nodeId))
            return;

        selectedPattern.Add(nodeId);

        PatternNode node = nodes.FirstOrDefault(n => n != null && n.nodeId == nodeId);
        if (node != null)
            node.SetSelected(true);

        Debug.Log("Seçilen node: " + nodeId + " | Pattern: " + string.Join(",", selectedPattern));

        if (selectedPattern.Count >= GetCorrectPatternCount())
            CheckPattern();
    }

    private int GetCorrectPatternCount()
    {
        return correctPattern.Split(',').Length;
    }

    private void CheckPattern()
    {
        inputLocked = true;

        string entered = string.Join(",", selectedPattern);

        if (entered == correctPattern)
        {
            Debug.Log("Doğru pattern!");
            OnPatternSuccess?.Invoke();
        }
        else
        {
            Debug.Log("Yanlış pattern! Girilen: " + entered + " | Beklenen: " + correctPattern);
            OnPatternFail?.Invoke();
            StartCoroutine(PlayWrongPatternEffect());
        }
    }

    private IEnumerator PlayWrongPatternEffect()
    {
        SetAllNodeColors(wrongColor);
        yield return new WaitForSecondsRealtime(wrongFlashDuration);

        if (rectTransform != null)
            yield return StartCoroutine(ShakePanel());

        ResetPattern();
    }

    private IEnumerator ShakePanel()
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float offsetX = UnityEngine.Random.Range(-shakeStrength, shakeStrength);
            float offsetY = UnityEngine.Random.Range(-shakeStrength, shakeStrength);

            rectTransform.anchoredPosition = originalAnchoredPos + new Vector2(offsetX, offsetY);
            yield return null;
        }

        rectTransform.anchoredPosition = originalAnchoredPos;
    }

    private void SetAllNodeColors(Color color)
    {
        if (nodes == null) return;

        foreach (var node in nodes)
        {
            if (node != null)
                node.SetColorDirect(color);
        }
    }

    public void ResetPattern()
    {
        StopAllCoroutines();

        selectedPattern.Clear();
        inputLocked = false;

        if (rectTransform != null)
            rectTransform.anchoredPosition = originalAnchoredPos;

        if (nodes != null)
        {
            foreach (var node in nodes)
            {
                if (node != null)
                    node.SetSelected(false);
            }
        }
    }
}