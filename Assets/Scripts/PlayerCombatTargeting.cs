using UnityEngine;

/// <summary>Explicit lock-on for strafing; never silently switches targets.</summary>
[DisallowMultipleComponent]
public sealed class PlayerCombatTargeting : MonoBehaviour
{
    [SerializeField] private Camera viewCamera;
    [SerializeField] private Transform cameraTarget;
    [SerializeField, Min(1f)] private float acquisitionRange = 14f;
    [SerializeField, Min(1f)] private float releaseRange = 18f;
    [SerializeField, Min(0f)] private float occlusionGrace = 0.6f;
    private readonly Collider[] candidates = new Collider[96];
    private readonly RaycastHit[] sightHits = new RaycastHit[32];
    private PlayerVitals vitals;
    private Vector3 originalCameraLocalPosition;
    private float unseenSince = -1f;
    private Vector3 pointerDirection;
    public EnemyHealth Target { get; private set; }
    public bool IsLocked => Target != null && !Target.IsDead && Target.isActiveAndEnabled;
    public Vector3 FacingDirection => IsLocked ? Vector3.ProjectOnPlane(Target.transform.position - transform.position, Vector3.up).normalized : Vector3.zero;

    private void Awake()
    {
        vitals = GetComponent<PlayerVitals>();
        if (viewCamera == null) viewCamera = Camera.main;
        if (cameraTarget != null) originalCameraLocalPosition = cameraTarget.localPosition;
    }
    public void ToggleLock()
    {
        if (!isActiveAndEnabled || Time.timeScale <= 0f || vitals == null || vitals.IsDead) return;
        if (IsLocked) { Release(); return; }
        float bestScore = float.PositiveInfinity;
        EnemyHealth best = null;
        int count = Physics.OverlapSphereNonAlloc(transform.position, acquisitionRange, candidates, ~(1 << 2), QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            var enemy = candidates[i].GetComponentInParent<EnemyHealth>();
            if (enemy == null || enemy.IsDead || !enemy.isActiveAndEnabled || !HasSight(enemy)) continue;
            Vector3 screen = viewCamera != null ? viewCamera.WorldToViewportPoint(enemy.transform.position + Vector3.up) : new Vector3(0.5f, 0.5f, 1f);
            if (screen.z <= 0f || screen.x < 0f || screen.x > 1f || screen.y < 0f || screen.y > 1f) continue;
            float score = Vector2.Distance(new Vector2(screen.x, screen.y), new Vector2(0.5f, 0.5f)) * 20f + Vector3.Distance(transform.position, enemy.transform.position);
            if (score < bestScore) { bestScore = score; best = enemy; }
        }
        Target = best;
        unseenSince = -1f;
    }
    public void Release() { Target = null; unseenSince = -1f; }
    public void ClearPointerAim() => pointerDirection = Vector3.zero;
    public void AimAtPointer(Ray ray)
    {
        if (IsLocked) return;
        Plane plane = new Plane(Vector3.up, transform.position);
        if (plane.Raycast(ray, out float distance))
            pointerDirection = Vector3.ProjectOnPlane(ray.GetPoint(distance) - transform.position, Vector3.up).normalized;
    }
    public void FaceForAttack()
    {
        Vector3 direction = IsLocked ? FacingDirection : pointerDirection;
        if (direction.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(direction);
        pointerDirection = Vector3.zero;
    }
    private void Update()
    {
        if (vitals == null || vitals.IsDead) { Release(); return; }
        if (!IsLocked) { Release(); return; }
        if (Vector3.Distance(transform.position, Target.transform.position) > releaseRange) { Release(); return; }
        if (HasSight(Target)) unseenSince = -1f;
        else if (unseenSince < 0f) unseenSince = Time.time;
        else if (Time.time - unseenSince > occlusionGrace) Release();
    }
    private void LateUpdate()
    {
        if (cameraTarget == null) return;
        Vector3 desired = originalCameraLocalPosition;
        if (IsLocked)
        {
            Vector3 offset = Vector3.ClampMagnitude((Target.transform.position - transform.position) * 0.35f, 3f);
            desired += cameraTarget.parent != null ? cameraTarget.parent.InverseTransformVector(offset) : offset;
        }
        cameraTarget.localPosition = Vector3.Lerp(cameraTarget.localPosition, desired, 1f - Mathf.Exp(-6f * Time.deltaTime));
    }
    private bool HasSight(EnemyHealth enemy)
    {
        Vector3 origin = transform.position + Vector3.up;
        Vector3 delta = enemy.transform.position + Vector3.up - origin;
        int count = Physics.RaycastNonAlloc(origin, delta.normalized, sightHits, delta.magnitude, ~(1 << 2), QueryTriggerInteraction.Ignore);
        if (count == sightHits.Length) return false;
        for (int i = 0; i < count; i++)
        {
            Transform hit = sightHits[i].transform;
            if (!hit.IsChildOf(transform) && !hit.IsChildOf(enemy.transform)) return false;
        }
        return true;
    }
    private void OnDisable()
    {
        Release();
        pointerDirection = Vector3.zero;
        if (cameraTarget != null) cameraTarget.localPosition = originalCameraLocalPosition;
    }
}
