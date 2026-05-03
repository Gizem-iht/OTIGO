using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "QuizGame/Correct Image Category Data", fileName = "CorrectImage_Category_")]
public class CorrectImageCategoryData : ScriptableObject
{
    public string categoryId;
    public string categoryName;
    public List<ItemData> items = new List<ItemData>();
}