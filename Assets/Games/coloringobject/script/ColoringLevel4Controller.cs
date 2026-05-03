using UnityEngine;

public class ColoringLevel4Controller : ColoringBaseController
{
    protected override void Start()
    {
        levelNumber = 4;
        nextSceneName = "Coloring_Level5";
        isLastLevel = false;

        base.Start();
    }
}