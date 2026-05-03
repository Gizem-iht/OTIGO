using UnityEngine;

public class StoryQuizLevel5Controller : StoryQuizBaseController
{
    protected override void Start()
    {
        levelNumber = 5;
        nextSceneName = "StoryQuiz_Level6";
        isLastLevel = false;
        base.Start();
    }
}