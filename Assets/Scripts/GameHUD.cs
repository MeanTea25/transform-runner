using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// In-game UI: the Transformation HUD (scale, rotation, speed mode / velocity), lives, level name,
/// control hints, plus pause / level-complete / game-over overlays. Built entirely from code.
/// </summary>
public class GameHUD : MonoBehaviour
{
    public static GameHUD Instance { get; private set; }

    public int levelIndex = 0;

    Text transformText, topRightText, messageText;
    GameObject pausePanel, completePanel, gameOverPanel;
    float messageTimer;

    static readonly Color Cyan = new Color(0.55f, 0.95f, 1f);

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        Build();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ------------------------------------------------------------------ build
    void Build()
    {
        Canvas canvas = UIFactory.CreateCanvas("HUD Canvas");
        Transform root = canvas.transform;

        // Transformation telemetry panel (top-left)
        UIFactory.Panel(root, "TelemetryBG", new Color(0f, 0f, 0f, 0.55f),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -290f), new Vector2(560f, -20f));
        transformText = UIFactory.Label(root, "Telemetry", "", 28, TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -282f), new Vector2(548f, -28f), Cyan);

        // Level + lives (top-right)
        topRightText = UIFactory.Label(root, "LevelInfo", "", 32, TextAnchor.UpperRight,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-640f, -130f), new Vector2(-24f, -24f), Color.white);

        // Controls hint (bottom)
        UIFactory.Label(root, "Hints",
            "WASD move  |  Space jump  |  X rotate  |  Y scale (Shift = shrink)  |  K speed mode  |  Q/E orbit camera  |  Esc pause",
            24, TextAnchor.LowerCenter, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(20f, 12f), new Vector2(-20f, 60f), new Color(1f, 1f, 1f, 0.8f));

        // Flash message (centre-top)
        messageText = UIFactory.Label(root, "Message", "", 48, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-600f, -230f), new Vector2(600f, -130f), Color.yellow);

        BuildPause(root);
        BuildComplete(root);
        BuildGameOver(root);
    }

    GameObject MakeOverlay(Transform root, string name, string title, Color titleColor)
    {
        RectTransform p = UIFactory.FullScreenPanel(root, name, new Color(0f, 0f, 0f, 0.7f));
        UIFactory.CenterLabel(p, title, 84, new Vector2(0f, 230f), new Vector2(1200f, 120f), titleColor);
        p.gameObject.SetActive(false);
        return p.gameObject;
    }

    void BuildPause(Transform root)
    {
        pausePanel = MakeOverlay(root, "PausePanel", "PAUSED", Color.white);
        Transform t = pausePanel.transform;
        UIFactory.MakeButton(t, "Resume", new Vector2(0f, 90f), new Vector2(420f, 80f), TogglePause);
        UIFactory.MakeButton(t, "Restart Level", new Vector2(0f, -10f), new Vector2(420f, 80f), Restart);
        UIFactory.MakeButton(t, "Level Select", new Vector2(0f, -110f), new Vector2(420f, 80f), GoToLevelSelect);
        UIFactory.MakeButton(t, "Main Menu", new Vector2(0f, -210f), new Vector2(420f, 80f), GameFlow.LoadMenu);
    }

    void BuildComplete(Transform root)
    {
        completePanel = MakeOverlay(root, "CompletePanel", "LEVEL COMPLETE!", new Color(0.4f, 1f, 0.55f));
        Transform t = completePanel.transform;
        bool hasNext = levelIndex + 1 < GameFlow.LevelCount;
        if (hasNext)
            UIFactory.MakeButton(t, "Next Level", new Vector2(0f, 90f), new Vector2(420f, 80f), NextLevel);
        else
            UIFactory.CenterLabel(t, "You finished every level!", 42, new Vector2(0f, 110f), new Vector2(1000f, 80f), Color.white);
        UIFactory.MakeButton(t, "Replay", new Vector2(0f, -10f), new Vector2(420f, 80f), Restart);
        UIFactory.MakeButton(t, "Level Select", new Vector2(0f, -110f), new Vector2(420f, 80f), GoToLevelSelect);
        UIFactory.MakeButton(t, "Main Menu", new Vector2(0f, -210f), new Vector2(420f, 80f), GameFlow.LoadMenu);
    }

    void BuildGameOver(Transform root)
    {
        gameOverPanel = MakeOverlay(root, "GameOverPanel", "GAME OVER", new Color(1f, 0.4f, 0.4f));
        Transform t = gameOverPanel.transform;
        UIFactory.MakeButton(t, "Retry", new Vector2(0f, 90f), new Vector2(420f, 80f), Restart);
        UIFactory.MakeButton(t, "Level Select", new Vector2(0f, -10f), new Vector2(420f, 80f), GoToLevelSelect);
        UIFactory.MakeButton(t, "Main Menu", new Vector2(0f, -110f), new Vector2(420f, 80f), GameFlow.LoadMenu);
    }

    // ----------------------------------------------------------------- update
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !completePanel.activeSelf && !gameOverPanel.activeSelf)
            TogglePause();

        if (messageTimer > 0f)
        {
            messageTimer -= Time.unscaledDeltaTime;
            if (messageTimer <= 0f) messageText.text = "";
        }

        PlayerController p = PlayerController.Instance;
        if (p == null || p.model == null) return;

        Vector3 s = p.ModelScale;
        Vector3 r = p.ModelEuler;
        Vector3 f = p.Facing;

        transformText.text =
            "TRANSFORMATION HUD\n" +
            "Scale     X " + s.x.ToString("0.00") + "   Y " + s.y.ToString("0.00") + "   Z " + s.z.ToString("0.00") + "\n" +
            "Rotation  X " + r.x.ToString("0.0") + "°   Y " + r.y.ToString("0.0") + "°   Z " + r.z.ToString("0.0") + "°\n" +
            "Facing    (" + f.x.ToString("0.00") + ", " + f.y.ToString("0.00") + ", " + f.z.ToString("0.00") + ")\n" +
            "Speed mode  " + p.Mode.ToString().ToUpper() + "  x" + p.SpeedMultiplier.ToString("0.0") + "\n" +
            "Velocity  " + p.HorizontalSpeed.ToString("0.0") + " m/s";

        topRightText.text =
            "Level " + (levelIndex + 1) + ": " + GameFlow.LevelTitles[Mathf.Clamp(levelIndex, 0, GameFlow.LevelCount - 1)] +
            "\nLives: " + p.Lives + " / " + p.maxLives;
    }

    // -------------------------------------------------------------------- API
    public void Flash(string msg, float seconds = 1.6f)
    {
        messageText.text = msg;
        messageTimer = seconds;
    }

    public void ShowLevelComplete()
    {
        Time.timeScale = 0f;
        completePanel.SetActive(true);
    }

    public void ShowGameOver()
    {
        Time.timeScale = 0f;
        gameOverPanel.SetActive(true);
    }

    void TogglePause()
    {
        bool pause = !pausePanel.activeSelf;
        pausePanel.SetActive(pause);
        Time.timeScale = pause ? 0f : 1f;
    }

    void Restart() { GameFlow.LoadLevel(levelIndex); }
    void NextLevel() { GameFlow.LoadLevel(levelIndex + 1); }
    void GoToLevelSelect()
    {
        MenuManager.StartOnLevelSelect = true;
        GameFlow.LoadMenu();
    }
}
