using UnityEngine;

/// <summary>A moving projectile: translates along its forward vector until it hits something.</summary>
public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public float lifetime = 6f;

    void Start() { Destroy(gameObject, lifetime); }

    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerController p = other.GetComponentInParent<PlayerController>();
        if (p != null)
        {
            p.Hit();
            Destroy(gameObject);
            return;
        }
        if (other.isTrigger) return;
        if (other.GetComponentInParent<ProjectileTurret>() != null) return;
        Destroy(gameObject);
    }
}
