using UnityEngine;
using TMPro;

public class ShadowMatchTitleAnimator : MonoBehaviour
{
    public float animationDuration = 2f;
    public float startYOffset = 500f;

    private TextMeshProUGUI textComponent;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float startTime;

    void Start()
    {
        textComponent = GetComponent<TextMeshProUGUI>();

        targetPosition = transform.position;
        startPosition = targetPosition + new Vector3(0, startYOffset, 0);
        transform.position = startPosition;

        startTime = Time.time;
    }

    void Update()
    {
        if (textComponent == null) return;

        float timeElapsed = Time.time - startTime;
        float t = Mathf.Clamp01(timeElapsed / animationDuration);

        transform.position = Vector3.Lerp(startPosition, targetPosition, t);

        if (t >= 1f)
            enabled = false;
    }
}