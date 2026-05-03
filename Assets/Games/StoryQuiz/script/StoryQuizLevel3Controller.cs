using UnityEngine;

public class StoryQuizLevel3Controller : StoryQuizBaseController
{
    protected override void Start()
    {
        levelNumber = 3;
        nextSceneName = "StoryQuiz_Level4";
        isLastLevel = false;
        base.Start();
    }
}