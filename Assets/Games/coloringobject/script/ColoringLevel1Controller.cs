using UnityEngine;

public class ColoringLevel1Controller : ColoringBaseController
{
    protected override void Start()
    {
        levelNumber = 1;
        nextSceneName = "Coloring_Level2";
        isLastLevel = false;

        base.Start();
    }
}