using UnityEngine;

public class StoryQuizLevel6Controller : StoryQuizBaseController
{
    protected override void Start()
    {
        levelNumber = 6;
        nextSceneName = "";
        isLastLevel = true;
        base.Start();
    }
}