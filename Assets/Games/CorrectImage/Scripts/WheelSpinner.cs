using UnityEngine;
using System.Collections;

public class WheelSpinner : MonoBehaviour
{
    public float minSpeed = 50f;
    public float maxSpeed = 200f;
    public float slowdownTime = 2f;
    public float waitTime = 1f;

    void Start()
    {
        StartCoroutine(SpinLoop());
    }

    IEnumerator SpinLoop()
    {
        while (true)
        {
            // Rastgele hız seç
            float startSpeed = Random.Range(minSpeed, maxSpeed);
            float speed = startSpeed;
            float t = 0f;

            // Başta hızlı dönme
            while (t < slowdownTime)
            {
                t += Time.deltaTime;
                speed = Mathf.Lerp(startSpeed, 0, t / slowdownTime);
                transform.Rotate(0, 0, speed * Time.deltaTime);
                yield return null;
            }

            // kısa bekleme
            yield return new WaitForSeconds(waitTime);
        }
    }
}
