using UnityEngine;

/// <summary>Constant rotation about an axis (spinning blades, menu showcase, etc).</summary>
public class Spinner : MonoBehaviour
{
    public Vector3 axis = Vector3.up;
    public float degreesPerSecond = 90f;

    void Update()
    {
        transform.Rotate(axis, degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
