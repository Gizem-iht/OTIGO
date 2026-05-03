using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ColoringObjectUI : MonoBehaviour
{
    public string objectId = "ball";
    public Image fillImage;
    public float paintDuration = 0.45f;

    [Header("Wrong Effect")]
    public float shakeDuration = 0.25f;
    public float shakeStrength = 15f;

    private bool painted = false;
    private RectTransform rectTransform;
    private Vector2 originalPos;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPos = rectTransform.anchoredPosition;

        ResetPaint();
    }

    public void OnClickObject()
    {
        ColoringBaseController controller = FindObjectOfType<ColoringBaseController>();

        if (controller != null)
        {
            controller.OnColoringObjectClickedUI(this);
        }
    }

    public void Paint(Color targetColor)
    {
        if (painted) return;

        painted = true;
        StartCoroutine(PaintRoutine(targetColor));
    }

    public void SetPaintedInstant(Color targetColor)
    {
        painted = true;

        if (fillImage != null)
        {
            Color c = targetColor;
            c.a = 1f;
            fillImage.color = c;
            fillImage.transform.localScale = Vector3.one;
        }
    }

    public void ResetPaint()
    {
        painted = false;

        if (fillImage != null)
        {
            Color c = fillImage.color;
            c.a = 0f;
            fillImage.color = c;
            fillImage.transform.localScale = Vector3.zero;
        }
    }

    public void Shake()
    {
        StopCoroutine(nameof(ShakeRoutine));
        StartCoroutine(nameof(ShakeRoutine));
    }

    private IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;

            float x = Random.Range(-shakeStrength, shakeStrength);
            float y = Random.Range(-shakeStrength, shakeStrength);

            rectTransform.anchoredPosition = originalPos + new Vector2(x, y);

            yield return null;
        }

        rectTransform.anchoredPosition = originalPos;
    }

    private IEnumerator PaintRoutine(Color targetColor)
    {
        if (fillImage == null)
            yield break;

        float elapsed = 0f;

        Color startColor = targetColor;
        startColor.a = 0f;

        Color endColor = targetColor;
        endColor.a = 1f;

        fillImage.color = startColor;
        fillImage.transform.localScale = Vector3.zero;

        while (elapsed < paintDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / paintDuration;

            fillImage.color = Color.Lerp(startColor, endColor, t);
            fillImage.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);

            yield return null;
        }

        fillImage.color = endColor;
        fillImage.transform.localScale = Vector3.one;
    }
}
