using UnityEngine;
using UnityEngine.UI;

/// <summary>Main menu, level select and about screens (all in one scene, built from code).</summary>
public class MenuManager : MonoBehaviour
{
    /// <summary>Set by the in-game menus so "Level Select" opens that screen directly.</summary>
    public static bool StartOnLevelSelect;

    [Header("About screen - edit these")]
    public string developerName = "Rojer Victor C. Nuqui";
    public string courseName = "Applied Mathematics for Games";
    public string schoolName = "Mapua Malayan Colleges Mindanao";

    GameObject mainPanel, levelPanel, aboutPanel;

    void Awake()
    {
        Time.timeScale = 1f;
        Canvas canvas = UIFactory.CreateCanvas("Menu Canvas");
        Transform root = canvas.transform;

        mainPanel = BuildMain(root);
        levelPanel = BuildLevelSelect(root);
        aboutPanel = BuildAbout(root);

        if (StartOnLevelSelect) Show(levelPanel);
        else Show(mainPanel);
        StartOnLevelSelect = false;
    }

    void Show(GameObject panel)
    {
        mainPanel.SetActive(panel == mainPanel);
        levelPanel.SetActive(panel == levelPanel);
        aboutPanel.SetActive(panel == aboutPanel);
    }

    static readonly Color Dim = new Color(0.02f, 0.04f, 0.1f, 0.35f);

    GameObject BuildMain(Transform root)
    {
        RectTransform p = UIFactory.FullScreenPanel(root, "MainPanel", new Color(0f, 0f, 0f, 0f));
        UIFactory.Panel(p, "Backdrop", Dim, new Vector2(0f, 0f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
        Text title = UIFactory.CenterLabel(p, "TRANSFORM RUNNER", 96, new Vector2(-480f, 330f), new Vector2(900f, 140f), Color.white);
        title.alignment = TextAnchor.MiddleCenter;
        UIFactory.CenterLabel(p, "translate  •  rotate  •  scale", 38, new Vector2(-480f, 250f), new Vector2(900f, 60f), new Color(0.55f, 0.95f, 1f));

        Vector2 size = new Vector2(440f, 84f);
        UIFactory.MakeButton(p, "Start Game", new Vector2(-480f, 110f), size, () => GameFlow.LoadLevel(0));
        UIFactory.MakeButton(p, "Level Select", new Vector2(-480f, 0f), size, () => Show(levelPanel));
        UIFactory.MakeButton(p, "About", new Vector2(-480f, -110f), size, () => Show(aboutPanel));
        UIFactory.MakeButton(p, "Quit", new Vector2(-480f, -220f), size, GameFlow.Quit);
        return p.gameObject;
    }

    GameObject BuildLevelSelect(Transform root)
    {
        RectTransform p = UIFactory.FullScreenPanel(root, "LevelSelectPanel", new Color(0.02f, 0.04f, 0.1f, 0.88f));
        UIFactory.CenterLabel(p, "SELECT LEVEL", 80, new Vector2(0f, 400f), new Vector2(1200f, 110f), Color.white);

        for (int i = 0; i < GameFlow.LevelCount; i++)
        {
            int index = i;
            float y = 220f - i * 190f;
            UIFactory.MakeButton(p, "Level " + (i + 1) + ":  " + GameFlow.LevelTitles[i],
                new Vector2(0f, y), new Vector2(820f, 100f), () => GameFlow.LoadLevel(index));
            UIFactory.CenterLabel(p, GameFlow.LevelBlurbs[i], 28, new Vector2(0f, y - 75f), new Vector2(1100f, 44f),
                new Color(1f, 1f, 1f, 0.75f));
        }
        UIFactory.MakeButton(p, "Back", new Vector2(0f, -400f), new Vector2(320f, 80f), () => Show(mainPanel));
        return p.gameObject;
    }

    GameObject BuildAbout(Transform root)
    {
        RectTransform p = UIFactory.FullScreenPanel(root, "AboutPanel", new Color(0.02f, 0.04f, 0.1f, 0.92f));
        UIFactory.CenterLabel(p, "ABOUT", 80, new Vector2(0f, 440f), new Vector2(1200f, 110f), Color.white);

        string credits =
            "DEVELOPER CREDITS\n" +
            "Developer: " + developerName + "\n" +
            "Course: " + courseName + "   |   " + schoolName + "\n\n" +
            "PROJECT OVERVIEW\n" +
            "A three-level 3D obstacle course that demonstrates vectors, coordinate systems and linear " +
            "transformations. Your custom robot is translated with WASD, rotated with a quaternion about a " +
            "chosen axis, and scaled in real time while patrolling hazards and turret projectiles try to stop you. " +
            "Reach the glowing ring in every level.";
        UIFactory.Label(p, "Credits", credits, 30, TextAnchor.UpperLeft,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-880f, -60f), new Vector2(-20f, 380f), Color.white);

        string controls =
            "CONTROLS\n" +
            "W A S D  -  translate (move)\n" +
            "Space  -  jump\n" +
            "X  -  rotate model about the Y axis (hold; Shift reverses)\n" +
            "Y  -  scale model (hold to grow; Shift + Y to shrink)\n" +
            "K  -  cycle speed mode: Normal x1 / Boost x2 / Slow x0.5\n" +
            "Q / E or right-mouse drag  -  orbit camera\n" +
            "Esc  -  pause menu";
        UIFactory.Label(p, "Controls", controls, 30, TextAnchor.UpperLeft,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(20f, -60f), new Vector2(900f, 380f), new Color(0.55f, 0.95f, 1f));

        UIFactory.MakeButton(p, "Back", new Vector2(0f, -400f), new Vector2(320f, 80f), () => Show(mainPanel));
        return p.gameObject;
    }
}
