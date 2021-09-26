using UnityEngine;

/// <summary>
/// End of level screen trigger
/// </summary>
public class LevelEndTrigger : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.SendMessage("EndReached");
        }
    }
}
