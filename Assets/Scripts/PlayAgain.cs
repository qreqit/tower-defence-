using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayAgain : MonoBehaviour
{
    public void playAgain()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void nextScene()
    {
        SceneManager.LoadScene ("Level1");
    }

    public void prevScene()
    {
        SceneManager.LoadScene ("Level1");
    }
}
