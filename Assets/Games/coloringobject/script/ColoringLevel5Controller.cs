using UnityEngine;

public class ColoringLevel5Controller : ColoringBaseController
{
    protected override void Start()
    {
        levelNumber = 5;
        nextSceneName = "Coloring_Level6";
        isLastLevel = false;

        base.Start();
    }
}