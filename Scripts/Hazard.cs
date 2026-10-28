using UnityEngine;

/// <summary>Hurts the player on contact (trigger collider + kinematic rigidbody).</summary>
public class Hazard : MonoBehaviour
{
    void OnTriggerEnter(Collider other) { Check(other); }
    void OnTriggerStay(Collider other) { Check(other); }

    void Check(Collider other)
    {
        PlayerController p = other.GetComponentInParent<PlayerController>();
        if (p != null) p.Hit();
    }
}
