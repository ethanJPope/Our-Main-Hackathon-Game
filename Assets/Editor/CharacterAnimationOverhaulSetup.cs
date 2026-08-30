using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

public static class CharacterAnimationOverhaulSetup
{
    private const string TestScenePath = "Assets/TestScene.unity";
    private const string PlayerControllerPath = "Assets/Models/Hy3D/Animations/AC_Player.controller";
    private const string BanditControllerPath = "Assets/Art/Characters/Bandits/SwordBandit/Animations/AC_SwordBandit.controller";
    private const string WardenControllerPath = "Assets/Art/Characters/MiniBosses/ShadowWarden/Animations/AC_ShadowWarden.controller";
    private const string PlayerIdlePath = "Assets/Models/Hy3D/Animations/Idle.fbx";
    private const string PlayerWalkPath = "Assets/Models/Hy3D/Animations/Walk.fbx";
    private const string PlayerDodgePath = "Assets/Models/Hy3D/Animations/DodgeRight.fbx";
    private const string PlayerPunchPath = "Assets/Models/Hy3D/Animations/Punch.fbx";
    private const string BanditAnimationPath = "Assets/Art/Characters/Bandits/SwordBandit/Models/SM_SwordBandit.fbx";
    private const string WardenAnimationPath = "Assets/Art/Characters/MiniBosses/ShadowWarden/Models/SM_ShadowWarden.fbx";
    private const string PlayerCohesionTexturePath = "Assets/Art/Characters/Player/Textures/T_Player_Cohesion_BaseColor.png";
    private const string BanditCohesionTexturePath = "Assets/Art/Characters/Bandits/SwordBandit/Textures/T_HoodedSwordBandit_Cohesion_BaseColor.png";
    private const string WardenCohesionTexturePath = "Assets/Art/Characters/MiniBosses/ShadowWarden/Textures/T_ShadowWarden_Cohesion_BaseColor.png";

    [MenuItem("Tools/Main Hackathon Game/Rebuild Character Animation Systems")]
    public static void Rebuild()
    {
        if (EditorSceneManager.GetActiveScene().path != TestScenePath)
        {
            throw new InvalidOperationException($"Open {TestScenePath} before rebuilding character animation systems.");
        }

        AddAnimationContactEvent(PlayerPunchPath, "Punch", 0.43f);
        AddAnimationContactEvent(BanditAnimationPath, "SB_Attack_Slash", 0.52f);

        AnimationClip idle = LoadClip(PlayerIdlePath, "Idle");
        AnimationClip walk = LoadClip(PlayerWalkPath, "Walk");
        AnimationClip dodge = LoadClip(PlayerDodgePath, "DodgeRight");
        AnimationClip punch = LoadClip(PlayerPunchPath, "Punch");
        AnimationClip banditIdle = LoadClip(BanditAnimationPath, "SB_Idle");
        AnimationClip banditWalk = LoadClip(BanditAnimationPath, "SB_Walk");
        AnimationClip banditAttack = LoadClip(BanditAnimationPath, "SB_Attack_Slash");
        AnimationClip hit = LoadClip(BanditAnimationPath, "SB_Hit");
        AnimationClip death = LoadClip(BanditAnimationPath, "SB_Death");
        AnimationClip wardenIdle = LoadClip(WardenAnimationPath, "SW_Idle");
        AnimationClip wardenWalk = LoadClip(WardenAnimationPath, "SW_Walk");

        BuildPlayerController(idle, walk, dodge, punch, hit, death);
        BuildEnemyController(BanditControllerPath, banditIdle, banditWalk, banditAttack, hit, death, 1f);
        BuildEnemyController(WardenControllerPath, wardenIdle, wardenWalk, punch, hit, death, 0.72f);
        ConfigureScene();
        ConfigureMaterials();

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[CharacterAnimationOverhaul] Controllers, contact events, characters, materials, and scene wiring rebuilt successfully.");
    }

    [MenuItem("Tools/Main Hackathon Game/Validate Character Animation Systems")]
    public static void ValidateSetup()
    {
        List<string> failures = new List<string>();
        ValidateHumanoid("Assets/Models/Hy3D/Hy3D_rigged_mia.fbx", "Player", failures);
        ValidateHumanoid("Assets/Art/Characters/Bandits/SwordBandit/Models/SM_SwordBandit.fbx", "Sword Bandit", failures);
        ValidateHumanoid(WardenAnimationPath, "Shadow Warden", failures);
        ValidateController(PlayerControllerPath, new[] { "Idle", "Walk", "Dodge", "Forward Punch", "Hit Reaction", "Death" }, true, failures);
        ValidateController(BanditControllerPath, new[] { "Idle", "Walk", "Attack", "Hit Reaction", "Death" }, false, failures);
        ValidateController(WardenControllerPath, new[] { "Idle", "Walk", "Attack", "Hit Reaction", "Death" }, false, failures);
        ValidateContactEvent(PlayerPunchPath, "Punch", failures);
        ValidateContactEvent(BanditAnimationPath, "SB_Attack_Slash", failures);

        GameObject player = GameObject.Find("GameRoot/PlayerRoot");
        GameObject bandit = GameObject.Find("SwordBandit_Prototype");
        GameObject warden = GameObject.Find("ShadowWarden");
        Require(player != null, "PlayerRoot is missing from the active scene.", failures);
        Require(bandit != null, "SwordBandit_Prototype is missing from the active scene.", failures);
        Require(warden != null, "ShadowWarden is missing from the active scene.", failures);
        if (player != null)
        {
            Require(player.GetComponent<Rigidbody>() == null, "PlayerRoot still has a Rigidbody competing with its CharacterController.", failures);
            Require(player.GetComponent<CharacterController>() != null, "PlayerRoot has no CharacterController.", failures);
            Require(player.GetComponent<PlayerSwordAttack>() != null, "PlayerRoot has no PlayerSwordAttack.", failures);
            ValidateRuntimePolish(player.GetComponentInChildren<HumanoidAnimationRuntime>(true), "Player", failures);
            ValidateSceneAnimator(player.GetComponentInChildren<Animator>(true), PlayerControllerPath, "Player", failures);
        }
        if (bandit != null)
        {
            SwordBanditAI banditAi = bandit.GetComponent<SwordBanditAI>();
            Require(banditAi != null, "Sword Bandit AI is missing.", failures);
            ValidateMeleeSafety(banditAi, "Sword Bandit", requireAnimationContact: true, failures);
            ValidateRuntimePolish(bandit.GetComponentInChildren<HumanoidAnimationRuntime>(true), "Sword Bandit", failures);
            Animator banditAnimator = bandit.GetComponentInChildren<Animator>(true);
            ValidateSceneAnimator(banditAnimator, BanditControllerPath, "Sword Bandit", failures);
            Transform hand = banditAnimator != null ? banditAnimator.GetBoneTransform(HumanBodyBones.RightHand) : null;
            Require(hand != null && hand.Find("BanditSword") != null, "Sword Bandit weapon is not attached to the humanoid right hand.", failures);
        }
        if (warden != null)
        {
            ShadowWardenAI wardenAi = warden.GetComponent<ShadowWardenAI>();
            Require(wardenAi != null, "Shadow Warden AI is missing.", failures);
            ValidateMeleeSafety(wardenAi, "Shadow Warden", requireAnimationContact: false, failures);
            Require(warden.GetComponent<NavMeshAgent>() != null, "Shadow Warden has no NavMeshAgent.", failures);
            Require(warden.GetComponent<EnemyHealth>() != null, "Shadow Warden has no health component.", failures);
            ValidateRuntimePolish(warden.GetComponent<HumanoidAnimationRuntime>(), "Shadow Warden", failures);
            ValidateSceneAnimator(warden.GetComponent<Animator>(), WardenControllerPath, "Shadow Warden", failures);
            ValidateWardenHoodLiner(warden, failures);
        }
        ValidateCharacterMaterialCohesion(failures);

        if (failures.Count > 0)
        {
            throw new InvalidOperationException("Character animation validation failed:\n- " + string.Join("\n- ", failures));
        }

        Debug.Log("[CharacterAnimationOverhaul] VALIDATION PASSED: humanoid rigs, controllers, animation contacts, scene wiring, grounding, AI, and bandit weapon are configured.");
    }

    [MenuItem("Tools/Main Hackathon Game/Trigger Player Test Attack")]
    public static void TriggerPlayerTestAttack()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Enter Play Mode before triggering the player test attack.");
        }

        GameObject player = GameObject.Find("GameRoot/PlayerRoot");
        PlayerSwordAttack attack = player != null ? player.GetComponent<PlayerSwordAttack>() : null;
        if (attack == null)
        {
            throw new InvalidOperationException("PlayerSwordAttack was not found on PlayerRoot.");
        }

        attack.TryAttack();
        Debug.Log("[CharacterAnimationOverhaul] Player test attack triggered.");
    }

    private static void BuildPlayerController(
        AnimationClip idle,
        AnimationClip walk,
        AnimationClip dodge,
        AnimationClip punch,
        AnimationClip hit,
        AnimationClip death)
    {
        AnimatorController controller = PrepareController(PlayerControllerPath);
        AddSharedParameters(controller, includeDodge: true, attackParameter: "Punch");
        AnimatorStateMachine machine = controller.layers[0].stateMachine;

        AnimatorState idleState = AddState(machine, "Idle", idle, 1f, true);
        AnimatorState walkState = AddState(machine, "Walk", walk, 1f, true, useLocomotionPlayback: true);
        AnimatorState dodgeState = AddState(machine, "Dodge", dodge, 1.05f, false);
        AnimatorState attackState = AddState(machine, "Forward Punch", punch, 1.25f, true);
        AnimatorState hitState = AddState(machine, "Hit Reaction", hit, 1f, true);
        AnimatorState deathState = AddState(machine, "Death", death, 1f, false);
        machine.defaultState = idleState;

        AddLocomotionTransitions(idleState, walkState);
        AddAnyTrigger(machine, dodgeState, "Dodge", 0.04f);
        AddExit(dodgeState, idleState, 0.9f, 0.08f);
        AddAnyTrigger(machine, attackState, "Punch", 0.035f);
        AddExit(attackState, idleState, 0.93f, 0.07f);
        AddAnyTrigger(machine, hitState, "Hit", 0.025f);
        AddExit(hitState, idleState, 0.9f, 0.08f);
        AddAnyTrigger(machine, deathState, "Die", 0.05f);
    }

    private static void BuildEnemyController(
        string path,
        AnimationClip idle,
        AnimationClip walk,
        AnimationClip attack,
        AnimationClip hit,
        AnimationClip death,
        float attackSpeed)
    {
        AnimatorController controller = PrepareController(path);
        AddSharedParameters(controller, includeDodge: false, attackParameter: "Attack");
        AnimatorStateMachine machine = controller.layers[0].stateMachine;

        AnimatorState idleState = AddState(machine, "Idle", idle, 1f, true);
        AnimatorState walkState = AddState(machine, "Walk", walk, 1f, true, useLocomotionPlayback: true);
        AnimatorState attackState = AddState(machine, "Attack", attack, attackSpeed, true);
        AnimatorState hitState = AddState(machine, "Hit Reaction", hit, 1f, true);
        AnimatorState deathState = AddState(machine, "Death", death, 0.92f, false);
        machine.defaultState = idleState;

        AddLocomotionTransitions(idleState, walkState);
        AddAnyTrigger(machine, attackState, "Attack", 0.045f);
        AddExit(attackState, idleState, 0.94f, 0.09f);
        AddAnyTrigger(machine, hitState, "Hit", 0.025f);
        AddExit(hitState, idleState, 0.9f, 0.08f);
        AddAnyTrigger(machine, deathState, "Die", 0.05f);
    }

    private static AnimatorController PrepareController(string path)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        }

        controller.parameters = Array.Empty<AnimatorControllerParameter>();
        AnimatorControllerLayer[] layers = controller.layers;
        if (layers.Length == 0)
        {
            controller.AddLayer("Base Layer");
            layers = controller.layers;
        }

        AnimatorControllerLayer layer = layers[0];
        layer.name = "Base Layer";
        layer.defaultWeight = 1f;
        layer.iKPass = true;
        controller.layers = new[] { layer };

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        foreach (ChildAnimatorState childState in machine.states.ToArray())
        {
            machine.RemoveState(childState.state);
        }

        foreach (AnimatorStateTransition transition in machine.anyStateTransitions.ToArray())
        {
            machine.RemoveAnyStateTransition(transition);
        }

        foreach (ChildAnimatorStateMachine childMachine in machine.stateMachines.ToArray())
        {
            machine.RemoveStateMachine(childMachine.stateMachine);
        }

        return controller;
    }

    private static void AddSharedParameters(AnimatorController controller, bool includeDodge, string attackParameter)
    {
        List<AnimatorControllerParameter> parameters = new List<AnimatorControllerParameter>
        {
            new AnimatorControllerParameter
            {
                name = "Speed",
                type = AnimatorControllerParameterType.Float,
                defaultFloat = 0f
            },
            new AnimatorControllerParameter
            {
                name = "LocomotionPlayback",
                type = AnimatorControllerParameterType.Float,
                defaultFloat = 1f
            },
            new AnimatorControllerParameter
            {
                name = "Grounded",
                type = AnimatorControllerParameterType.Bool,
                defaultBool = true
            },
            new AnimatorControllerParameter { name = attackParameter, type = AnimatorControllerParameterType.Trigger },
            new AnimatorControllerParameter { name = "Hit", type = AnimatorControllerParameterType.Trigger },
            new AnimatorControllerParameter { name = "Die", type = AnimatorControllerParameterType.Trigger }
        };
        if (includeDodge)
        {
            parameters.Add(new AnimatorControllerParameter { name = "Dodge", type = AnimatorControllerParameterType.Trigger });
        }

        controller.parameters = parameters.ToArray();
    }

    private static void ValidateHumanoid(string path, string label, List<string> failures)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        Require(importer != null, $"{label} model importer is missing at {path}.", failures);
        if (importer != null)
        {
            Require(importer.animationType == ModelImporterAnimationType.Human, $"{label} is not imported as Humanoid.", failures);
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
            Require(avatar != null && avatar.isValid && avatar.isHuman, $"{label} has no valid Humanoid Avatar.", failures);
        }
    }

    private static void ValidateController(string path, IEnumerable<string> expectedStates, bool player, List<string> failures)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        Require(controller != null, $"Animator Controller is missing at {path}.", failures);
        if (controller == null)
        {
            return;
        }

        Require(controller.layers.Length == 1 && controller.layers[0].iKPass, $"{controller.name} must have one IK-enabled base layer.", failures);
        HashSet<string> states = controller.layers[0].stateMachine.states.Select(item => item.state.name).ToHashSet();
        foreach (string expected in expectedStates)
        {
            Require(states.Contains(expected), $"{controller.name} is missing state '{expected}'.", failures);
        }

        Dictionary<string, AnimatorControllerParameter> parameters = controller.parameters.ToDictionary(parameter => parameter.name);
        Require(parameters.TryGetValue("LocomotionPlayback", out AnimatorControllerParameter playback)
            && Mathf.Approximately(playback.defaultFloat, 1f), $"{controller.name} LocomotionPlayback must default to 1.", failures);
        Require(parameters.TryGetValue("Grounded", out AnimatorControllerParameter grounded)
            && grounded.defaultBool, $"{controller.name} Grounded must default to true.", failures);
        Require(parameters.ContainsKey(player ? "Punch" : "Attack"), $"{controller.name} is missing its attack trigger.", failures);
    }

    private static void ValidateSceneAnimator(Animator animator, string controllerPath, string label, List<string> failures)
    {
        RuntimeAnimatorController expectedController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
        Require(animator != null, $"{label} scene Animator is missing.", failures);
        if (animator == null)
        {
            return;
        }

        Require(animator.avatar != null && animator.avatar.isValid && animator.avatar.isHuman,
            $"{label} scene Animator does not use a valid Humanoid Avatar.", failures);
        Require(animator.runtimeAnimatorController == expectedController,
            $"{label} scene Animator is not assigned to {controllerPath}.", failures);
    }

    private static void ValidateContactEvent(string path, string clipName, List<string> failures)
    {
        AnimationClip clip = LoadClip(path, clipName);
        Require(clip != null, $"Animation clip {clipName} is missing at {path}.", failures);
        if (clip != null)
        {
            int contacts = AnimationUtility.GetAnimationEvents(clip)
                .Count(animationEvent => animationEvent.functionName == "AnimationAttackContact");
            Require(contacts == 1, $"{clipName} must contain exactly one AnimationAttackContact event; found {contacts}.", failures);
        }
    }

    private static void ValidateRuntimePolish(
        HumanoidAnimationRuntime runtime,
        string label,
        List<string> failures)
    {
        Require(runtime != null, $"{label} has no Humanoid grounding and balance runtime.", failures);
        if (runtime == null)
        {
            return;
        }

        SerializedObject serialized = new SerializedObject(runtime);
        Require(serialized.FindProperty("maximumForwardLeanDegrees").floatValue <= 6f,
            $"{label} forward locomotion lean must stay subtle enough to preserve the authored clip.", failures);
        Require(serialized.FindProperty("maximumLateralLeanDegrees").floatValue <= 4f,
            $"{label} lateral locomotion lean must stay subtle enough to preserve the authored clip.", failures);
        Require(serialized.FindProperty("maximumTurnTwistDegrees").floatValue <= 7f,
            $"{label} torso turn twist must stay subtle enough to preserve the authored clip.", failures);
        SerializedProperty plantHeight = serialized.FindProperty("footPlantHeight");
        SerializedProperty releaseHeight = serialized.FindProperty("footReleaseHeight");
        Require(plantHeight != null && releaseHeight != null
            && plantHeight.floatValue >= 0.04f
            && releaseHeight.floatValue >= plantHeight.floatValue
            && releaseHeight.floatValue <= 0.22f,
            $"{label} foot IK must retain a stable plant/release window so swing feet do not slide.", failures);
    }

    private static void ValidateMeleeSafety(
        UnityEngine.Object ai,
        string label,
        bool requireAnimationContact,
        List<string> failures)
    {
        if (ai == null)
        {
            return;
        }

        SerializedObject serialized = new SerializedObject(ai);
        SerializedProperty sight = serialized.FindProperty("requireLineOfSight");
        Require(sight != null && sight.boolValue,
            $"{label} must require line of sight before a melee contact can land.", failures);

        if (requireAnimationContact)
        {
            SerializedProperty animationContact = serialized.FindProperty("useAnimationStrikeEvent");
            Require(animationContact != null && animationContact.boolValue,
                $"{label} must use its authored animation strike-contact event.", failures);
        }
    }

    private static void ValidateCharacterMaterialCohesion(List<string> failures)
    {
        (string materialPath, string expectedTexturePath)[] materials =
        {
            ("Assets/Art/Characters/Player/Materials/MAT_Player_Hy3D.mat", PlayerCohesionTexturePath),
            ("Assets/Art/Characters/Bandits/SwordBandit/Materials/MAT_HoodedSwordBandit.mat", BanditCohesionTexturePath),
            ("Assets/Art/Characters/MiniBosses/ShadowWarden/Materials/MAT_ShadowWarden.mat", WardenCohesionTexturePath)
        };

        foreach ((string materialPath, string expectedTexturePath) in materials)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Require(material != null, $"Character material is missing at {materialPath}.", failures);
            if (material == null)
            {
                continue;
            }

            Require(material.HasProperty("_BaseColorMap") && material.GetTexture("_BaseColorMap") != null,
                $"{material.name} must retain a base-color texture.", failures);
            Texture assignedTexture = material.HasProperty("_BaseColorMap")
                ? material.GetTexture("_BaseColorMap")
                : null;
            Require(assignedTexture != null && AssetDatabase.GetAssetPath(assignedTexture) == expectedTexturePath,
                $"{material.name} must use its approved low-poly cohesion texture ({expectedTexturePath}).", failures);
            Require(!material.HasProperty("_Metallic") || material.GetFloat("_Metallic") <= 0.05f,
                $"{material.name} is too metallic for the matte environment style.", failures);
            Require(!material.HasProperty("_Smoothness") || material.GetFloat("_Smoothness") <= 0.2f,
                $"{material.name} is too glossy for the matte environment style.", failures);
        }
    }

    private static void ValidateWardenHoodLiner(GameObject warden, List<string> failures)
    {
        Transform liner = warden.transform.Find(
            "RIG_Cloaked_Guy/root/pelvis/spine_01/spine_02/neck/HoodInteriorLiner");
        Require(liner != null, "Shadow Warden is missing the neck-following hood interior liner.", failures);
        if (liner == null)
        {
            return;
        }

        Require(liner.parent != null && liner.parent.name == "neck",
            "Shadow Warden hood liner must remain parented to the neck bone.", failures);
        Require(liner.localPosition.y >= 0.12f && liner.localPosition.y <= 0.16f,
            "Shadow Warden hood liner must stay raised enough to close the upper hood seam.", failures);
        Require(liner.localScale.x >= 0.35f && liner.localScale.y >= 0.42f && liner.localScale.z >= 0.30f,
            "Shadow Warden hood liner is too small to protect the hood seam from all angles.", failures);

        MeshRenderer renderer = liner.GetComponent<MeshRenderer>();
        Material expectedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Art/Characters/MiniBosses/ShadowWarden/Materials/MAT_ShadowWarden.mat");
        Require(renderer != null && renderer.sharedMaterial == expectedMaterial,
            "Shadow Warden hood liner must use the Warden's opaque double-sided material.", failures);
    }

    private static void Require(bool condition, string message, List<string> failures)
    {
        if (!condition)
        {
            failures.Add(message);
        }
    }

    private static AnimatorState AddState(
        AnimatorStateMachine machine,
        string name,
        AnimationClip clip,
        float speed,
        bool footIk,
        bool useLocomotionPlayback = false)
    {
        if (clip == null)
        {
            throw new InvalidOperationException($"Cannot build Animator state '{name}' because its clip was not found.");
        }

        AnimatorState state = machine.AddState(name);
        state.motion = clip;
        state.speed = speed;
        state.iKOnFeet = footIk;
        state.writeDefaultValues = false;
        if (useLocomotionPlayback)
        {
            state.speedParameterActive = true;
            state.speedParameter = "LocomotionPlayback";
        }

        return state;
    }

    private static void AddLocomotionTransitions(AnimatorState idle, AnimatorState walk)
    {
        AnimatorStateTransition toWalk = idle.AddTransition(walk);
        toWalk.hasExitTime = false;
        toWalk.duration = 0.12f;
        toWalk.AddCondition(AnimatorConditionMode.Greater, 0.08f, "Speed");

        AnimatorStateTransition toIdle = walk.AddTransition(idle);
        toIdle.hasExitTime = false;
        toIdle.duration = 0.14f;
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.08f, "Speed");
    }

    private static void AddAnyTrigger(AnimatorStateMachine machine, AnimatorState destination, string parameter, float duration)
    {
        AnimatorStateTransition transition = machine.AddAnyStateTransition(destination);
        transition.hasExitTime = false;
        transition.duration = duration;
        transition.canTransitionToSelf = false;
        transition.interruptionSource = TransitionInterruptionSource.SourceThenDestination;
        transition.orderedInterruption = true;
        transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
    }

    private static void AddExit(AnimatorState source, AnimatorState destination, float exitTime, float duration)
    {
        AnimatorStateTransition transition = source.AddTransition(destination);
        transition.hasExitTime = true;
        transition.exitTime = exitTime;
        transition.duration = duration;
        transition.hasFixedDuration = true;
    }

    private static AnimationClip LoadClip(string assetPath, string clipName)
    {
        return AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal)
                && string.Equals(clip.name, clipName, StringComparison.OrdinalIgnoreCase));
    }

    private static void AddAnimationContactEvent(string modelPath, string clipName, float normalizedTime)
    {
        ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
        AnimationClip assetClip = LoadClip(modelPath, clipName);
        if (importer == null || assetClip == null)
        {
            throw new InvalidOperationException($"Could not configure animation event for {modelPath}:{clipName}.");
        }

        ModelImporterClipAnimation[] clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0)
        {
            clips = importer.defaultClipAnimations;
        }

        bool changed = false;
        for (int index = 0; index < clips.Length; index++)
        {
            if (!string.Equals(clips[index].name, clipName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            clips[index].events = new[]
            {
                new AnimationEvent
                {
                    functionName = "AnimationAttackContact",
                    time = assetClip.length * Mathf.Clamp01(normalizedTime)
                }
            };
            changed = true;
        }

        if (!changed)
        {
            throw new InvalidOperationException($"Clip definition '{clipName}' was not found in {modelPath}.");
        }

        importer.clipAnimations = clips;
        importer.SaveAndReimport();
    }

    private static void ConfigureScene()
    {
        GameObject player = GameObject.Find("GameRoot/PlayerRoot");
        GameObject bandit = GameObject.Find("SwordBandit_Prototype");
        GameObject warden = GameObject.Find("ShadowWarden");
        if (player == null || bandit == null || warden == null)
        {
            throw new InvalidOperationException("PlayerRoot, SwordBandit_Prototype, and ShadowWarden must all exist in TestScene.");
        }

        ConfigurePlayer(player);
        ConfigureBandit(bandit);
        ConfigureWarden(warden, player);
    }

    private static void ConfigurePlayer(GameObject player)
    {
        Rigidbody rigidbody = player.GetComponent<Rigidbody>();
        if (rigidbody != null)
        {
            Undo.DestroyObjectImmediate(rigidbody);
        }

        CharacterController controller = player.GetComponent<CharacterController>();
        controller.center = new Vector3(0f, 0.95f, 0f);
        controller.height = 1.9f;
        controller.radius = 0.34f;
        controller.stepOffset = 0.28f;
        controller.slopeLimit = 50f;
        controller.skinWidth = 0.04f;
        controller.minMoveDistance = 0f;

        Animator animator = player.GetComponentInChildren<Animator>(true);
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerControllerPath);
        animator.applyRootMotion = false;
        HumanoidAnimationRuntime runtime = EnsureAnimatorRuntime(animator, player.transform);
        SetObjectReference(player.GetComponent<PlayerAnimationDriver>(), "proceduralMotion", runtime);
        PlayerSwordAttack attack = player.GetComponent<PlayerSwordAttack>();
        SetObjectReference(attack, "proceduralMotion", runtime);
        SetFloat(attack, "attackRange", 1.75f);
        SetFloat(attack, "attackRadius", 0.42f);
        SetFloat(attack, "attackCooldown", 0.72f);
        SetFloat(attack, "animationDuration", 0.72f);
        SetFloat(attack, "contactFallbackDelay", 0.36f);
        EnsureReaction(player, animator);
    }

    private static void ConfigureBandit(GameObject bandit)
    {
        Animator animator = bandit.GetComponentInChildren<Animator>(true);
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BanditControllerPath);
        animator.applyRootMotion = false;
        HumanoidAnimationRuntime runtime = EnsureAnimatorRuntime(animator, bandit.transform);
        SetObjectReference(bandit.GetComponent<SwordBanditAnimationDriver>(), "proceduralMotion", runtime);
        SetObjectReference(bandit.GetComponent<SwordBanditAnimationDriver>(), "animator", animator);

        SwordBanditAI ai = bandit.GetComponent<SwordBanditAI>();
        SetBoolean(ai, "useAnimationStrikeEvent", true);
        SetFloat(ai, "windupDuration", 0.55f);
        SetFloat(ai, "strikeDuration", 0.25f);
        SetFloat(ai, "recoveryDuration", 0.58f);
        EnsureReaction(bandit, animator);

        CapsuleCollider collider = bandit.GetComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 0.9f, 0f);
        collider.height = 1.8f;
        collider.radius = 0.34f;
        AttachBanditSword(animator);
    }

    private static void ConfigureWarden(GameObject warden, GameObject player)
    {
        Animator animator = warden.GetComponent<Animator>();
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(WardenControllerPath);
        animator.applyRootMotion = false;
        HumanoidAnimationRuntime runtime = EnsureAnimatorRuntime(animator, warden.transform);

        MiniBossSlowPatrol oldPatrol = warden.GetComponent<MiniBossSlowPatrol>();
        if (oldPatrol != null)
        {
            Undo.DestroyObjectImmediate(oldPatrol);
        }

        NavMeshAgent agent = GetOrAdd<NavMeshAgent>(warden);
        agent.speed = 1.35f;
        agent.acceleration = 6f;
        agent.angularSpeed = 320f;
        agent.stoppingDistance = 1.68f;
        agent.radius = 0.45f;
        agent.height = 2f;
        agent.baseOffset = 0f;

        CapsuleCollider collider = GetOrAdd<CapsuleCollider>(warden);
        collider.center = new Vector3(0f, 1f, 0f);
        collider.height = 2f;
        collider.radius = 0.45f;

        EnemyHealth health = GetOrAdd<EnemyHealth>(warden);
        SetFloat(health, "maximumHealth", 160f);
        SetFloat(health, "startingHealth", 160f);
        SetFloat(health, "destroyDelay", 4f);

        ShadowWardenAI ai = GetOrAdd<ShadowWardenAI>(warden);
        SetObjectReference(ai, "target", player.transform);
        SetObjectReference(ai, "targetVitals", player.GetComponent<PlayerVitals>());
        SetObjectReference(ai, "animator", animator);
        SetObjectReference(ai, "proceduralMotion", runtime);
        EnsureReaction(warden, animator);
    }

    private static HumanoidAnimationRuntime EnsureAnimatorRuntime(Animator animator, Transform characterRoot)
    {
        CharacterAnimationEventRelay relay = GetOrAdd<CharacterAnimationEventRelay>(animator.gameObject);
        HumanoidAnimationRuntime runtime = GetOrAdd<HumanoidAnimationRuntime>(animator.gameObject);
        SetObjectReference(runtime, "animator", animator);
        SetObjectReference(runtime, "characterRoot", characterRoot);
        return runtime;
    }

    private static void EnsureReaction(GameObject root, Animator animator)
    {
        CombatHitReaction reaction = GetOrAdd<CombatHitReaction>(root);
        SetObjectReference(reaction, "animator", animator);
    }

    private static void AttachBanditSword(Animator animator)
    {
        Transform hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        if (hand == null)
        {
            Debug.LogWarning("[CharacterAnimationOverhaul] Bandit has no mapped right hand; sword attachment skipped.");
            return;
        }

        Transform existing = hand.Find("BanditSword");
        if (existing != null)
        {
            return;
        }

        GameObject swordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Art/Characters/Bandits/SwordBandit/Prefabs/P_BanditSword.prefab");
        if (swordPrefab == null)
        {
            throw new InvalidOperationException("P_BanditSword prefab is missing.");
        }

        GameObject sword = PrefabUtility.InstantiatePrefab(swordPrefab, hand) as GameObject;
        sword.name = "BanditSword";
        sword.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        sword.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        sword.transform.localScale = Vector3.one * 1.2f;
        foreach (Collider collider in sword.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }
    }

    private static void ConfigureMaterials()
    {
        ConfigurePlayerMaterial();
        AssignBaseTexture(
            "Assets/Art/Characters/Bandits/SwordBandit/Materials/MAT_HoodedSwordBandit.mat",
            BanditCohesionTexturePath,
            "Assets/Art/Characters/Bandits/SwordBandit/Textures/T_HoodedSwordBandit_BaseColor.png");
        TuneMaterial(
            "Assets/Art/Characters/Bandits/SwordBandit/Materials/MAT_HoodedSwordBandit.mat",
            metallic: 0f,
            smoothness: 0.16f,
            baseTint: Color.white);
        AssignBaseTexture(
            "Assets/Art/Characters/MiniBosses/ShadowWarden/Materials/MAT_ShadowWarden.mat",
            WardenCohesionTexturePath,
            "Assets/Art/Characters/MiniBosses/ShadowWarden/Textures/T_ShadowWarden_BaseColor.png");
        TuneMaterial(
            "Assets/Art/Characters/MiniBosses/ShadowWarden/Materials/MAT_ShadowWarden.mat",
            metallic: 0.02f,
            smoothness: 0.12f,
            baseTint: Color.white);
    }

    private static void ConfigurePlayerMaterial()
    {
        GameObject playerVisual = GameObject.Find("GameRoot/PlayerRoot/PlayerVisual_Hy3D");
        SkinnedMeshRenderer renderer = playerVisual.GetComponentInChildren<SkinnedMeshRenderer>(true);
        const string path = "Assets/Art/Characters/Player/Materials/MAT_Player_Hy3D.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = renderer.sharedMaterial != null
                ? new Material(renderer.sharedMaterial)
                : new Material(Shader.Find("HDRP/Lit"));
            material.name = "MAT_Player_Hy3D";
            AssetDatabase.CreateAsset(material, path);
        }

        AssignBaseTexture(
            material,
            PlayerCohesionTexturePath,
            "Assets/Models/Hy3D/Hy3D_rigged_mia.fbm/Image_0.png");

        renderer.sharedMaterial = material;
        TuneMaterial(path, 0f, 0.15f, Color.white);
    }

    private static void AssignBaseTexture(string materialPath, string preferredTexturePath, string fallbackTexturePath)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            throw new InvalidOperationException($"Material not found: {materialPath}");
        }

        AssignBaseTexture(material, preferredTexturePath, fallbackTexturePath);
    }

    private static void AssignBaseTexture(Material material, string preferredTexturePath, string fallbackTexturePath)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(preferredTexturePath)
            ?? AssetDatabase.LoadAssetAtPath<Texture2D>(fallbackTexturePath);
        if (texture == null)
        {
            throw new InvalidOperationException(
                $"Neither the cohesion texture nor fallback texture could be loaded for {material.name}.");
        }

        if (material.HasProperty("_BaseColorMap"))
        {
            material.SetTexture("_BaseColorMap", texture);
            EditorUtility.SetDirty(material);
        }
    }

    private static void TuneMaterial(string path, float metallic, float smoothness, Color baseTint)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            throw new InvalidOperationException($"Material not found: {path}");
        }

        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_CoatMask")) material.SetFloat("_CoatMask", 0f);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", baseTint);
        EditorUtility.SetDirty(material);
    }

    private static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(gameObject);
    }

    private static void SetObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            throw new InvalidOperationException($"{target.GetType().Name}.{propertyName} was not found.");
        }

        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetBoolean(UnityEngine.Object target, string propertyName, bool value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        serializedObject.FindProperty(propertyName).boolValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(UnityEngine.Object target, string propertyName, float value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        serializedObject.FindProperty(propertyName).floatValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }
}
