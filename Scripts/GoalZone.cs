using UnityEngine;

/// <summary>Level exit. Spins slowly and completes the level when the player enters.</summary>
public class GoalZone : MonoBehaviour
{
    public float spinSpeed = 60f;
    bool done;

    void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (done) return;
        if (other.GetComponentInParent<PlayerController>() == null) return;
        done = true;
        if (GameHUD.Instance != null) GameHUD.Instance.ShowLevelComplete();
    }
}
