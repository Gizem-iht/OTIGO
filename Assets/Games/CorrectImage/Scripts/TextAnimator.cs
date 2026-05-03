using UnityEngine;
using TMPro;
using System.Collections;

public class TextAnimator : MonoBehaviour
{
    public float duration = 1f;

    private TextMeshProUGUI text;

    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();

        Color c = text.color;
        c.a = 0;
        text.color = c;

        StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;

            Color c = text.color;
            c.a = t;
            text.color = c;

            yield return null;
        }
    }
}
