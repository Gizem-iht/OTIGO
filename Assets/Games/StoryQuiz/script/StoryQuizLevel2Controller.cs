using UnityEngine;

public class StoryQuizLevel2Controller : StoryQuizBaseController
{
    protected override void Start()
    {
        levelNumber = 2;
        nextSceneName = "StoryQuiz_Level3";
        isLastLevel = false;
        base.Start();
    }
}