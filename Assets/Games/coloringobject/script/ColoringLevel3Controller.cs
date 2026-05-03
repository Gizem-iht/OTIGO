using UnityEngine;

public class ColoringLevel3Controller : ColoringBaseController
{
    protected override void Start()
    {
        levelNumber = 3;
        nextSceneName = "Coloring_Level4";
        isLastLevel = false;

        base.Start();
    }
}