using UnityEngine;

/// <summary>Fires projectiles on an interval, either in a fixed direction or tracking the player.</summary>
public class ProjectileTurret : MonoBehaviour
{
    public Projectile projectilePrefab;
    public float interval = 2f;
    public float startDelay = 0f;
    public float projectileSpeed = 10f;
    public bool trackPlayer = false;
    public float range = 45f;
    public float turnSpeed = 90f;
    public Vector3 muzzleLocalOffset = new Vector3(0f, 0.8f, 1.3f);

    float timer;

    void Start() { timer = startDelay; }

    void Update()
    {
        bool inRange = true;
        if (trackPlayer && PlayerController.Instance != null)
        {
            Vector3 to = PlayerController.Instance.transform.position - transform.position;
            to.y = 0f;
            inRange = to.magnitude <= range;
            if (inRange && to.sqrMagnitude > 0.01f)
            {
                Quaternion look = Quaternion.LookRotation(to);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * Time.deltaTime);
            }
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = interval;
            if (inRange) Fire();
        }
    }

    void Fire()
    {
        if (projectilePrefab == null) return;
        Vector3 pos = transform.TransformPoint(muzzleLocalOffset);
        Projectile p = Instantiate(projectilePrefab, pos, transform.rotation);
        p.speed = projectileSpeed;
    }
}
