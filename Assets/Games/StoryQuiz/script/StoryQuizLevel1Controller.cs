using UnityEngine;

public class StoryQuizLevel1Controller : StoryQuizBaseController
{
    protected override void Start()
    {
        levelNumber = 1;
        nextSceneName = "StoryQuiz_Level2";
        isLastLevel = false;
        base.Start();
    }
}