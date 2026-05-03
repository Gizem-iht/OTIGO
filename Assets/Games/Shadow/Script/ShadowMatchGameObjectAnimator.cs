using UnityEngine;

public class ShadowMatchGameObjectAnimator : MonoBehaviour
{
    public float animationDuration = 0.8f;

    private Vector3 targetScale;
    private float startTime;

    void Awake()
    {
        targetScale = transform.localScale;
        transform.localScale = Vector3.zero;
        startTime = Time.time;
    }

    void Update()
    {
        float timeElapsed = Time.time - startTime;
        float t = Mathf.Clamp01(timeElapsed / animationDuration);

        transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);

        if (t >= 1f)
        {
            enabled = false;
        }
    }
}