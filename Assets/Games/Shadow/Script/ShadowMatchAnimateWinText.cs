using UnityEngine;
using TMPro;
using System.Collections;

public class ShadowMatchAnimateWinText : MonoBehaviour
{
    public TextMeshProUGUI text;
    public float duration = 0.5f;

    private Vector3 startScale;

    void Awake()
    {
        startScale = transform.localScale;
        transform.localScale = Vector3.zero;

        if (text != null)
            text.text = "";
    }

    public void Show(string message)
    {
        if (text == null) return;

        text.text = message;
        StopAllCoroutines();
        StartCoroutine(ScaleIn());
    }

    IEnumerator ScaleIn()
    {
        float t = 0f;
        transform.localScale = Vector3.zero;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            transform.localScale = Vector3.Lerp(Vector3.zero, startScale, t);
            yield return null;
        }

        transform.localScale = startScale;
    }
}