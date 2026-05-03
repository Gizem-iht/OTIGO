using UnityEngine;

public class ColoringLevel2Controller : ColoringBaseController
{
    protected override void Start()
    {
        levelNumber = 2;
        nextSceneName = "Coloring_Level3";
        isLastLevel = false;

        base.Start();
    }
}