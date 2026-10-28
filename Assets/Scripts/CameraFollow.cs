using UnityEngine;

/// <summary>Third-person chase camera. Q / E or right-mouse drag orbits it around the player.</summary>
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float distance = 8f;
    public float height = 1.4f;
    public float pitch = 24f;
    public float smooth = 10f;
    public float keyTurnSpeed = 90f;
    public float mouseSensitivity = 3f;
    [HideInInspector] public float yaw;

    void Start()
    {
        if (target != null)
        {
            transform.position = Desired();
            transform.rotation = Quaternion.LookRotation(Focus() - transform.position);
        }
    }

    float ScaleFactor()
    {
        PlayerController p = PlayerController.Instance;
        return p != null ? p.ModelScale.x : 1f;
    }

    Vector3 Focus()
    {
        return target.position + Vector3.up * height * ScaleFactor();
    }

    Vector3 Desired()
    {
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        float d = distance * Mathf.Lerp(1f, ScaleFactor(), 0.7f);
        return Focus() - rot * Vector3.forward * d;
    }

    void LateUpdate()
    {
        if (target == null || Time.timeScale == 0f) return;

        if (Input.GetKey(KeyCode.Q)) yaw -= keyTurnSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.E)) yaw += keyTurnSpeed * Time.deltaTime;
        if (Input.GetMouseButton(1)) yaw += Input.GetAxis("Mouse X") * mouseSensitivity;

        float k = 1f - Mathf.Exp(-smooth * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, Desired(), k);
        transform.rotation = Quaternion.LookRotation(Focus() - transform.position);
    }
}
