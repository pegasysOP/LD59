using System.Collections;
using DG.Tweening;
using UnityEngine;

public class EndCutsceneAlien : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("Root transform of the alien that will be moved during the end cutscene.")]
    public Transform alien;
    [Tooltip("Animator driving the alien. Leave empty to auto-find in children of 'alien'.")]
    public Animator animator;

    [Header("Animation")]
    public string runStateName = "run";
    [Tooltip("State played while the alien is spawned but not yet charging. Leave empty to skip.")]
    public string idleStateName = "";
    public float animCrossfade = 0.15f;

    [Header("Movement")]
    [Tooltip("Stop this far from the player (metres) along the XZ approach line.")]
    public float approachDistance = 3f;
    [Tooltip("Min distance from player the approach target can sit, even if approachDistance is smaller.")]
    public float approachDistanceFloor = 0.5f;
    public Ease moveEase = Ease.Linear;

    [Header("Grounding")]
    [Tooltip("Max distance to raycast downward to find the floor under the spawn marker.")]
    public float groundRaycastDistance = 10f;
    [Tooltip("Layers considered floor for the spawn raycast.")]
    public LayerMask groundMask = ~0;

    private Tweener runTween;

    public Vector3 SpawnPosition => GetGroundedPosition(transform.position);

    private void Awake()
    {
        if (animator == null && alien != null)
            animator = alien.GetComponentInChildren<Animator>(true);
    }

    public void Activate()
    {
        if (alien == null) return;

        alien.gameObject.SetActive(true);
        alien.position = SpawnPosition;
        if (!string.IsNullOrEmpty(idleStateName))
            PlayAnim(idleStateName);
    }

    public IEnumerator RunTowardPlayer(Transform player, float duration)
    {
        if (alien == null || player == null) yield break;

        Vector3 start = alien.position;
        Vector3 target = ComputeApproachTarget(start, player.position);

        KillTween();
        FaceToward(player.position);
        PlayAnim(runStateName);
        runTween = alien.DOMove(target, duration)
            .SetEase(moveEase)
            .SetUpdate(true)
            .OnUpdate(() => FaceToward(player.position));

        yield return runTween.WaitForCompletion();
        runTween = null;
    }

    private void KillTween()
    {
        if (runTween != null && runTween.IsActive())
            runTween.Kill();
        runTween = null;
    }

    private void PlayAnim(string stateName)
    {
        if (animator == null || string.IsNullOrEmpty(stateName)) return;
        animator.CrossFadeInFixedTime(stateName, animCrossfade);
    }

    private void OnDisable()
    {
        KillTween();
    }

    private void FaceToward(Vector3 worldPos)
    {
        if (alien == null) return;
        Vector3 flat = worldPos - alien.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f) return;
        alien.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);
    }

    private Vector3 ComputeApproachTarget(Vector3 spawnPos, Vector3 playerPos)
    {
        Vector3 toPlayer = playerPos - spawnPos;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;
        float approach = Mathf.Max(approachDistanceFloor, approachDistance);

        if (distance <= approach || distance < 0.0001f)
            return spawnPos;

        Vector3 dir = toPlayer / distance;
        Vector3 target = playerPos - dir * approach;
        target.y = spawnPos.y;
        return target;
    }

    private Vector3 GetGroundedPosition(Vector3 origin)
    {
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundRaycastDistance, groundMask, QueryTriggerInteraction.Ignore))
            return hit.point;
        return origin;
    }

    private bool TryGetGroundedPosition(Vector3 origin, out Vector3 pos)
    {
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundRaycastDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            pos = hit.point;
            return true;
        }
        pos = origin;
        return false;
    }

    private void OnDrawGizmos()
    {
        DrawGizmos(new Color(1f, 0.3f, 0.2f, 0.35f), new Color(1f, 0.3f, 0.2f, 0.9f));
    }

    private void OnDrawGizmosSelected()
    {
        DrawGizmos(new Color(1f, 0.5f, 0.3f, 0.5f), new Color(1f, 0.7f, 0.4f, 1f));
    }

    private void DrawGizmos(Color fill, Color wire)
    {
        bool hit = TryGetGroundedPosition(transform.position, out Vector3 spawn);
        Color f = hit ? fill : new Color(1f, 0.2f, 0.2f, fill.a);
        Color w = hit ? wire : new Color(1f, 0.1f, 0.1f, 1f);

        Gizmos.color = f;
        Gizmos.DrawSphere(spawn, 0.3f);
        Gizmos.color = w;
        Gizmos.DrawWireSphere(spawn, 0.35f);

        Vector3 playerRef;
        if (GameManager.Instance != null && GameManager.Instance.playerController != null)
            playerRef = GameManager.Instance.playerController.transform.position;
        else
            playerRef = spawn + transform.forward * 10f;

        Vector3 approachTarget = ComputeApproachTarget(spawn, playerRef);

        Gizmos.color = new Color(w.r, w.g, w.b, 0.8f);
        Gizmos.DrawLine(spawn, approachTarget);
        Gizmos.DrawWireSphere(approachTarget, 0.25f);
    }
}
