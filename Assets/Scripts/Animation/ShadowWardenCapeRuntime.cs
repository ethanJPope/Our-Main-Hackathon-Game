using System.Collections;
using UnityEngine;

/// <summary>
/// Keeps the Warden garment stable across teleports, enable cycles, pauses and
/// combat impacts. Ordinary movement remains entirely driven by Unity Cloth.
/// </summary>
[DefaultExecutionOrder(300)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Cloth))]
public sealed class ShadowWardenCapeRuntime : MonoBehaviour
{
    [SerializeField] private Cloth cape;
    [SerializeField] private Transform motionRoot;
    [SerializeField, Min(0.25f)] private float teleportDistance = 2.25f;
    [SerializeField, Range(10f, 180f)] private float teleportAngle = 75f;
    [SerializeField, Min(0f)] private float impactAcceleration = 6f;
    [SerializeField, Min(0.01f)] private float impactDecayPerSecond = 18f;

    private EnemyHealth health;
    private Vector3 previousRootPosition;
    private Quaternion previousRootRotation;
    private Vector3 currentImpactAcceleration;
    private bool subscribed;
    private Coroutine restoreDeathCollidersRoutine;

    public int TransformResetCount { get; private set; }
    public int ImpactCount { get; private set; }
    public float CurrentImpactStrength => currentImpactAcceleration.magnitude;
    public bool DeathHandled { get; private set; }

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        CacheReferences();
        Subscribe();
        ResetSimulation();
    }

    private void OnDisable()
    {
        Unsubscribe();
        if (restoreDeathCollidersRoutine != null)
        {
            StopCoroutine(restoreDeathCollidersRoutine);
            restoreDeathCollidersRoutine = null;
        }

        currentImpactAcceleration = Vector3.zero;
        if (cape != null)
        {
            cape.externalAcceleration = Vector3.zero;
        }
    }

    private void LateUpdate()
    {
        if (cape == null || motionRoot == null)
        {
            return;
        }

        float distanceSquared = (motionRoot.position - previousRootPosition).sqrMagnitude;
        float angle = Quaternion.Angle(previousRootRotation, motionRoot.rotation);
        if (distanceSquared >= teleportDistance * teleportDistance || angle >= teleportAngle)
        {
            ResetSimulation();
        }
        else
        {
            previousRootPosition = motionRoot.position;
            previousRootRotation = motionRoot.rotation;
        }

        currentImpactAcceleration = Vector3.MoveTowards(
            currentImpactAcceleration,
            Vector3.zero,
            impactDecayPerSecond * Time.unscaledDeltaTime);
        cape.externalAcceleration = Time.timeScale > 0f ? currentImpactAcceleration : Vector3.zero;
    }

    public void ResetSimulation()
    {
        if (cape == null || motionRoot == null)
        {
            return;
        }

        cape.ClearTransformMotion();
        cape.externalAcceleration = Vector3.zero;
        currentImpactAcceleration = Vector3.zero;
        previousRootPosition = motionRoot.position;
        previousRootRotation = motionRoot.rotation;
        TransformResetCount++;
    }

    public void ApplyImpact(Vector3 worldDirection, float strength = 1f)
    {
        Vector3 direction = worldDirection.normalized;
        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = motionRoot != null ? -motionRoot.forward : Vector3.back;
        }

        float clampedStrength = Mathf.Clamp(strength, 0.25f, 2f);
        currentImpactAcceleration = direction * (impactAcceleration * clampedStrength)
            + Vector3.up * (0.8f * clampedStrength);
        ImpactCount++;
    }

    private void CacheReferences()
    {
        if (cape == null)
        {
            cape = GetComponent<Cloth>();
        }

        if (health == null)
        {
            health = GetComponentInParent<EnemyHealth>();
        }

        if (motionRoot == null)
        {
            motionRoot = health != null ? health.transform : transform.root;
        }
    }

    private void Subscribe()
    {
        if (health == null || subscribed)
        {
            return;
        }

        health.Damaged += OnDamaged;
        health.Died += OnDied;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (health == null || !subscribed)
        {
            return;
        }

        health.Damaged -= OnDamaged;
        health.Died -= OnDied;
        subscribed = false;
    }

    private void OnDamaged(EnemyHealth _, float amount, Vector3 hitPoint, Vector3 hitDirection)
    {
        ApplyImpact(hitDirection, Mathf.Clamp(amount / 10f, 0.5f, 1.5f));
    }

    private void OnDied(EnemyHealth _)
    {
        DeathHandled = true;
        if (cape != null)
        {
            cape.ClearTransformMotion();
            cape.externalAcceleration = Vector3.zero;
            currentImpactAcceleration = Vector3.zero;
            // EnemyHealth disables child colliders immediately after its death
            // event. Restore only the proxies explicitly wired to this Cloth on
            // the next frame so the garment follows the short death reaction
            // without leaving combat hitboxes active.
            restoreDeathCollidersRoutine = StartCoroutine(RestoreDeathCollisionProxies());
        }
    }

    private IEnumerator RestoreDeathCollisionProxies()
    {
        yield return null;

        if (cape != null)
        {
            ClothSphereColliderPair[] pairs = cape.sphereColliders;
            if (pairs != null)
            {
                foreach (ClothSphereColliderPair pair in pairs)
                {
                    if (pair.first != null)
                    {
                        pair.first.enabled = true;
                    }

                    if (pair.second != null)
                    {
                        pair.second.enabled = true;
                    }
                }
            }

            cape.ClearTransformMotion();
        }

        restoreDeathCollidersRoutine = null;
    }
}
