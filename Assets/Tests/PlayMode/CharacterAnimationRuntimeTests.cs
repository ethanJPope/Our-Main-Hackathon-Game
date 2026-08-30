using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MainHackathonGame.Tests
{
    public sealed class CharacterAnimationRuntimeTests
    {
        [UnitySetUp]
        public IEnumerator LoadGameplayScene()
        {
            yield return SceneManager.LoadSceneAsync("TestScene", LoadSceneMode.Single);
            yield return null;
            yield return new WaitForSeconds(0.25f);
        }

        [UnityTest]
        public IEnumerator HumanoidsAreGroundedAndUseValidControllers()
        {
            GameObject player = RequiredObject("GameRoot/PlayerRoot");
            GameObject bandit = RequiredObject("SwordBandit_Prototype");
            GameObject warden = RequiredObject("ShadowWarden");

            AssertCharacterAnimator(player, "Player");
            AssertCharacterAnimator(bandit, "Sword Bandit");
            AssertCharacterAnimator(warden, "Shadow Warden");
            Assert.That(player.GetComponent<Rigidbody>(), Is.Null, "Player must not combine a dynamic Rigidbody with CharacterController motion.");
            Assert.That(player.GetComponent<CharacterController>(), Is.Not.Null);

            yield return new WaitForSeconds(0.35f);
            AssertFeetNearGround(player, "Player");
            AssertFeetNearGround(bandit, "Sword Bandit");
            AssertFeetNearGround(warden, "Shadow Warden");
        }

        [UnityTest]
        public IEnumerator EnemiesChaseWithDeliberateLocomotionAndCanDamagePlayer()
        {
            GameObject player = RequiredObject("GameRoot/PlayerRoot");
            GameObject bandit = RequiredObject("SwordBandit_Prototype");
            GameObject warden = RequiredObject("ShadowWarden");
            NavMeshAgent banditAgent = bandit.GetComponent<NavMeshAgent>();
            NavMeshAgent wardenAgent = warden.GetComponent<NavMeshAgent>();
            Assert.That(banditAgent, Is.Not.Null);
            Assert.That(wardenAgent, Is.Not.Null);
            Assert.That(banditAgent.isOnNavMesh, Is.True, "Sword Bandit must start on the baked NavMesh.");
            Assert.That(wardenAgent.isOnNavMesh, Is.True, "Shadow Warden must start on the baked NavMesh.");
            Assert.That(wardenAgent.speed, Is.InRange(1.1f, 1.5f), "Shadow Warden should use a slow, deliberate walk speed.");

            Vector3 playerPosition = player.transform.position;
            WarpNearPlayer(banditAgent, playerPosition + Vector3.forward * 4f + Vector3.left * 0.8f);
            WarpNearPlayer(wardenAgent, playerPosition + Vector3.forward * 5f + Vector3.right * 0.8f);
            bandit.transform.LookAt(new Vector3(playerPosition.x, bandit.transform.position.y, playerPosition.z));
            warden.transform.LookAt(new Vector3(playerPosition.x, warden.transform.position.y, playerPosition.z));
            Vector3 banditStart = bandit.transform.position;
            Vector3 wardenStart = warden.transform.position;
            float startingHealth = ReadFloat(RequiredComponent(player, "PlayerVitals"), "CurrentHealth");

            yield return new WaitForSeconds(1.8f);
            float banditMovement = Vector3.ProjectOnPlane(bandit.transform.position - banditStart, Vector3.up).magnitude;
            float wardenMovement = Vector3.ProjectOnPlane(warden.transform.position - wardenStart, Vector3.up).magnitude;
            Assert.That(banditMovement, Is.GreaterThan(0.2f), "Sword Bandit did not chase the player.");
            Assert.That(wardenMovement, Is.GreaterThan(0.2f), "Shadow Warden did not begin its slow chase.");

            yield return new WaitForSeconds(2.8f);
            float endingHealth = ReadFloat(RequiredComponent(player, "PlayerVitals"), "CurrentHealth");
            Assert.That(endingHealth, Is.LessThan(startingHealth), "Enemy animation contacts did not damage the player at close range.");
        }

        [UnityTest]
        public IEnumerator PlayerPunchMovesForwardAndDamagesAtContact()
        {
            GameObject player = RequiredObject("GameRoot/PlayerRoot");
            GameObject bandit = RequiredObject("SwordBandit_Prototype");
            GameObject warden = RequiredObject("ShadowWarden");
            SetBehaviourEnabled(RequiredComponent(bandit, "SwordBanditAI"), false);
            SetBehaviourEnabled(RequiredComponent(warden, "ShadowWardenAI"), false);
            NavMeshAgent banditAgent = bandit.GetComponent<NavMeshAgent>();
            if (banditAgent != null)
            {
                banditAgent.enabled = false;
            }

            warden.SetActive(false);
            player.transform.rotation = Quaternion.identity;
            bandit.transform.position = player.transform.position + player.transform.forward * 1.25f;
            bandit.transform.rotation = Quaternion.LookRotation(-player.transform.forward, Vector3.up);
            Physics.SyncTransforms();
            Component health = RequiredComponent(bandit, "EnemyHealth");
            Component attack = RequiredComponent(player, "PlayerSwordAttack");
            float startingHealth = ReadFloat(health, "CurrentHealth");

            bool started = (bool)Invoke(attack, "TryAttack");
            Assert.That(started, Is.True, "Player attack should start when off cooldown.");
            yield return new WaitForSeconds(0.12f);
            Animator playerAnimator = player.GetComponentInChildren<Animator>(true);
            AnimatorStateInfo attackState = playerAnimator.GetCurrentAnimatorStateInfo(0);
            Assert.That(attackState.IsName("Forward Punch") || attackState.IsName("Base Layer.Forward Punch"), Is.True,
                "Player Animator did not enter the forward punch state.");

            Assert.That(ReadFloat(health, "CurrentHealth"), Is.EqualTo(startingHealth).Within(0.001f),
                "Damage was applied before the authored contact callback.");
            Invoke(attack, "OnAnimationAttackContact");
            yield return null;
            float contactHealth = ReadFloat(health, "CurrentHealth");
            Assert.That(startingHealth - contactHealth, Is.EqualTo(10f).Within(0.01f),
                "The authored contact callback should apply exactly one attack damage quantum.");

            Invoke(attack, "OnAnimationAttackContact");

            yield return new WaitForSeconds(0.42f);
            float endingHealth = ReadFloat(health, "CurrentHealth");
            Assert.That(endingHealth, Is.EqualTo(contactHealth).Within(0.001f),
                "Duplicate animation callbacks or the timed fallback applied a second hit.");
        }

        [UnityTest]
        public IEnumerator LethalPlayerDamageStopsActionsAndStartsDeathReaction()
        {
            GameObject player = RequiredObject("GameRoot/PlayerRoot");
            Component vitals = RequiredComponent(player, "PlayerVitals");
            Component attack = RequiredComponent(player, "PlayerSwordAttack");
            Component movement = RequiredComponent(player, "PlayerJoystickMovement");
            Component reaction = RequiredComponent(player, "CombatHitReaction");

            bool damaged = (bool)Invoke(vitals, "ApplyDamage", 1000f, player.transform.position + Vector3.up, Vector3.back);
            Assert.That(damaged, Is.True);
            Assert.That(ReadBool(vitals, "IsDead"), Is.True, "Lethal damage did not enter the player death state.");
            Assert.That(ReadBool(attack, "CanAttack"), Is.False, "Dead player can still begin attacks.");
            Assert.That(ReadBool(movement, "CanAct"), Is.False, "Dead player can still move or dodge.");
            Assert.That(ReadBool(reaction, "IsReacting"), Is.True, "Lethal damage did not start a visible reaction.");
            Assert.That((bool)Invoke(attack, "TryAttack"), Is.False, "Dead player accepted a new attack request.");
            Assert.That((bool)Invoke(vitals, "ApplyDamage", 10f, player.transform.position, Vector3.forward), Is.False,
                "Dead player accepted repeated damage.");

            yield return new WaitForSeconds(0.12f);
            Animator animator = player.GetComponentInChildren<Animator>(true);
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            Assert.That(state.IsName("Death") || state.IsName("Base Layer.Death"), Is.True,
                "Player Animator did not enter the death state after lethal damage.");
        }

        [UnityTest]
        public IEnumerator EnemiesRefuseMeleeContactThroughSolidCover()
        {
            GameObject player = RequiredObject("GameRoot/PlayerRoot");
            GameObject bandit = RequiredObject("SwordBandit_Prototype");
            GameObject warden = RequiredObject("ShadowWarden");
            NavMeshAgent banditAgent = bandit.GetComponent<NavMeshAgent>();
            NavMeshAgent wardenAgent = warden.GetComponent<NavMeshAgent>();

            Vector3 playerPosition = player.transform.position;
            WarpNearPlayer(banditAgent, playerPosition + Vector3.forward * 1.42f + Vector3.left * 0.45f);
            WarpNearPlayer(wardenAgent, playerPosition + Vector3.forward * 1.55f + Vector3.right * 0.45f);
            bandit.transform.LookAt(new Vector3(playerPosition.x, bandit.transform.position.y, playerPosition.z));
            warden.transform.LookAt(new Vector3(playerPosition.x, warden.transform.position.y, playerPosition.z));

            GameObject cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cover.name = "Combat LOS Test Cover";
            cover.transform.position = playerPosition + Vector3.forward * 0.72f + Vector3.up * 1.2f;
            cover.transform.localScale = new Vector3(4f, 2.4f, 0.22f);
            Physics.SyncTransforms();

            try
            {
                Component banditAi = RequiredComponent(bandit, "SwordBanditAI");
                Component wardenAi = RequiredComponent(warden, "ShadowWardenAI");
                float banditDistance = Vector3.ProjectOnPlane(playerPosition - bandit.transform.position, Vector3.up).magnitude;

                Assert.That((bool)InvokePrivate(banditAi, "CanStartAttack", banditDistance), Is.False,
                    "Sword Bandit would start a sword strike through solid cover.");
                Assert.That((bool)InvokePrivate(wardenAi, "CanHitTarget"), Is.False,
                    "Shadow Warden would start a heavy strike through solid cover.");
                yield return null;
            }
            finally
            {
                Object.Destroy(cover);
            }
        }

        private static GameObject RequiredObject(string pathOrName)
        {
            GameObject gameObject = GameObject.Find(pathOrName);
            Assert.That(gameObject, Is.Not.Null, $"Required scene object '{pathOrName}' is missing.");
            return gameObject;
        }

        private static Component RequiredComponent(GameObject gameObject, string componentName)
        {
            Component component = gameObject.GetComponent(componentName);
            if (component == null)
            {
                foreach (Component candidate in gameObject.GetComponentsInChildren<Component>(true))
                {
                    if (candidate != null && candidate.GetType().Name == componentName)
                    {
                        component = candidate;
                        break;
                    }
                }
            }

            Assert.That(component, Is.Not.Null, $"{gameObject.name} is missing {componentName}.");
            return component;
        }

        private static void AssertCharacterAnimator(GameObject root, string label)
        {
            Animator animator = root.GetComponentInChildren<Animator>(true);
            Assert.That(animator, Is.Not.Null, $"{label} has no Animator.");
            Assert.That(animator.isHuman, Is.True, $"{label} is not using a valid Humanoid Avatar.");
            Assert.That(animator.runtimeAnimatorController, Is.Not.Null, $"{label} has no Animator Controller.");
            Assert.That(animator.layerCount, Is.GreaterThan(0));
        }

        private static void AssertFeetNearGround(GameObject root, string label)
        {
            Animator animator = root.GetComponentInChildren<Animator>(true);
            AssertFootNearGround(root.transform, animator.GetBoneTransform(HumanBodyBones.LeftFoot), label + " left foot");
            AssertFootNearGround(root.transform, animator.GetBoneTransform(HumanBodyBones.RightFoot), label + " right foot");
        }

        private static void AssertFootNearGround(Transform characterRoot, Transform foot, string label)
        {
            Assert.That(foot, Is.Not.Null, $"{label} is not mapped in the Humanoid Avatar.");
            RaycastHit[] hits = Physics.RaycastAll(foot.position + Vector3.up * 0.2f, Vector3.down, 10f, ~0, QueryTriggerInteraction.Ignore);
            float bestDistance = float.PositiveInfinity;
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.transform == characterRoot || hit.collider.transform.IsChildOf(characterRoot))
                {
                    continue;
                }

                bestDistance = Mathf.Min(bestDistance, foot.position.y - hit.point.y);
            }

            Assert.That(bestDistance, Is.LessThanOrEqualTo(0.28f),
                $"{label} is visibly floating {bestDistance:0.000}m above the ground (foot={foot.position}, root={characterRoot.position}).");
            Assert.That(bestDistance, Is.GreaterThanOrEqualTo(-0.08f), $"{label} is penetrating the ground by {-bestDistance:0.000}m.");
        }

        private static void WarpNearPlayer(NavMeshAgent agent, Vector3 desiredPosition)
        {
            bool found = NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, 3f, NavMesh.AllAreas);
            Assert.That(found, Is.True, $"Could not find a NavMesh position near {desiredPosition}.");
            Assert.That(agent.Warp(hit.position), Is.True, $"Could not warp {agent.name} onto the NavMesh test position.");
        }

        private static float ReadFloat(Component component, string propertyName)
        {
            PropertyInfo property = component.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, $"{component.GetType().Name}.{propertyName} is missing.");
            return (float)property.GetValue(component);
        }

        private static bool ReadBool(Component component, string propertyName)
        {
            PropertyInfo property = component.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, $"{component.GetType().Name}.{propertyName} is missing.");
            return (bool)property.GetValue(component);
        }

        private static object Invoke(Component component, string methodName, params object[] arguments)
        {
            MethodInfo method = component.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(method, Is.Not.Null, $"{component.GetType().Name}.{methodName} is missing.");
            return method.Invoke(component, arguments);
        }

        private static object InvokePrivate(Component component, string methodName, params object[] arguments)
        {
            MethodInfo method = component.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"{component.GetType().Name}.{methodName} is missing.");
            return method.Invoke(component, arguments);
        }

        private static void SetBehaviourEnabled(Component component, bool enabled)
        {
            Assert.That(component, Is.InstanceOf<Behaviour>());
            ((Behaviour)component).enabled = enabled;
        }
    }
}
