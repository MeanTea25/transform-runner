using UnityEngine;

/// <summary>
/// Waypoint patrol. loop = false: ping-pong A -> B -> A.  loop = true: perimeter A -> B -> C -> D -> A.
/// The hazard turns to face its direction of travel.
/// </summary>
public class PatrolHazard : MonoBehaviour
{
    public Vector3[] points;
    public float speed = 3f;
    public float waitTime = 0.3f;
    public float turnSpeed = 360f;
    public bool loop = false;

    int index = 1;
    int step = 1;
    float wait;

    void Start()
    {
        if (points != null && points.Length > 0) transform.position = points[0];
        if (points != null && points.Length > 1) index = 1;
    }

    void Update()
    {
        if (points == null || points.Length < 2) return;
        if (wait > 0f) { wait -= Time.deltaTime; return; }

        Vector3 target = points[index];
        Vector3 toTarget = target - transform.position;

        if (toTarget.magnitude < 0.05f)
        {
            Advance();
            wait = waitTime;
            return;
        }

        // Face direction of travel (yaw only).
        Vector3 flat = new Vector3(toTarget.x, 0f, toTarget.z);
        if (flat.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(flat);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * Time.deltaTime);
        }

        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
    }

    void Advance()
    {
        if (loop)
        {
            index = (index + 1) % points.Length;
            return;
        }
        if (index + step >= points.Length || index + step < 0) step = -step;
        index += step;
    }

    void OnDrawGizmosSelected()
    {
        if (points == null) return;
        Gizmos.color = Color.red;
        for (int i = 0; i < points.Length; i++)
        {
            Gizmos.DrawSphere(points[i], 0.2f);
            if (i + 1 < points.Length) Gizmos.DrawLine(points[i], points[i + 1]);
            else if (loop) Gizmos.DrawLine(points[i], points[0]);
        }
    }
}
