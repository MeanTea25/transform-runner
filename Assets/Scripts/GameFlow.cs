using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Scene names and navigation helpers shared by menus and levels.</summary>
public static class GameFlow
{
    public const string MainMenuScene = "MainMenu";

    public static readonly string[] LevelScenes = { "Level1", "Level2", "Level3" };
    public static readonly string[] LevelTitles = { "Meadow Run", "Scorched Canyon", "Neon Frostbite" };
    public static readonly string[] LevelBlurbs =
    {
        "Green fields, patrolling spikebots and a crossfire turret.",
        "Leap across floating canyon platforms past turrets and spinning blades.",
        "A frozen arena: perimeter scouts, tracking turrets and spinners."
    };

    public static int LevelCount { get { return LevelScenes.Length; } }

    public static void LoadLevel(int index)
    {
        index = Mathf.Clamp(index, 0, LevelScenes.Length - 1);
        Time.timeScale = 1f;
        SceneManager.LoadScene(LevelScenes[index]);
    }

    public static void LoadMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(MainMenuScene);
    }

    public static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
