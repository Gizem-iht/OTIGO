using UnityEngine;

[System.Serializable]
public class ColoringTask
{
    public string objectId;
    public Color correctColor;
    public string taskText;
    public AudioClip taskVoice;
    public bool completed;
}