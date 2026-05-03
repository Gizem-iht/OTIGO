using UnityEngine;

public class StoryQuizLevel4Controller : StoryQuizBaseController
{
    protected override void Start()
    {
        levelNumber = 4;
        nextSceneName = "StoryQuiz_Level5";
        isLastLevel = false;
        base.Start();
    }
}