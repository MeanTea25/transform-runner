using System.Collections;
using UnityEngine;

/// <summary>
/// Player locomotion and the three live transformation controls.
///   W A S D  - translation (camera-relative), Space - jump
///   X        - rotation of the model about a designated axis (hold; Shift reverses)
///   Y        - scale of the model (hold to grow; Shift + Y to shrink)
///   K        - cycles the movement speed mode: Normal x1 -> Boost x2 -> Slow x0.5
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    public enum SpeedMode { Normal, Boost, Slow }

    [Header("References")]
    public Transform model;
    public CameraFollow cameraRig;

    [Header("Translation")]
    public float baseSpeed = 6f;
    public float jumpSpeed = 8f;
    public float gravity = -20f;

    [Header("Rotation (X key)")]
    public Vector3 rotationAxis = Vector3.up;   // the designated axis (model-local)
    public float rotateSpeed = 120f;            // degrees / second

    [Header("Scale (Y key)")]
    public float scaleRate = 1f;                // scale units / second
    public float minScale = 0.4f;
    public float maxScale = 2.5f;

    [Header("Health")]
    public int maxLives = 3;
    public float invulnerableTime = 1.5f;
    public float killHeight = -15f;

    public SpeedMode Mode { get; private set; }
    public int Lives { get; private set; }
    public float SpeedMultiplier
    {
        get { return Mode == SpeedMode.Boost ? 2f : (Mode == SpeedMode.Slow ? 0.5f : 1f); }
    }
    public Vector3 ModelScale { get { return model.localScale; } }
    public Vector3 ModelEuler { get { return model.localEulerAngles; } }
    public Vector3 Facing { get { return model.forward; } }
    public float HorizontalSpeed { get; private set; }
    public bool IsGrounded { get { return controller != null && controller.isGrounded; } }

    CharacterController controller;
    float baseHeight, baseRadius, baseStep;
    float verticalVelocity;
    float invulnerable;
    Vector3 spawnPoint;

    void Awake()
    {
        Instance = this;
        controller = GetComponent<CharacterController>();
        baseHeight = controller.height;
        baseRadius = controller.radius;
        baseStep = controller.stepOffset;
        Lives = maxLives;
        spawnPoint = transform.position;
        if (model == null && transform.childCount > 0) model = transform.GetChild(0);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        if (invulnerable > 0f) invulnerable -= Time.deltaTime;

        HandleTransformKeys();
        HandleMovement();

        if (transform.position.y < killHeight) Hit();
    }

    // ------------------------------------------------------------ X / Y / K
    void HandleTransformKeys()
    {
        float dt = Time.deltaTime;
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        float dir = shift ? -1f : 1f;

        // ROTATION: Quaternion.AngleAxis(angle, axis) composed with the current orientation.
        if (Input.GetKey(KeyCode.X))
        {
            Quaternion delta = Quaternion.AngleAxis(dir * rotateSpeed * dt, rotationAxis.normalized);
            model.localRotation = model.localRotation * delta;
        }

        // SCALE: component-wise Vector3 scale, kept uniform and clamped.
        if (Input.GetKey(KeyCode.Y))
        {
            float s = Mathf.Clamp(model.localScale.x + dir * scaleRate * dt, minScale, maxScale);
            model.localScale = new Vector3(s, s, s);
            ResizeController(s);
        }

        // SPEED MODE
        if (Input.GetKeyDown(KeyCode.K))
        {
            Mode = (SpeedMode)(((int)Mode + 1) % 3);
            if (GameHUD.Instance != null)
                GameHUD.Instance.Flash("Speed mode: " + Mode.ToString().ToUpper() + "  (x" + SpeedMultiplier.ToString("0.0") + ")");
        }
    }

    // The CharacterController does not follow the visual scale, so match it by hand.
    void ResizeController(float s)
    {
        controller.height = baseHeight * s;
        controller.radius = Mathf.Min(baseRadius * s, controller.height * 0.5f);
        controller.center = new Vector3(0f, controller.height * 0.5f, 0f);
        controller.stepOffset = Mathf.Min(baseStep * s, controller.height);
    }

    // ------------------------------------------------------------- WASD
    void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        float yaw = cameraRig != null ? cameraRig.yaw :
                    (Camera.main != null ? Camera.main.transform.eulerAngles.y : 0f);

        // Input vector rotated into world space by the camera's yaw.
        Vector3 wish = Quaternion.Euler(0f, yaw, 0f) * new Vector3(h, 0f, v);
        if (wish.sqrMagnitude > 1f) wish.Normalize();

        if (controller.isGrounded)
        {
            if (verticalVelocity < 0f) verticalVelocity = -2f;
            if (Input.GetButtonDown("Jump")) verticalVelocity = jumpSpeed;
        }
        verticalVelocity += gravity * Time.deltaTime;

        // TRANSLATION: displacement = velocity * dt
        Vector3 velocity = wish * (baseSpeed * SpeedMultiplier) + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        Vector3 actual = controller.velocity;
        HorizontalSpeed = new Vector3(actual.x, 0f, actual.z).magnitude;
    }

    // ----------------------------------------------------------- Damage
    public void Hit()
    {
        if (invulnerable > 0f || Time.timeScale == 0f) return;

        Lives--;
        if (Lives <= 0)
        {
            if (GameHUD.Instance != null) GameHUD.Instance.ShowGameOver();
            return;
        }

        if (GameHUD.Instance != null) GameHUD.Instance.Flash("Ouch! Lives left: " + Lives);
        Respawn();
        invulnerable = invulnerableTime;
        StartCoroutine(Blink());
    }

    void Respawn()
    {
        controller.enabled = false;
        transform.position = spawnPoint;
        controller.enabled = true;
        verticalVelocity = 0f;
    }

    IEnumerator Blink()
    {
        Renderer[] rs = model.GetComponentsInChildren<Renderer>();
        float t = 0f;
        bool on = true;
        while (t < invulnerableTime)
        {
            on = !on;
            foreach (Renderer r in rs) r.enabled = on;
            yield return new WaitForSeconds(0.1f);
            t += 0.1f;
        }
        foreach (Renderer r in rs) r.enabled = true;
    }
}
