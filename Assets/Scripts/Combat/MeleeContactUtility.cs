using UnityEngine;
using UnityEngine.AI;

public static class MeleeContactUtility
{
    public static bool IsWithinArc(Vector3 origin, Vector3 forward, Vector3 target, float arcDegrees)
    {
        Vector3 direction = Vector3.ProjectOnPlane(target - origin, Vector3.up);
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return true;
        }

        Vector3 planarForward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
        float minimumDot = Mathf.Cos(Mathf.Clamp(arcDegrees, 1f, 180f) * 0.5f * Mathf.Deg2Rad);
        return Vector3.Dot(planarForward, direction.normalized) >= minimumDot;
    }

    /// <summary>
    /// Returns true only when the first relevant collider between an attacker
    /// and its target belongs to the target's hierarchy. This prevents melee
    /// damage from landing through scenery, props, or another character.
    /// </summary>
    public static bool HasLineOfSight(
        Transform observer,
        Transform target,
        float eyeHeight,
        float targetAimHeight,
        LayerMask mask,
        Collider[] ownColliders,
        RaycastHit[] hitBuffer)
    {
        if (observer == null || target == null)
        {
            return false;
        }

        Vector3 origin = observer.position + Vector3.up * eyeHeight;
        Vector3 aimPoint = target.position + Vector3.up * targetAimHeight;
        Vector3 toTarget = aimPoint - origin;
        float distance = toTarget.magnitude;
        if (distance <= Mathf.Epsilon)
        {
            return true;
        }

        int hitCount = Physics.RaycastNonAlloc(
            origin,
            toTarget / distance,
            hitBuffer,
            distance,
            mask,
            QueryTriggerInteraction.Ignore);

        Collider closestRelevantHit = null;
        float closestDistance = float.PositiveInfinity;
        for (int index = 0; index < hitCount; index++)
        {
            Collider hitCollider = hitBuffer[index].collider;
            if (hitCollider == null || IsOneOf(ownColliders, hitCollider))
            {
                continue;
            }

            if (hitBuffer[index].distance < closestDistance)
            {
                closestDistance = hitBuffer[index].distance;
                closestRelevantHit = hitCollider;
            }
        }

        if (closestRelevantHit == null)
        {
            return true;
        }

        Transform hitTransform = closestRelevantHit.transform;
        return hitTransform == target || hitTransform.IsChildOf(target) || target.IsChildOf(hitTransform);
    }

    /// <summary>
    /// Calculates a route before handing it to an agent. Partial paths are
    /// deliberately rejected so enemies do not swing at unreachable targets.
    /// </summary>
    public static bool TrySetCompletePath(NavMeshAgent agent, Vector3 destination, NavMeshPath path)
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh || path == null)
        {
            return false;
        }

        if (!agent.CalculatePath(destination, path) || path.status != NavMeshPathStatus.PathComplete)
        {
            agent.ResetPath();
            return false;
        }

        return agent.SetDestination(destination);
    }

    public static bool HasCompletePath(NavMeshAgent agent, Vector3 destination, NavMeshPath path)
    {
        return agent != null
            && agent.enabled
            && agent.isOnNavMesh
            && path != null
            && agent.CalculatePath(destination, path)
            && path.status == NavMeshPathStatus.PathComplete;
    }

    private static bool IsOneOf(Collider[] colliders, Collider candidate)
    {
        if (colliders == null)
        {
            return false;
        }

        for (int index = 0; index < colliders.Length; index++)
        {
            if (colliders[index] == candidate)
            {
                return true;
            }
        }

        return false;
    }
}
