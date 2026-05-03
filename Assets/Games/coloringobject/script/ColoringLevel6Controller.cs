using UnityEngine;

public class ColoringLevel6Controller : ColoringBaseController
{
    protected override void Start()
    {
        levelNumber = 6;
        isLastLevel = true;
        tebrikSceneName = "Coloring_Tebrik";

        base.Start();
    }
}