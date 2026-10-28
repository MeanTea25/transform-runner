using UnityEngine;

/// <summary>
/// Procedural walk / jump / idle animation. The robot is built from separate parts (body, 2 legs, 2 arms),
/// each pivoting at its joint, so no rig or Animator Controller is needed.
///   Walking: legs and arms swing in opposition, faster and wider with speed.
///   Airborne: arms raise, one leg tucks.
///   Idle: gentle arm sway.
/// </summary>
public class PlayerAnimator : MonoBehaviour
{
    public Transform legL, legR, armL, armR;
    public PlayerController player;
    public bool demoWalk;               // menu showcase: walk in place

    public float legSwing = 45f;        // degrees
    public float armSwing = 35f;
    public float smoothing = 18f;

    float phase;

    void Awake()
    {
        if (player == null) player = GetComponentInParent<PlayerController>();
    }

    void Update()
    {
        float speed = 0f;
        bool grounded = true;
        if (demoWalk) speed = 3f;
        else if (player != null)
        {
            speed = player.HorizontalSpeed;
            grounded = player.IsGrounded;
        }

        float dt = Time.deltaTime;
        phase += (5f + speed * 1.1f) * dt;       // faster stride at higher speed
        float move = Mathf.Clamp01(speed / 2.5f);
        float s = Mathf.Sin(phase);

        Quaternion tLegL, tLegR, tArmL, tArmR;
        if (grounded)
        {
            float idle = Mathf.Sin(Time.time * 1.8f) * 3f * (1f - move);
            tLegL = Quaternion.Euler(s * legSwing * move, 0f, 0f);
            tLegR = Quaternion.Euler(-s * legSwing * move, 0f, 0f);
            tArmL = Quaternion.Euler(-s * armSwing * move + idle, 0f, 0f);
            tArmR = Quaternion.Euler(s * armSwing * move - idle, 0f, 0f);
        }
        else
        {
            tLegL = Quaternion.Euler(-40f, 0f, 0f);   // tucked forward
            tLegR = Quaternion.Euler(25f, 0f, 0f);    // trailing back
            tArmL = Quaternion.Euler(-140f, 0f, 0f);  // arms up
            tArmR = Quaternion.Euler(-140f, 0f, 0f);
        }

        float k = 1f - Mathf.Exp(-smoothing * dt);
        Blend(legL, tLegL, k);
        Blend(legR, tLegR, k);
        Blend(armL, tArmL, k);
        Blend(armR, tArmR, k);
    }

    static void Blend(Transform t, Quaternion target, float k)
    {
        if (t != null) t.localRotation = Quaternion.Slerp(t.localRotation, target, k);
    }
}
