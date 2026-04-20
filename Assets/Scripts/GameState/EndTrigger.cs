using UnityEngine;

public class EndTrigger : MonoBehaviour
{
    private bool hasPlayed;

    private void OnTriggerEnter(Collider other)
    {
        if (hasPlayed) return;
        if (!other.CompareTag("Player")) return;

        hasPlayed = true;

        if (CutsceneManager.Instance != null)
            CutsceneManager.Instance.PlayCutscene(CutsceneManager.CutsceneType.EscapePod);
    }
}
