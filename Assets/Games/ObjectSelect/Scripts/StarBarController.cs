using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class StarBarController : MonoBehaviour
{
    public List<Image> stars = new List<Image>();
    public Sprite emptyStar;
    public Sprite filledStar;

    private int currentStar = 0;

    public void Init(int totalStars)
    {
        currentStar = 0;

        for (int i = 0; i < stars.Count; i++)
        {
            if (stars[i] == null) continue;

            stars[i].sprite = emptyStar;
            stars[i].gameObject.SetActive(i < totalStars);
            stars[i].transform.localScale = Vector3.one;
        }
    }

    public void AddStar()
    {
        if (currentStar >= stars.Count) return;

        StartCoroutine(AnimateStar(stars[currentStar]));
        currentStar++;
    }

    private IEnumerator AnimateStar(Image star)
    {
        star.sprite = filledStar;
        star.transform.localScale = Vector3.zero;

        float t = 0f;
        float duration = 0.2f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float s = Mathf.SmoothStep(0f, 1f, t / duration);
            star.transform.localScale = Vector3.one * s;
            yield return null;
        }
    }

    public void ResetStars()
    {
        currentStar = 0;

        foreach (var star in stars)
        {
            if (star != null)
                star.sprite = emptyStar;
        }
    }
}