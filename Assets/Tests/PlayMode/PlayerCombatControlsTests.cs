using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MainHackathonGame.Tests
{
    public sealed class PlayerCombatControlsTests
    {
        private GameObject player;
        private Component movement, attack, vitals, input, targeting;
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        [UnitySetUp]
        public IEnumerator LoadPlayerScene()
        {
            Time.timeScale=1f;
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/PlayerScene.unity",new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return SceneManager.LoadSceneAsync("PlayerScene");
#endif
            yield return null;
            var practice=GameObject.Find("PlayerPracticeTarget");
            if(practice!=null) practice.SetActive(false);
            yield return new WaitForSeconds(0.15f);
            player=GameObject.Find("GameRoot/PlayerRoot");
            Assert.That(player,Is.Not.Null);
            movement=player.GetComponent("PlayerJoystickMovement"); attack=player.GetComponent("PlayerSwordAttack");
            vitals=player.GetComponent("PlayerVitals"); input=player.GetComponent("PlayerCombatInput"); targeting=player.GetComponent("PlayerCombatTargeting");
            Assert.That(player.GetComponent<CharacterController>().isGrounded,Is.True);
        }
        [UnityTearDown] public IEnumerator Cleanup() { Time.timeScale=1f; yield return null; }
        private static object Call(Component c,string method,params object[] args)=>c.GetType().GetMethod(method,Flags).Invoke(c,args);
        private static T Read<T>(Component c,string property)=>(T)c.GetType().GetProperty(property,Flags).GetValue(c);
        private static void Set(Component c,string field,object value)=>c.GetType().GetField(field,Flags).SetValue(c,value);
        private void Stamina(float value)
        {
            var method=vitals.GetType().GetMethod("SetCurrent");
            var resource=Enum.Parse(method.GetParameters()[0].ParameterType,"Stamina");
            method.Invoke(vitals,new object[]{resource,value});
        }
        private void Queue(string action)
        {
            var method=input.GetType().GetMethod("Queue");
            method.Invoke(input,new[]{Enum.Parse(method.GetParameters()[0].ParameterType,action)});
        }
        private bool Damage()=> (bool)Call(vitals,"ApplyDamage",10f,player.transform.position,Vector3.forward);

        [UnityTest] public IEnumerator DirectionalDodgeUsesInputInsteadOfFacing()
        {
            player.transform.rotation=Quaternion.identity;
            Vector3 start=player.transform.position;
            Assert.That((bool)Call(movement,"TryDodge",Vector3.right),Is.True);
            Assert.That(Read<float>(vitals,"CurrentStamina"),Is.EqualTo(82f).Within(0.1f));
            yield return new WaitForSeconds(0.36f);
            Vector3 delta=player.transform.position-start;
            Assert.That(delta.x,Is.EqualTo(3.2f).Within(0.15f));
            Assert.That(Mathf.Abs(delta.z),Is.LessThan(0.1f));
        }
        [UnityTest] public IEnumerator NeutralDodgeBackstepsAndDoesNotRepeat()
        {
            player.transform.rotation=Quaternion.identity;
            Vector3 start=player.transform.position;
            Assert.That((bool)Call(movement,"TryDodge",Vector3.zero),Is.True);
            Assert.That((bool)Call(movement,"TryDodge",Vector3.forward),Is.False);
            yield return new WaitForSeconds(0.65f);
            Assert.That(player.transform.position.z-start.z,Is.EqualTo(-3.2f).Within(0.15f));
        }
        [UnityTest] public IEnumerator DodgeProtectsOnlyDuringItsAuthoredWindow()
        {
            Assert.That((bool)Call(movement,"TryDodge",Vector3.right),Is.True);
            Assert.That(Read<bool>(movement,"IsInvulnerable"),Is.False,"Startup must be vulnerable.");
            yield return new WaitForSeconds(0.09f);
            Assert.That(Read<bool>(movement,"IsInvulnerable"),Is.True);
            Assert.That(Damage(),Is.False);
            yield return new WaitForSeconds(0.18f);
            Assert.That(Read<bool>(movement,"IsInvulnerable"),Is.False);
            Assert.That(Damage(),Is.True,"Recovery must be punishable.");
        }
        [UnityTest] public IEnumerator AttacksAndDodgesCannotOverlap()
        {
            Assert.That((bool)Call(attack,"TryAttack"),Is.True);
            Assert.That(Read<float>(vitals,"CurrentStamina"),Is.EqualTo(88f).Within(0.1f));
            Assert.That((bool)Call(movement,"TryDodge",Vector3.right),Is.False);
            Assert.That((bool)Call(attack,"TryAttack"),Is.False);
            yield return new WaitForSeconds(0.76f);
            Assert.That((bool)Call(movement,"TryDodge",Vector3.right),Is.True);
            Assert.That((bool)Call(attack,"TryAttack"),Is.False);
            yield return new WaitForSeconds(0.36f);
            Assert.That(Read<bool>(movement,"IsDodging"),Is.False);
            Assert.That((bool)Call(attack,"TryAttack"),Is.False,"Dodge recovery must also block attacks.");
        }
        [UnityTest] public IEnumerator TakingDamageInterruptsPendingStrike()
        {
            var dummy=CreateTarget(1.2f);
            yield return null;
            var health=dummy.GetComponent("EnemyHealth");
            float before=Read<float>(health,"CurrentHealth");
            Assert.That((bool)Call(attack,"TryAttack"),Is.True);
            yield return new WaitForSeconds(0.1f);
            Assert.That(Damage(),Is.True);
            Assert.That((bool)Call(movement,"TryDodge",Vector3.right),Is.False,"Cannot dodge out of hit stun.");
            yield return new WaitForSeconds(0.4f);
            Assert.That(Read<float>(health,"CurrentHealth"),Is.EqualTo(before));
            UnityEngine.Object.Destroy(dummy);
        }
        [UnityTest] public IEnumerator DeathRejectsActionsAndClearsBufferedInput()
        {
            Queue("Attack");
            Call(vitals,"ApplyDamage",1000f,player.transform.position,Vector3.forward);
            yield return null;
            Assert.That((bool)Call(attack,"TryAttack"),Is.False);
            Assert.That((bool)Call(movement,"TryDodge",Vector3.right),Is.False);
            Assert.That(Read<bool>(vitals,"IsDead"),Is.True);
        }
        [UnityTest] public IEnumerator LateDodgePressSurvivesAttackRecovery()
        {
            Assert.That((bool)Call(attack,"TryAttack"),Is.True);
            yield return new WaitForSeconds(0.62f);
            Queue("Dodge");
            yield return new WaitForSeconds(0.14f);
            Assert.That(Read<bool>(movement,"IsDodging"),Is.True);
        }
        [UnityTest] public IEnumerator EarlyPressExpiresInsteadOfFiringMuchLater()
        {
            Assert.That((bool)Call(attack,"TryAttack"),Is.True);
            Queue("Dodge");
            Vector3 start=player.transform.position;
            yield return new WaitForSeconds(0.8f);
            Assert.That(Read<bool>(movement,"IsDodging"),Is.False);
            Assert.That(Vector3.ProjectOnPlane(player.transform.position-start,Vector3.up).magnitude,Is.LessThan(0.01f));
        }
        [UnityTest] public IEnumerator ExhaustionPauseAndDisableRejectActions()
        {
            Stamina(0f);
            Assert.That((bool)Call(attack,"TryAttack"),Is.False);
            Assert.That((bool)Call(movement,"TryDodge",Vector3.right),Is.False);
            Stamina(100f);
            Time.timeScale=0f;
            Assert.That((bool)Call(attack,"TryAttack"),Is.False);
            Assert.That((bool)Call(movement,"TryDodge",Vector3.right),Is.False);
            Time.timeScale=1f;
            ((Behaviour)movement).enabled=false;
            Assert.That((bool)Call(movement,"TryDodge",Vector3.right),Is.False);
            ((Behaviour)attack).enabled=false;
            Assert.That((bool)Call(attack,"TryAttack"),Is.False);
            yield return null;
        }
        [UnityTest] public IEnumerator DodgeCollidesWithWall()
        {
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position=player.transform.position+Vector3.right*1.5f+Vector3.up;
            wall.transform.localScale=new Vector3(0.2f,3f,4f);
            Physics.SyncTransforms();
            float start=player.transform.position.x;
            Assert.That((bool)Call(movement,"TryDodge",Vector3.right),Is.True);
            yield return new WaitForSeconds(0.36f);
            Assert.That(player.transform.position.x-start,Is.LessThan(1.3f));
            UnityEngine.Object.Destroy(wall);
        }
        [UnityTest] public IEnumerator LockAcquiresVisibleTargetAndReleasesOnDeath()
        {
            var dummy=CreateTarget(3f);
            yield return null;
            Call(targeting,"ToggleLock");
            Assert.That(Read<bool>(targeting,"IsLocked"),Is.True);
            UnityEngine.Object.Destroy(dummy);
            yield return null;
            yield return null;
            Assert.That(Read<bool>(targeting,"IsLocked"),Is.False);
        }
        [UnityTest] public IEnumerator StrikeDamagesOnlyAtContactAndOnce()
        {
            var dummy=CreateTarget(1.2f);
            yield return null;
            var health=dummy.GetComponent("EnemyHealth");
            float before=Read<float>(health,"CurrentHealth");
            Assert.That((bool)Call(attack,"TryAttack"),Is.True);
            yield return new WaitForSeconds(0.12f);
            Assert.That(Read<float>(health,"CurrentHealth"),Is.EqualTo(before));
            yield return new WaitForSeconds(0.32f);
            Assert.That(Read<float>(health,"CurrentHealth"),Is.EqualTo(before-10f));
            Call(attack,"OnAnimationAttackContact");
            Assert.That(Read<float>(health,"CurrentHealth"),Is.EqualTo(before-10f));
            UnityEngine.Object.Destroy(dummy);
        }
        private GameObject CreateTarget(float distance)
        {
            var dummy=new GameObject("Combat Test Target");
            dummy.transform.position=player.transform.position+player.transform.forward*distance;
            var capsule=dummy.AddComponent<CapsuleCollider>(); capsule.center=Vector3.up; capsule.height=2f; capsule.radius=0.45f;
            dummy.AddComponent(vitals.GetType().Assembly.GetType("EnemyHealth"));
            Physics.SyncTransforms(); return dummy;
        }
    }
}
