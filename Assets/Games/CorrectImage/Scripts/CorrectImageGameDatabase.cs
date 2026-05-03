using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "QuizGame/Correct Image Game Database", fileName = "CorrectImage_GameDatabase")]
public class CorrectImageGameDatabase : ScriptableObject
{
    public List<CorrectImageCategoryData> categories = new List<CorrectImageCategoryData>();

    // Level bazlı ekranda kaç seçenek olacağı
    // Level1=2, Level2=2, Level3=3, Level4=3, Level5=4, Level6=4
    public List<int> optionsPerLevel = new List<int>() { 2, 2, 3, 3, 4, 4 };
}