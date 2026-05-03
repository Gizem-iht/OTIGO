using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLauncher : MonoBehaviour
{
    void Start()
    {
        SceneManager.LoadScene("Puzzle_MainMenu");
    }
}
