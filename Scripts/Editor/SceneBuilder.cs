using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// One-click project setup:  Transform Game > Build All Scenes
/// Creates materials, the projectile prefab, the MainMenu scene and Level1-3, and registers them
/// in Build Settings. Runs automatically once, the first time the project opens.
/// Delete Assets/Scenes and re-run the menu item any time to rebuild.
/// </summary>
public static class SceneBuilder
{
    const string ModelDir = "Assets/Models/";
    const string MatDir = "Assets/Materials/";
    const string PrefabDir = "Assets/Prefabs/";
    const string SceneDir = "Assets/Scenes/";

    // ------------------------------------------------------------------ themes
    class Theme
    {
        public string id;
        public Color ground, wall, sky, ambient, fog, stone, light;
        public float fogDensity;
    }

    static readonly Theme MenuTheme = new Theme
    {
        id = "menu", ground = new Color(0.12f, 0.14f, 0.22f), wall = new Color(0.2f, 0.2f, 0.3f),
        sky = new Color(0.03f, 0.04f, 0.1f), ambient = new Color(0.4f, 0.45f, 0.6f), fog = Color.black,
        stone = new Color(0.35f, 0.4f, 0.55f), light = new Color(0.8f, 0.85f, 1f), fogDensity = 0f
    };
    static readonly Theme Meadow = new Theme
    {
        id = "meadow", ground = new Color(0.35f, 0.65f, 0.3f), wall = new Color(0.45f, 0.35f, 0.25f),
        sky = new Color(0.55f, 0.8f, 0.95f), ambient = new Color(0.55f, 0.6f, 0.65f), fog = new Color(0.7f, 0.85f, 0.95f),
        stone = new Color(0.55f, 0.55f, 0.58f), light = new Color(1f, 0.96f, 0.88f), fogDensity = 0.006f
    };
    static readonly Theme Canyon = new Theme
    {
        id = "canyon", ground = new Color(0.85f, 0.62f, 0.38f), wall = new Color(0.65f, 0.3f, 0.2f),
        sky = new Color(0.95f, 0.7f, 0.45f), ambient = new Color(0.7f, 0.55f, 0.45f), fog = new Color(0.95f, 0.75f, 0.55f),
        stone = new Color(0.75f, 0.45f, 0.3f), light = new Color(1f, 0.8f, 0.6f), fogDensity = 0.008f
    };
    static readonly Theme Frost = new Theme
    {
        id = "frost", ground = new Color(0.6f, 0.8f, 0.95f), wall = new Color(0.3f, 0.4f, 0.7f),
        sky = new Color(0.04f, 0.05f, 0.14f), ambient = new Color(0.35f, 0.4f, 0.6f), fog = new Color(0.05f, 0.07f, 0.18f),
        stone = new Color(0.3f, 0.35f, 0.6f), light = new Color(0.6f, 0.7f, 1f), fogDensity = 0.012f
    };

    static readonly Dictionary<string, Color> Palette = new Dictionary<string, Color>
    {
        { "Body", new Color(0.2f, 0.55f, 0.95f) }, { "Accent", new Color(1f, 0.6f, 0.1f) },
        { "Visor", new Color(0.1f, 1f, 0.9f) }, { "Dark", new Color(0.15f, 0.16f, 0.2f) },
        { "Hull", new Color(0.5f, 0.15f, 0.6f) }, { "Spike", new Color(0.9f, 0.15f, 0.15f) },
        { "Eye", new Color(1f, 1f, 0.2f) }, { "Base", new Color(0.35f, 0.38f, 0.42f) },
        { "Barrel", new Color(0.2f, 0.2f, 0.22f) }, { "Glow", new Color(1f, 0.3f, 0.1f) },
        { "Stone", new Color(0.55f, 0.55f, 0.58f) }, { "Wood", new Color(0.6f, 0.4f, 0.2f) },
        { "Band", new Color(0.25f, 0.2f, 0.15f) }, { "Warn", new Color(1f, 0.5f, 0f) },
        { "GoalGlow", new Color(0.3f, 1f, 0.5f) }, { "Bark", new Color(0.4f, 0.27f, 0.15f) },
        { "Leaf", new Color(0.2f, 0.6f, 0.25f) }, { "Crystal", new Color(0.5f, 0.8f, 1f) },
        { "Cactus", new Color(0.25f, 0.55f, 0.3f) }
    };
    static readonly HashSet<string> Emissive = new HashSet<string> { "Visor", "Eye", "Glow", "GoalGlow", "Crystal" };

    // ------------------------------------------------------------------- state
    static Theme theme;
    static Dictionary<string, string[]> modelMats = new Dictionary<string, string[]>();
    static Projectile projectilePrefab;
    static Transform gTerrain, gObstacles, gHazards, gDecor;

    // ------------------------------------------------------------ entry points
    [InitializeOnLoadMethod]
    static void AutoBuildOnFirstOpen()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(SceneDir + "MainMenu.unity")) return;
            if (!File.Exists(ModelDir + "Player_Body.obj")) return;
            BuildAll();
        };
    }

    [MenuItem("Transform Game/Build All Scenes")]
    public static void BuildAll()
    {
        AssetDatabase.Refresh();
        if (AssetDatabase.LoadAssetAtPath<GameObject>(ModelDir + "Player_Body.obj") == null)
        {
            Debug.LogError("[SceneBuilder] Models not imported yet (Assets/Models/*.obj). Wait for the import to finish, then use Transform Game > Build All Scenes.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureFolder("Materials");
        EnsureFolder("Prefabs");
        EnsureFolder("Scenes");
        LoadModelManifest();
        ConfigureInput();

        BuildMenuScene();
        BuildLevel1();
        BuildLevel2();
        BuildLevel3();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(SceneDir + "MainMenu.unity", true),
            new EditorBuildSettingsScene(SceneDir + "Level1.unity", true),
            new EditorBuildSettingsScene(SceneDir + "Level2.unity", true),
            new EditorBuildSettingsScene(SceneDir + "Level3.unity", true)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(SceneDir + "MainMenu.unity");
        Debug.Log("[SceneBuilder] Done. MainMenu + Level1-3 created and added to Build Settings. Open MainMenu and press Play.");
    }

    // ----------------------------------------------------------- project setup
    static void EnsureFolder(string name)
    {
        if (!AssetDatabase.IsValidFolder("Assets/" + name)) AssetDatabase.CreateFolder("Assets", name);
    }

    static void LoadModelManifest()
    {
        modelMats.Clear();
        foreach (string line in File.ReadAllLines(ModelDir + "materials.txt"))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            string[] kv = line.Split(':');
            modelMats[kv[0].Trim()] = kv[1].Trim().Split(',');
        }
    }

    // The scripts use the legacy Input class; Unity 6 projects default to the new Input System only.
    static void ConfigureInput()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
        if (assets == null || assets.Length == 0) return;
        SerializedObject so = new SerializedObject(assets[0]);
        SerializedProperty p = so.FindProperty("activeInputHandler");
        if (p != null && p.intValue == 1)
        {
            p.intValue = 2; // 0 = old, 1 = new, 2 = both
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Debug.LogWarning("[SceneBuilder] Active Input Handling switched to 'Both'. Restart the editor if Unity asks you to.");
        }
    }

    // --------------------------------------------------------------- materials
    static Shader PickShader()
    {
        Shader s = null;
        if (GraphicsSettings.currentRenderPipeline != null) s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Standard");
        if (s == null) s = Shader.Find("Sprites/Default");
        return s;
    }

    static Material ColorMat(string key, Color c, float emission = 0f)
    {
        string path = MatDir + key + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;

        m = new Material(PickShader());
        m.color = c;
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.15f);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.15f);
        if (emission > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * emission);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static Material PaletteMat(string name)
    {
        Color c;
        if (!Palette.TryGetValue(name, out c)) c = Color.gray;
        string key = "M_" + name;
        if (name == "Stone") { c = theme.stone; key = "M_Stone_" + theme.id; }
        return ColorMat(key, c, Emissive.Contains(name) ? 1.2f : 0f);
    }

    static Material FlatMat(Color c)
    {
        return ColorMat("Flat_" + ColorUtility.ToHtmlStringRGB(c), c);
    }

    // ------------------------------------------------------------ spawn helpers
    static GameObject Spawn(string model, Vector3 pos, float yaw, float scale, string name, Transform parent)
    {
        GameObject src = AssetDatabase.LoadAssetAtPath<GameObject>(ModelDir + model + ".obj");
        if (src == null) throw new System.Exception("Missing model: " + model);

        GameObject go = (GameObject)Object.Instantiate(src);
        go.name = name ?? model;
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        go.transform.localScale = Vector3.one * scale;

        string[] names = modelMats[model];
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
        {
            Material[] old = r.sharedMaterials;
            Material[] nw = new Material[old.Length];
            for (int i = 0; i < old.Length; i++)
                nw[i] = PaletteMat(names[Mathf.Min(i, names.Length - 1)]);
            r.sharedMaterials = nw;
        }
        return go;
    }

    /// <summary>Builds the player from separate parts (body + 2 legs + 2 arms) so PlayerAnimator can swing the limbs.</summary>
    static GameObject AssemblePlayer(Vector3 pos, Transform parent, bool demoWalk)
    {
        GameObject model = new GameObject("Model");
        if (parent != null) model.transform.SetParent(parent, false);
        model.transform.position = pos;
        model.transform.rotation = Quaternion.identity;

        Spawn("Player_Body", pos, 0f, 1f, "Body", model.transform);
        GameObject legL = Spawn("Player_Leg", pos + new Vector3(-0.18f, 0.85f, 0f), 0f, 1f, "LegL", model.transform);
        GameObject legR = Spawn("Player_Leg", pos + new Vector3(0.18f, 0.85f, 0f), 0f, 1f, "LegR", model.transform);
        GameObject armL = Spawn("Player_Arm", pos + new Vector3(-0.5f, 1.4f, 0f), 0f, 1f, "ArmL", model.transform);
        GameObject armR = Spawn("Player_Arm", pos + new Vector3(0.5f, 1.4f, 0f), 0f, 1f, "ArmR", model.transform);

        PlayerAnimator anim = model.AddComponent<PlayerAnimator>();
        anim.legL = legL.transform;
        anim.legR = legR.transform;
        anim.armL = armL.transform;
        anim.armR = armR.transform;
        anim.demoWalk = demoWalk;
        return model;
    }

    static void AddMeshColliders(GameObject go)
    {
        foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;
            MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
        }
    }

    static void MakeHazard(GameObject go)
    {
        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        go.AddComponent<Hazard>();
    }

    static void AddTrigger(GameObject go, Vector3 center, Vector3 size)
    {
        BoxCollider bc = go.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.center = center;
        bc.size = size;
    }

    static void AddBoundsTrigger(GameObject go, float shrink)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        Vector3 ls = go.transform.lossyScale;
        AddTrigger(go, go.transform.InverseTransformPoint(b.center),
            new Vector3(b.size.x / ls.x * shrink, b.size.y / ls.y * shrink, b.size.z / ls.z * shrink));
    }

    static GameObject Slab(string name, Vector3 center, Vector3 size, Color color, Transform parent)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube); // terrain only - never the player/enemies
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.position = center;
        g.transform.localScale = size;
        g.GetComponent<Renderer>().sharedMaterial = FlatMat(color);
        return g;
    }

    static Transform Group(string name)
    {
        return new GameObject(name).transform;
    }

    // -------------------------------------------------------- level components
    static GameObject Obstacle(string model, Vector3 pos, float yaw = 0f, float scale = 1f)
    {
        GameObject go = Spawn(model, pos, yaw, scale, null, gObstacles);
        AddMeshColliders(go);
        return go;
    }

    static GameObject Decor(string model, Vector3 pos, float yaw = 0f, float scale = 1f)
    {
        GameObject go = Spawn(model, pos, yaw, scale, null, gDecor);
        AddMeshColliders(go);
        return go;
    }

    static GameObject Patroller(Vector3[] pts, float speed, bool loop)
    {
        GameObject go = Spawn("Spikebot", pts[0], 0f, 1f, "PatrolSpikebot", gHazards);
        PatrolHazard ph = go.AddComponent<PatrolHazard>();
        ph.points = pts;
        ph.speed = speed;
        ph.loop = loop;
        AddBoundsTrigger(go, 0.8f);
        MakeHazard(go);
        return go;
    }

    static GameObject Trap(Vector3 pos)
    {
        GameObject go = Spawn("SpikeTrap", pos, 0f, 1f, null, gHazards);
        AddTrigger(go, new Vector3(0f, 0.4f, 0f), new Vector3(1.8f, 0.8f, 1.8f));
        MakeHazard(go);
        return go;
    }

    static GameObject Blade(Vector3 pos, float degPerSec)
    {
        GameObject go = Spawn("Spinner", pos, 0f, 1f, null, gHazards);
        Spinner sp = go.AddComponent<Spinner>();
        sp.degreesPerSecond = degPerSec;
        AddTrigger(go, new Vector3(0f, 0.6f, 0f), new Vector3(8.6f, 0.5f, 0.5f));
        MakeHazard(go);
        return go;
    }

    static GameObject Turret(Vector3 pos, float yaw, float interval, float delay, float speed, bool track)
    {
        GameObject go = Spawn("Turret", pos, yaw, 1f, null, gHazards);
        ProjectileTurret t = go.AddComponent<ProjectileTurret>();
        t.projectilePrefab = projectilePrefab;
        t.interval = interval;
        t.startDelay = delay;
        t.projectileSpeed = speed;
        t.trackPlayer = track;
        BoxCollider bc = go.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 0.6f, 0f);
        bc.size = new Vector3(1f, 1.2f, 1f);
        return go;
    }

    static GameObject Goal(Vector3 pos)
    {
        GameObject go = Spawn("Goal", pos, 0f, 1f, "Goal", gObstacles);
        go.AddComponent<GoalZone>();
        AddTrigger(go, new Vector3(0f, 1.5f, 0f), new Vector3(2.6f, 3f, 1.4f));
        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        return go;
    }

    // ------------------------------------------------------------ scene plumbing
    static Scene BeginScene(Theme t)
    {
        theme = t;
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = t.ambient;
        RenderSettings.fog = t.fogDensity > 0f;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = t.fog;
        RenderSettings.fogDensity = t.fogDensity;

        GameObject lightGo = new GameObject("Directional Light");
        Light l = lightGo.AddComponent<Light>();
        l.type = LightType.Directional;
        l.color = t.light;
        l.intensity = 1.1f;
        l.shadows = LightShadows.Soft;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        gTerrain = Group("Terrain");
        gObstacles = Group("Obstacles");
        gHazards = Group("Hazards");
        gDecor = Group("Decor");
        return scene;
    }

    static Camera MakeCamera(Vector3 pos, Color background)
    {
        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.position = pos;
        Camera cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = background;
        cam.farClipPlane = 250f;
        camGo.AddComponent<AudioListener>();
        return cam;
    }

    static void EnsureProjectilePrefab()
    {
        string path = PrefabDir + "Projectile.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) { projectilePrefab = existing.GetComponent<Projectile>(); return; }

        GameObject go = Spawn("Projectile", Vector3.zero, 0f, 1.5f, "Projectile", null);
        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        SphereCollider sc = go.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 0.25f;
        go.AddComponent<Projectile>();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        projectilePrefab = prefab.GetComponent<Projectile>();
    }

    static PlayerController BuildPlayerRig(Vector3 start, int levelIndex)
    {
        GameObject root = new GameObject("Player");
        root.transform.position = start;
        CharacterController cc = root.AddComponent<CharacterController>();
        cc.height = 1.85f;
        cc.radius = 0.45f;
        cc.center = new Vector3(0f, 0.925f, 0f);
        cc.stepOffset = 0.35f;
        cc.skinWidth = 0.05f;

        GameObject model = AssemblePlayer(start, root.transform, false);
        PlayerController pc = root.AddComponent<PlayerController>();
        pc.model = model.transform;

        Camera cam = MakeCamera(start + new Vector3(0f, 4f, -8f), theme.sky);
        CameraFollow cf = cam.gameObject.AddComponent<CameraFollow>();
        cf.target = root.transform;
        pc.cameraRig = cf;

        GameObject ui = new GameObject("GameHUD");
        GameHUD hud = ui.AddComponent<GameHUD>();
        hud.levelIndex = levelIndex;
        return pc;
    }

    static void FinishScene(Scene scene, string name)
    {
        EditorSceneManager.SaveScene(scene, SceneDir + name + ".unity");
    }

    // ---------------------------------------------------------------- MAIN MENU
    static void BuildMenuScene()
    {
        Scene scene = BeginScene(MenuTheme);
        EnsureProjectilePrefab();

        MakeCamera(new Vector3(-1.4f, 1.5f, -4.4f), theme.sky).transform.LookAt(new Vector3(-1.4f, 1.0f, 0f));

        Slab("Pedestal", new Vector3(0f, -0.1f, 0f), new Vector3(3.4f, 0.2f, 3.4f), theme.ground, gTerrain);
        GameObject showcase = AssemblePlayer(Vector3.zero, gDecor, true);
        showcase.name = "PlayerShowcase";
        showcase.transform.localScale = Vector3.one * 1.3f;
        Spinner sp = showcase.AddComponent<Spinner>();
        sp.degreesPerSecond = 35f;

        Spawn("Crystal", new Vector3(2.6f, 0f, 1.6f), 20f, 1f, null, gDecor);
        Spawn("Crystal", new Vector3(-2.2f, 0f, 2.4f), -35f, 0.8f, null, gDecor);
        Spawn("Spikebot", new Vector3(2.4f, 0f, -0.2f), 200f, 0.7f, "ShowcaseSpikebot", gDecor);

        GameObject mm = new GameObject("MenuManager");
        mm.AddComponent<MenuManager>();
        FinishScene(scene, "MainMenu");
    }

    // ------------------------------------------------------------------ LEVEL 1
    static void BuildLevel1()
    {
        Scene scene = BeginScene(Meadow);
        EnsureProjectilePrefab();

        Slab("Ground", new Vector3(0f, -0.5f, 30f), new Vector3(26f, 1f, 80f), theme.ground, gTerrain);
        Slab("WallL", new Vector3(-13.5f, 1f, 30f), new Vector3(1f, 2f, 80f), theme.wall, gTerrain);
        Slab("WallR", new Vector3(13.5f, 1f, 30f), new Vector3(1f, 2f, 80f), theme.wall, gTerrain);
        Slab("WallBack", new Vector3(0f, 1f, -10.5f), new Vector3(28f, 2f, 1f), theme.wall, gTerrain);
        Slab("WallFront", new Vector3(0f, 1f, 70.5f), new Vector3(28f, 2f, 1f), theme.wall, gTerrain);

        BuildPlayerRig(new Vector3(0f, 0.1f, 0f), 0);

        // static obstacles
        Obstacle("Pillar", new Vector3(-8f, 0f, 9f));
        Obstacle("Pillar", new Vector3(8f, 0f, 9f));
        Obstacle("Crate", new Vector3(-3f, 0f, 14f), 15f);
        Obstacle("Crate", new Vector3(-1.8f, 0f, 14.4f), -10f);
        Obstacle("Crate", new Vector3(-2.4f, 1f, 14.2f), 30f);
        Obstacle("Crate", new Vector3(4f, 0f, 30f), 20f);
        Obstacle("Crate", new Vector3(5.2f, 0f, 30.5f), -15f);
        Obstacle("Pillar", new Vector3(0f, 0f, 46f));
        Obstacle("Rock", new Vector3(6f, 0f, 22f), 40f, 1.2f);
        Obstacle("Rock", new Vector3(-6.5f, 0f, 36f), 120f, 1.4f);

        // patrolling hazards
        Patroller(new[] { new Vector3(-9f, 0f, 20f), new Vector3(9f, 0f, 20f) }, 3.5f, false);
        Patroller(new[] { new Vector3(9f, 0f, 38f), new Vector3(-9f, 0f, 38f) }, 4.5f, false);
        Patroller(new[] { new Vector3(-4f, 0f, 42f), new Vector3(4f, 0f, 42f), new Vector3(4f, 0f, 50f), new Vector3(-4f, 0f, 50f) }, 3f, true);

        // static hazards
        Trap(new Vector3(-6f, 0f, 28f));
        Trap(new Vector3(6f, 0f, 26f));
        Trap(new Vector3(0f, 0f, 58f));

        // projectile turrets (crossfire)
        Turret(new Vector3(-12f, 0f, 44f), 90f, 2.2f, 0f, 8f, false);
        Turret(new Vector3(12f, 0f, 56f), -90f, 2.2f, 1.1f, 8f, false);

        Goal(new Vector3(0f, 0f, 64f));

        for (int i = 0; i < 7; i++)
        {
            float z = i * 10f + 2f;
            float s = 1f + 0.15f * ((i * 7) % 3);
            Decor("Tree", new Vector3(-11f, 0f, z), i * 53f, s);
            Decor("Tree", new Vector3(11f, 0f, z + 4f), i * 91f, s);
        }
        FinishScene(scene, "Level1");
    }

    // ------------------------------------------------------------------ LEVEL 2
    static void BuildLevel2()
    {
        Scene scene = BeginScene(Canyon);
        EnsureProjectilePrefab();

        // floating platforms with gaps (jump with Space; Boost mode makes it easier)
        Slab("Platform1", new Vector3(0f, -0.5f, 5f), new Vector3(14f, 1f, 20f), theme.ground, gTerrain);
        Slab("Platform2", new Vector3(0f, -0.5f, 28f), new Vector3(10f, 1f, 20f), theme.ground, gTerrain);
        Slab("Platform3", new Vector3(0f, -0.5f, 51f), new Vector3(12f, 1f, 20f), theme.ground, gTerrain);
        Slab("Platform4", new Vector3(0f, -0.5f, 74.5f), new Vector3(10f, 1f, 21f), theme.ground, gTerrain);

        // distant canyon walls (scenery)
        for (int i = 0; i < 5; i++)
        {
            float z = i * 22f - 5f;
            Slab("CanyonL" + i, new Vector3(-20f - (i % 2) * 3f, 5f, z), new Vector3(8f, 14f + (i % 3) * 4f, 20f), theme.wall, gTerrain);
            Slab("CanyonR" + i, new Vector3(20f + ((i + 1) % 2) * 3f, 5f, z + 8f), new Vector3(8f, 14f + ((i + 2) % 3) * 4f, 20f), theme.wall, gTerrain);
        }

        BuildPlayerRig(new Vector3(0f, 0.1f, 0f), 1);

        // platform 1: warm-up
        Obstacle("Cactus", new Vector3(-5.5f, 0f, 8f), 0f, 1.2f);
        Obstacle("Rock", new Vector3(5.5f, 0f, 3f), 70f, 1.3f);
        Obstacle("Crate", new Vector3(3f, 0f, 11f), 20f);

        // platform 2: patrolling spikebots + spike traps
        Patroller(new[] { new Vector3(-3.5f, 0f, 26f), new Vector3(3.5f, 0f, 26f) }, 3.5f, false);
        Patroller(new[] { new Vector3(3.5f, 0f, 33f), new Vector3(-3.5f, 0f, 33f) }, 4f, false);
        Trap(new Vector3(-3.6f, 0f, 21f));
        Trap(new Vector3(3.6f, 0f, 36f));

        // platform 3: turret gauntlet with crate cover
        Turret(new Vector3(-5.3f, 0f, 48f), 90f, 2f, 0f, 10f, false);
        Turret(new Vector3(5.3f, 0f, 54f), -90f, 2f, 1f, 10f, false);
        Obstacle("Crate", new Vector3(0f, 0f, 45.5f), 10f);
        Obstacle("Crate", new Vector3(-2.5f, 0f, 51f), -20f);
        Obstacle("Crate", new Vector3(2.5f, 0f, 57f), 35f);

        // platform 4: spinning blades then the exit
        Blade(new Vector3(0f, 0f, 70f), 90f);
        Blade(new Vector3(0f, 0f, 77f), -110f);
        Goal(new Vector3(0f, 0f, 83f));

        FinishScene(scene, "Level2");
    }

    // ------------------------------------------------------------------ LEVEL 3
    static void BuildLevel3()
    {
        Scene scene = BeginScene(Frost);
        EnsureProjectilePrefab();

        Slab("IceFloor", new Vector3(0f, -0.5f, 0f), new Vector3(46f, 1f, 46f), theme.ground, gTerrain);
        Slab("WallN", new Vector3(0f, 1.5f, 23.5f), new Vector3(48f, 3f, 1f), theme.wall, gTerrain);
        Slab("WallS", new Vector3(0f, 1.5f, -23.5f), new Vector3(48f, 3f, 1f), theme.wall, gTerrain);
        Slab("WallE", new Vector3(23.5f, 1.5f, 0f), new Vector3(1f, 3f, 46f), theme.wall, gTerrain);
        Slab("WallW", new Vector3(-23.5f, 1.5f, 0f), new Vector3(1f, 3f, 46f), theme.wall, gTerrain);

        BuildPlayerRig(new Vector3(0f, 0.1f, -19f), 2);

        // crystal cover
        Obstacle("Crystal", new Vector3(-6f, 0f, -8f), 15f, 1.2f);
        Obstacle("Crystal", new Vector3(6f, 0f, -6f), -40f, 1.1f);
        Obstacle("Crystal", new Vector3(0f, 0f, -2f), 60f, 1.3f);
        Obstacle("Crystal", new Vector3(-12f, 0f, 6f), 100f, 1.2f);
        Obstacle("Crystal", new Vector3(12f, 0f, 8f), 5f, 1.2f);
        Obstacle("Pillar", new Vector3(-17f, 0f, 0f));
        Obstacle("Pillar", new Vector3(17f, 0f, 0f));

        // perimeter scouts (counter-rotating loops)
        Patroller(new[] { new Vector3(-14f, 0f, -14f), new Vector3(14f, 0f, -14f), new Vector3(14f, 0f, 14f), new Vector3(-14f, 0f, 14f) }, 5f, true);
        Patroller(new[] { new Vector3(14f, 0f, 14f), new Vector3(-14f, 0f, 14f), new Vector3(-14f, 0f, -14f), new Vector3(14f, 0f, -14f) }, 5f, true);

        // spinning blades in the arena
        Blade(new Vector3(-9f, 0f, 4f), 80f);
        Blade(new Vector3(9f, 0f, 4f), -80f);
        Blade(new Vector3(0f, 0f, 11f), 70f);

        // tracking turrets in the corners
        Turret(new Vector3(-20f, 0f, -20f), 45f, 3f, 0f, 8f, true);
        Turret(new Vector3(20f, 0f, -20f), -45f, 3f, 0.75f, 8f, true);
        Turret(new Vector3(-20f, 0f, 20f), 135f, 3f, 1.5f, 8f, true);
        Turret(new Vector3(20f, 0f, 20f), -135f, 3f, 2.25f, 8f, true);

        Goal(new Vector3(0f, 0f, 19f));

        Decor("Crystal", new Vector3(-21f, 0f, -6f), 0f, 1.6f);
        Decor("Crystal", new Vector3(21f, 0f, 6f), 90f, 1.6f);
        Decor("Crystal", new Vector3(-21f, 0f, 10f), 40f, 1.3f);
        Decor("Crystal", new Vector3(21f, 0f, -12f), 10f, 1.3f);

        FinishScene(scene, "Level3");
    }
}
