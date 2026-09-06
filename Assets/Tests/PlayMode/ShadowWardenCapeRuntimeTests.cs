using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MainHackathonGame.Tests
{
    public sealed class ShadowWardenCapeRuntimeTests
    {
        private int originalTargetFrameRate;
        private int originalVSyncCount;

        [UnitySetUp]
        public IEnumerator LoadGameplayScene()
        {
            originalTargetFrameRate = Application.targetFrameRate;
            originalVSyncCount = QualitySettings.vSyncCount;
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("TestScene", LoadSceneMode.Single);
            yield return null;
            yield return new WaitForSeconds(0.25f);
        }

        [UnityTearDown]
        public IEnumerator RestoreTiming()
        {
            Time.timeScale = 1f;
            Application.targetFrameRate = originalTargetFrameRate;
            QualitySettings.vSyncCount = originalVSyncCount;
            yield return null;
        }

        [UnityTest]
        public IEnumerator CapeConfigurationIsAttachedGroundedAndMobileSafe()
        {
            GameObject warden = RequiredObject("ShadowWarden");
            Cloth[] clothComponents = warden.GetComponentsInChildren<Cloth>(true);
            Assert.That(clothComponents, Has.Length.EqualTo(1));
            Cloth cloth = clothComponents[0];
            SkinnedMeshRenderer clothRenderer = cloth.GetComponent<SkinnedMeshRenderer>();
            SkinnedMeshRenderer body = FindRenderer(warden, "ShadowWarden_Body");
            Animator animator = warden.GetComponentInChildren<Animator>(true);

            Assert.That(clothRenderer.name, Is.EqualTo("ShadowWarden_ClothLayers"));
            Assert.That(clothRenderer.sharedMaterial, Is.Not.Null);
            Assert.That(clothRenderer.sharedMaterial.name, Does.StartWith("MAT_ShadowWarden_Cloth"));
            Assert.That(body.GetComponent<Cloth>(), Is.Null);
            Assert.That(clothRenderer.enabled, Is.True);
            Assert.That(clothRenderer.updateWhenOffscreen, Is.True);
            Assert.That(clothRenderer.sharedMesh.vertexCount, Is.EqualTo(18868));
            Assert.That(cloth.vertices, Has.Length.EqualTo(5594));
            Assert.That(cloth.coefficients, Has.Length.EqualTo(5594));
            int movableParticles = cloth.coefficients.Count(coefficient => coefficient.maxDistance > 0.0001f);
            Assert.That(movableParticles, Is.InRange(2500, 2950), "Only the broad rear cape should consume mobile solver work.");
            Assert.That(cloth.useGravity, Is.True);
            Assert.That(cloth.useTethers, Is.True);
            Assert.That(cloth.enableContinuousCollision, Is.True);
            Assert.That(cloth.useVirtualParticles, Is.EqualTo(0f).Within(0.001f));
            Assert.That(cloth.clothSolverFrequency, Is.LessThanOrEqualTo(60.01f));
            Assert.That(cloth.stiffnessFrequency, Is.EqualTo(10f).Within(0.01f));
            Assert.That(cloth.sphereColliders, Has.Length.EqualTo(3));
            Assert.That(animator.transform, Is.Not.EqualTo(warden.transform), "Visual bob must not write the navigation root.");
            Assert.That(animator.enabled, Is.False, "Unsafe imported takes must remain disabled; procedural source-rig motion is used.");
            Assert.That(animator.runtimeAnimatorController, Is.Not.Null);
            Assert.That(animator.GetBoneTransform(HumanBodyBones.Neck).Find("HoodInteriorLiner"), Is.Not.Null);
            Assert.That(animator.GetBoneTransform(HumanBodyBones.Hips).Find("ShadowWarden_BackOpeningRepair"), Is.Not.Null);
            Assert.That(warden.GetComponent("ShadowWardenNativeWalk"), Is.Not.Null);
            Assert.That(cloth.GetComponent("ShadowWardenCapeRuntime"), Is.Not.Null);

            SphereCollider[] proxies = warden.GetComponentsInChildren<SphereCollider>(true)
                .Where(collider => collider.name.StartsWith("CapeCollision_", StringComparison.Ordinal))
                .ToArray();
            Assert.That(proxies, Has.Length.EqualTo(6));
            Assert.That(proxies.All(proxy => proxy.enabled && proxy.isTrigger && proxy.gameObject.layer == 2), Is.True);
            AssertCollisionProxy(animator, proxies, "CapeCollision_UpperChest", HumanBodyBones.UpperChest, 0.19f);
            AssertCollisionProxy(animator, proxies, "CapeCollision_Hips", HumanBodyBones.Hips, 0.25f);
            AssertCollisionProxy(animator, proxies, "CapeCollision_LeftUpperLeg", HumanBodyBones.LeftUpperLeg, 0.15f);
            AssertCollisionProxy(animator, proxies, "CapeCollision_LeftLowerLeg", HumanBodyBones.LeftLowerLeg, 0.11f);
            AssertCollisionProxy(animator, proxies, "CapeCollision_RightUpperLeg", HumanBodyBones.RightUpperLeg, 0.15f);
            AssertCollisionProxy(animator, proxies, "CapeCollision_RightLowerLeg", HumanBodyBones.RightLowerLeg, 0.11f);
            SphereCollider[] wiredProxies = cloth.sphereColliders
                .SelectMany(pair => new[] { pair.first, pair.second })
                .ToArray();
            Assert.That(wiredProxies.All(proxy => proxy != null), Is.True);
            Assert.That(wiredProxies.Distinct().Count(), Is.EqualTo(6));
            Assert.That(wiredProxies.All(proxy => proxies.Contains(proxy)), Is.True);

            Component playerAttack = RequiredComponent(RequiredObject("GameRoot/PlayerRoot"), "PlayerSwordAttack");
            LayerMask combatMask = (LayerMask)ReadPrivateField(playerAttack, "hittableLayers");
            Assert.That((combatMask.value & (1 << 2)) == 0, Is.True,
                "Cloth-only Ignore Raycast proxies must never consume the player's combat query buffer.");

            yield return new WaitForSeconds(0.35f);
            AssertFootNearGround(warden, animator.GetBoneTransform(HumanBodyBones.LeftFoot), "left foot");
            AssertFootNearGround(warden, animator.GetBoneTransform(HumanBodyBones.RightFoot), "right foot");
            AssertFiniteAndBounded(cloth, 3.5f);
            AssertRendererWorldBounds(warden, cloth, 4f);
        }

        [UnityTest]
        public IEnumerator CapeSurvivesReversalsTurnsTimingPauseCullingTeleportAndEnableCycles()
        {
            GameObject warden = RequiredObject("ShadowWarden");
            DisableAutonomy(warden);
            Cloth cloth = warden.GetComponentInChildren<Cloth>(true);
            Component runtime = RequiredComponent(cloth.gameObject, "ShadowWardenCapeRuntime");
            SkinnedMeshRenderer renderer = cloth.GetComponent<SkinnedMeshRenderer>();
            InvokePublic(runtime, "ResetSimulation");
            yield return new WaitForSeconds(0.25f);

            Vector3[] initial = cloth.vertices;
            Vector3 origin = warden.transform.position;
            Quaternion originRotation = warden.transform.rotation;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 15;
            float elapsed = 0f;
            float greatestMotion = 0f;
            while (elapsed < 3.2f)
            {
                float realDelta = Mathf.Max(Time.unscaledDeltaTime, 0.001f);
                elapsed += realDelta;
                Time.timeScale = elapsed < 0.8f ? 0.35f : elapsed < 1.6f ? 1.45f : 1f;
                float reversal = Mathf.FloorToInt(elapsed / 0.16f) % 2 == 0 ? 1f : -1f;
                float burst = Mathf.PingPong(elapsed * 3.8f, 1f);
                Vector3 planarOffset = new Vector3(
                    Mathf.Sin(elapsed * 4.5f) * 0.42f,
                    0f,
                    reversal * burst * 0.48f);
                warden.transform.position = origin + planarOffset;
                warden.transform.rotation = originRotation * Quaternion.Euler(0f, elapsed * 225f, 0f);
                renderer.forceRenderingOff = elapsed > 1.0f && elapsed < 1.7f;
                yield return null;

                Vector3[] current = cloth.vertices;
                greatestMotion = Mathf.Max(greatestMotion, MaximumDisplacement(initial, current));
                AssertFiniteAndBounded(current, 3.5f);
                AssertRendererWorldBounds(warden, cloth, 4f);
            }

            renderer.forceRenderingOff = false;
            Assert.That(greatestMotion, Is.GreaterThan(0.005f), "Movable cape particles never responded to stress motion.");

            Time.timeScale = 0f;
            InvokePublic(runtime, "ApplyImpact", Vector3.right, 1f);
            yield return new WaitForSecondsRealtime(0.22f);
            Assert.That(cloth.externalAcceleration, Is.EqualTo(Vector3.zero), "Pause must not leave an acceleration spike queued on Cloth.");
            Time.timeScale = 1f;

            int resetsBeforeTeleport = ReadInt(runtime, "TransformResetCount");
            warden.transform.SetPositionAndRotation(origin + Vector3.right * 5.5f, originRotation * Quaternion.Euler(0f, 180f, 0f));
            yield return null;
            Assert.That(ReadInt(runtime, "TransformResetCount"), Is.GreaterThan(resetsBeforeTeleport), "A 5m/180-degree discontinuity did not clear transform motion.");
            AssertFiniteAndBounded(cloth, 3.5f);
            AssertRendererWorldBounds(warden, cloth, 4f);

            int resetsBeforeEnableCycle = ReadInt(runtime, "TransformResetCount");
            cloth.gameObject.SetActive(false);
            yield return null;
            cloth.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.12f);
            Assert.That(ReadInt(runtime, "TransformResetCount"), Is.GreaterThan(resetsBeforeEnableCycle));
            Assert.That(cloth.coefficients, Has.Length.EqualTo(5594));
            AssertFiniteAndBounded(cloth, 3.5f);

            // Unity Cloth only consumes its explicitly wired body proxies, but
            // nearby world geometry must not destabilize it. Exercise the
            // boundary case beside a real collider and close to the ground.
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "CapeNearWallStressFixture";
            wall.transform.position = warden.transform.position + warden.transform.forward * 0.55f + Vector3.up * 1.05f;
            wall.transform.localScale = new Vector3(3f, 2.1f, 0.12f);
            try
            {
                Collider wallCollider = wall.GetComponent<Collider>();
                Assert.That(Vector3.Distance(wallCollider.ClosestPoint(renderer.bounds.center), renderer.bounds.center),
                    Is.LessThan(1.25f), "Wall fixture was not actually near the garment.");
                Vector3 nearWallOrigin = warden.transform.position;
                for (int frame = 0; frame < 48; frame++)
                {
                    float phase = frame / 47f * Mathf.PI * 4f;
                    warden.transform.position = nearWallOrigin + warden.transform.forward * (Mathf.Sin(phase) * 0.24f);
                    yield return null;
                    AssertFiniteAndBounded(cloth, 3.5f);
                    AssertRendererWorldBounds(warden, cloth, 4f);
                    AssertCapeNearGround(warden, renderer, 0.35f);
                }
            }
            finally
            {
                UnityEngine.Object.Destroy(wall);
            }
        }

        [UnityTest]
        public IEnumerator CapeSurvivesAttackAndImpactSpamThenDeathWithMomentum()
        {
            GameObject warden = RequiredObject("ShadowWarden");
            DisableAutonomy(warden);
            Cloth cloth = warden.GetComponentInChildren<Cloth>(true);
            Component runtime = RequiredComponent(cloth.gameObject, "ShadowWardenCapeRuntime");
            Component nativeMotion = RequiredComponent(warden, "ShadowWardenNativeWalk");
            Component ai = RequiredComponent(warden, "ShadowWardenAI");
            Component health = RequiredComponent(warden, "EnemyHealth");
            Component hitReaction = RequiredComponent(warden, "CombatHitReaction");

            int attacksBefore = ReadInt(nativeMotion, "AttackRequestCount");
            InvokePrivate(ai, "BeginAttack");
            Assert.That(ReadInt(nativeMotion, "AttackRequestCount"), Is.EqualTo(attacksBefore + 1), "AI attack did not reach native presentation motion.");
            for (int index = 0; index < 5; index++)
            {
                InvokePublic(nativeMotion, "BeginHeavyStrike", 0.55f);
                InvokePublic(hitReaction, "PlayReaction", index % 2 == 0 ? Vector3.right : Vector3.left);
                InvokePublic(runtime, "ApplyImpact", index % 2 == 0 ? Vector3.forward : Vector3.back, 0.7f);
                yield return new WaitForSeconds(0.045f);
                AssertFiniteAndBounded(cloth, 3.5f);
            }

            int impactsBeforeDamage = ReadInt(runtime, "ImpactCount");
            cloth.useGravity = false;
            InvokePublic(runtime, "ResetSimulation");
            yield return new WaitForSeconds(0.18f);
            Vector3[] beforeDamage = cloth.vertices;
            Assert.That((bool)InvokePublic(health, "TakeDamage", 1f, warden.transform.position + Vector3.up, Vector3.right), Is.True);
            yield return null;
            yield return null;
            yield return null;
            float damageDrivenMotion = MaximumDisplacement(beforeDamage, cloth.vertices);
            Assert.That(damageDrivenMotion, Is.GreaterThan(0.0005f), "Damage reached the lifecycle component but did not visibly move cape particles.");
            Assert.That((bool)InvokePublic(health, "TakeDamage", 1f, warden.transform.position + Vector3.up, Vector3.left), Is.True);
            Assert.That(ReadInt(runtime, "ImpactCount"), Is.EqualTo(impactsBeforeDamage + 2));
            float peakImpact = ReadFloat(runtime, "CurrentImpactStrength");
            Assert.That(peakImpact, Is.GreaterThan(0.5f));
            yield return new WaitForSeconds(0.65f);
            Assert.That(ReadFloat(runtime, "CurrentImpactStrength"), Is.LessThan(peakImpact * 0.25f), "Impact acceleration did not decay.");

            InvokePublic(nativeMotion, "BeginHeavyStrike", 1f);
            InvokePublic(runtime, "ApplyImpact", Vector3.forward + Vector3.right, 1.4f);
            yield return new WaitForSeconds(0.08f);
            Vector3[] beforeDeath = cloth.vertices;
            Assert.That((bool)InvokePublic(health, "TakeDamage", 1000f, warden.transform.position + Vector3.up, Vector3.forward), Is.True);
            Assert.That(ReadBool(health, "IsDead"), Is.True);
            Assert.That(ReadBool(runtime, "DeathHandled"), Is.True, "Cape did not receive the death lifecycle event.");
            Assert.That(ReadBool(nativeMotion, "IsDying"), Is.True, "Native presentation did not start its death fall.");
            Assert.That(cloth.enabled, Is.True, "Cape simulation must stay live for the death reaction.");
            Assert.That(cloth.GetComponent<SkinnedMeshRenderer>().enabled, Is.True, "The garment must remain visible during the death delay.");
            yield return new WaitForSeconds(0.25f);
            float deathMotion = MaximumDisplacement(beforeDeath, cloth.vertices);
            Assert.That(deathMotion, Is.InRange(0.0005f, 0.75f), "Cape either froze/snapped or detached during death.");
            Animator deathAnimator = warden.GetComponentInChildren<Animator>(true);
            Assert.That(Quaternion.Angle(deathAnimator.transform.localRotation, Quaternion.identity), Is.GreaterThan(5f),
                "Death fall did not visibly rotate the Warden's visual root.");
            SphereCollider[] deathProxies = warden.GetComponentsInChildren<SphereCollider>(true)
                .Where(collider => collider.name.StartsWith("CapeCollision_", StringComparison.Ordinal))
                .ToArray();
            Assert.That(deathProxies.All(proxy => proxy.enabled), Is.True, "Cloth-only body proxies were not restored after combat hitboxes shut down.");
            AssertFiniteAndBounded(cloth, 3.5f);
            AssertRendererWorldBounds(warden, cloth, 4f);
        }

        [UnityTest]
        public IEnumerator CapeHasNoLongDurationDriftOrNumericalExplosion()
        {
            GameObject warden = RequiredObject("ShadowWarden");
            DisableAutonomy(warden);
            Cloth cloth = warden.GetComponentInChildren<Cloth>(true);
            Component runtime = RequiredComponent(cloth.gameObject, "ShadowWardenCapeRuntime");
            Vector3 origin = warden.transform.position;
            Quaternion originRotation = warden.transform.rotation;

            for (int frame = 0; frame < 360; frame++)
            {
                float phase = frame / 60f;
                float reversal = (frame / 12) % 2 == 0 ? 1f : -1f;
                warden.transform.position = origin + new Vector3(
                    Mathf.Sin(phase * 2.7f) * 0.65f,
                    0f,
                    Mathf.Cos(phase * 3.1f) * 0.45f * reversal);
                warden.transform.rotation = originRotation * Quaternion.Euler(0f, phase * 120f, 0f);
                if (frame % 45 == 0)
                {
                    InvokePublic(runtime, "ApplyImpact", new Vector3(reversal, 0.15f, -reversal), 0.75f);
                }

                yield return null;
                if (frame % 12 == 0)
                {
                    AssertFiniteAndBounded(cloth, 3.5f);
                    AssertRendererWorldBounds(warden, cloth, 4f);
                }
            }

            Assert.That(ReadInt(runtime, "TransformResetCount"), Is.LessThan(4), "Ordinary fast motion was misclassified as repeated teleports.");
            AssertFiniteAndBounded(cloth, 3.5f);
        }

        [UnityTest]
        public IEnumerator CapePersistsAcrossAnExplicitSceneReload()
        {
            AssertCapeIdentity(RequiredObject("ShadowWarden"));
            yield return SceneManager.LoadSceneAsync("TestScene", LoadSceneMode.Single);
            yield return null;
            yield return new WaitForSeconds(0.25f);
            AssertCapeIdentity(RequiredObject("ShadowWarden"));
        }

        private static void AssertCapeIdentity(GameObject warden)
        {
            Cloth[] cloth = warden.GetComponentsInChildren<Cloth>(true);
            Assert.That(cloth, Has.Length.EqualTo(1));
            Assert.That(cloth[0].name, Is.EqualTo("ShadowWarden_ClothLayers"));
            Assert.That(cloth[0].coefficients, Has.Length.EqualTo(5594));
            Assert.That(cloth[0].GetComponent("ShadowWardenCapeRuntime"), Is.Not.Null);
        }

        private static void DisableAutonomy(GameObject warden)
        {
            Component ai = warden.GetComponent("ShadowWardenAI");
            if (ai is Behaviour aiBehaviour)
            {
                aiBehaviour.enabled = false;
            }

            NavMeshAgent agent = warden.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled)
            {
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                }

                agent.enabled = false;
            }
        }

        private static SkinnedMeshRenderer FindRenderer(GameObject root, string name)
        {
            SkinnedMeshRenderer renderer = root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .FirstOrDefault(candidate => candidate.name == name);
            Assert.That(renderer, Is.Not.Null, $"Renderer '{name}' is missing.");
            return renderer;
        }

        private static float MaximumDisplacement(Vector3[] left, Vector3[] right)
        {
            Assert.That(right.Length, Is.EqualTo(left.Length));
            float maximum = 0f;
            for (int index = 0; index < left.Length; index += 17)
            {
                maximum = Mathf.Max(maximum, (right[index] - left[index]).magnitude);
            }

            return maximum;
        }

        private static void AssertCollisionProxy(
            Animator animator,
            SphereCollider[] proxies,
            string name,
            HumanBodyBones expectedBone,
            float expectedRadius)
        {
            SphereCollider proxy = proxies.Single(candidate => candidate.name == name);
            Assert.That(proxy.transform.parent, Is.EqualTo(animator.GetBoneTransform(expectedBone)), $"{name} is on the wrong bone.");
            Vector3 scale = proxy.transform.lossyScale;
            float worldRadius = proxy.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            Assert.That(worldRadius, Is.EqualTo(expectedRadius).Within(0.015f), $"{name} has an invalid world radius.");
        }

        private static void AssertFiniteAndBounded(Cloth cloth, float maximumSize)
        {
            AssertFiniteAndBounded(cloth.vertices, maximumSize);
        }

        private static void AssertFiniteAndBounded(Vector3[] vertices, float maximumSize)
        {
            Assert.That(vertices, Is.Not.Empty);
            Bounds bounds = new Bounds(vertices[0], Vector3.zero);
            foreach (Vector3 vertex in vertices)
            {
                Assert.That(float.IsFinite(vertex.x) && float.IsFinite(vertex.y) && float.IsFinite(vertex.z), Is.True,
                    $"Cape produced a non-finite vertex: {vertex}.");
                bounds.Encapsulate(vertex);
            }

            Assert.That(bounds.size.magnitude, Is.LessThan(maximumSize),
                $"Cape particle field expanded beyond its safe envelope: {bounds.size}.");
        }

        private static void AssertRendererWorldBounds(GameObject root, Cloth cloth, float maximumSize)
        {
            Bounds bounds = cloth.GetComponent<SkinnedMeshRenderer>().bounds;
            Assert.That(float.IsFinite(bounds.center.x) && float.IsFinite(bounds.center.y) && float.IsFinite(bounds.center.z), Is.True);
            Assert.That(float.IsFinite(bounds.size.x) && float.IsFinite(bounds.size.y) && float.IsFinite(bounds.size.z), Is.True);
            Assert.That(bounds.size.magnitude, Is.LessThan(maximumSize), $"Visible cape world bounds exploded: {bounds}.");
            Assert.That(Vector3.Distance(bounds.center, root.transform.position), Is.LessThan(3f), $"Visible cape detached from Warden: {bounds.center}.");
        }

        private static void AssertCapeNearGround(GameObject root, SkinnedMeshRenderer renderer, float tolerance)
        {
            Assert.That(renderer.bounds.min.y, Is.GreaterThanOrEqualTo(root.transform.position.y - tolerance),
                $"Cape dropped materially below the Warden's grounded root plane ({renderer.bounds.min.y:0.000}m).");
        }

        private static void AssertFootNearGround(GameObject root, Transform foot, string label)
        {
            Assert.That(foot, Is.Not.Null);
            RaycastHit[] hits = Physics.RaycastAll(foot.position + Vector3.up * 0.2f, Vector3.down, 10f, ~0, QueryTriggerInteraction.Ignore);
            float bestDistance = float.PositiveInfinity;
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.transform == root.transform || hit.collider.transform.IsChildOf(root.transform))
                {
                    continue;
                }

                bestDistance = Mathf.Min(bestDistance, foot.position.y - hit.point.y);
            }

            Assert.That(bestDistance, Is.InRange(-0.08f, 0.28f), $"{label} is not grounded ({bestDistance:0.000}m).");
        }

        private static GameObject RequiredObject(string name)
        {
            GameObject result = GameObject.Find(name);
            Assert.That(result, Is.Not.Null, $"Required object '{name}' is missing.");
            return result;
        }

        private static Component RequiredComponent(GameObject gameObject, string componentName)
        {
            Component component = gameObject.GetComponent(componentName);
            if (component == null)
            {
                component = gameObject.GetComponentsInChildren<Component>(true)
                    .FirstOrDefault(candidate => candidate != null && candidate.GetType().Name == componentName);
            }

            Assert.That(component, Is.Not.Null, $"{gameObject.name} is missing {componentName}.");
            return component;
        }

        private static int ReadInt(Component component, string propertyName)
        {
            return (int)ReadProperty(component, propertyName);
        }

        private static float ReadFloat(Component component, string propertyName)
        {
            return (float)ReadProperty(component, propertyName);
        }

        private static bool ReadBool(Component component, string propertyName)
        {
            return (bool)ReadProperty(component, propertyName);
        }

        private static object ReadProperty(Component component, string propertyName)
        {
            PropertyInfo property = component.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, $"{component.GetType().Name}.{propertyName} is missing.");
            return property.GetValue(component);
        }

        private static object ReadPrivateField(Component component, string fieldName)
        {
            FieldInfo field = component.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"{component.GetType().Name}.{fieldName} is missing.");
            return field.GetValue(component);
        }

        private static object InvokePublic(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(candidate => candidate.Name == methodName && candidate.GetParameters().Length == arguments.Length);
            Assert.That(method, Is.Not.Null, $"{target.GetType().Name}.{methodName} is missing.");
            return method.Invoke(target, arguments);
        }

        private static object InvokePrivate(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"{target.GetType().Name}.{methodName} is missing.");
            return method.Invoke(target, arguments);
        }
    }
}
