using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the imported Clothy character into the project's reusable Warden prefab
/// and installs a scene instance that uses the existing deliberate boss walk.
/// The separate cloth renderer is intentionally limited to the generated garment
/// layers; rigid armor and the body mesh stay outside the simulation.
/// </summary>
public static class ClothyShadowWardenInstaller
{
    private const string ScenePath = "Assets/TestScene.unity";
    private const string Root = "Assets/Art/Characters/MiniBosses/ShadowWarden";
    private const string ModelPath = Root + "/Models/SM_ShadowWarden_OriginalRig.fbx";
    private const string ControllerPath = Root + "/Animations/AC_ShadowWarden.controller";
    private const string BodyMaterialPath = Root + "/Materials/MAT_ShadowWarden.mat";
    private const string ClothMaterialPath = Root + "/Materials/MAT_ShadowWarden_Cloth.mat";
    private const string PrefabPath = Root + "/Prefabs/P_ShadowWarden.prefab";
    private const string BodyTexturePath = Root + "/Textures/T_ShadowWarden_Cohesion_BaseColor.png";
    private const string ClothTexturePath = Root + "/Textures/T_ShadowWarden_Cloth_BaseColor.png";
    private const string IdlePath = "Assets/Models/Hy3D/Animations/Idle.fbx";
    private const string WalkPath = "Assets/Models/Hy3D/Animations/Walk.fbx";
    private const string PunchPath = "Assets/Models/Hy3D/Animations/Punch.fbx";

    [MenuItem("Tools/Main Hackathon Game/Install Clothy Shadow Warden")]
    public static void Install()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            throw new InvalidOperationException($"Open {ScenePath} before installing the Clothy Shadow Warden.");
        }

        SaveSafetyCopy(scene);
        RemoveStaleSceneWarden();
        EnsureFolders();
        ConfigureModelImporter();

        AnimatorController controller = BuildController();
        Material bodyMaterial = BuildMaterial(BodyMaterialPath, BodyTexturePath, "MAT_ShadowWarden", false);
        GameObject prefab = BuildPrefab(controller, bodyMaterial);
        GameObject sceneWarden = ReplaceSceneInstance(prefab);

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[ClothyShadowWarden] Installed '{sceneWarden.name}' with a separate Unity Cloth garment renderer.");
    }

    [MenuItem("Tools/Main Hackathon Game/Validate Clothy Shadow Warden")]
    public static void ValidateInstall()
    {
        List<string> failures = new List<string>();
        ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        Require(importer != null && importer.animationType == ModelImporterAnimationType.Human, "The Warden model is not Humanoid.", failures);
        Require(AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null, "The Warden Animator Controller is missing.", failures);
        Require(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null, "The Warden prefab is missing.", failures);
        Require(AssetDatabase.LoadAssetAtPath<Material>(BodyMaterialPath) != null, "The Warden body material is missing.", failures);

        GameObject warden = GameObject.Find("ShadowWarden");
        Require(warden != null, "ShadowWarden is missing from the active scene.", failures);
        if (warden != null)
        {
            Animator animator = warden.GetComponentInChildren<Animator>(true);
            SkinnedMeshRenderer sourceRenderer = warden.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Require(animator != null && animator.isHuman && animator.avatar != null && animator.avatar.isValid,
                "The Warden Animator is not using a valid Humanoid Avatar.", failures);
            Require(animator != null && animator.runtimeAnimatorController == AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath),
                "The Warden Animator is not using its dedicated controller.", failures);
            Require(warden.GetComponent<NavMeshAgent>() != null, "The Warden has no NavMeshAgent.", failures);
            Require(warden.GetComponent<ShadowWardenAI>() != null, "The Warden has no ShadowWardenAI.", failures);
            Require(warden.GetComponent<EnemyHealth>() != null, "The Warden has no EnemyHealth.", failures);
            Require(sourceRenderer != null && sourceRenderer.sharedMaterial == AssetDatabase.LoadAssetAtPath<Material>(BodyMaterialPath),
                "The clean source mesh is not using the dedicated full-colour material.", failures);
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException("Clothy Shadow Warden validation failed:\n- " + string.Join("\n- ", failures));
        }

        Debug.Log("[ClothyShadowWarden] VALIDATION PASSED: clean source geometry, full-colour material, humanoid rig, controller, prefab, and scene wiring are valid.");
    }

    private static void SaveSafetyCopy(Scene scene)
    {
        try
        {
            string projectPath = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            string folder = Path.Combine(projectPath, "Library", "CodexSceneBackups");
            Directory.CreateDirectory(folder);
            string backup = Path.Combine(folder, $"TestScene-before-clothy-{DateTime.UtcNow:yyyyMMdd-HHmmss}.unity");
            EditorSceneManager.SaveScene(scene, backup, true);
            Debug.Log($"[ClothyShadowWarden] Safety copy written to {backup}.");
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[ClothyShadowWarden] Could not create a non-destructive scene safety copy: {exception.Message}");
        }
    }

    private static void EnsureFolders()
    {
        foreach (string folder in new[] { Root, Root + "/Models", Root + "/Animations", Root + "/Materials", Root + "/Prefabs", Root + "/Textures" })
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
                string name = Path.GetFileName(folder);
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }

    private static void RemoveStaleSceneWarden()
    {
        GameObject existing = GameObject.Find("ShadowWarden");
        if (existing != null)
        {
            // A previous interrupted install leaves only an unconfigured model
            // instance. Remove that known generated residue before rebuilding.
            UnityEngine.Object.DestroyImmediate(existing);
        }
    }

    private static void ConfigureModelImporter()
    {
        ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (importer == null)
        {
            throw new InvalidOperationException($"The prepared model is missing at {ModelPath}.");
        }

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = false;
        importer.optimizeGameObjects = false;
        importer.isReadable = true;
        importer.importCameras = false;
        importer.importLights = false;
        importer.SaveAndReimport();
    }

    private static AnimatorController BuildController()
    {
        AnimationClip idle = LoadClip(IdlePath, "Idle");
        AnimationClip walk = LoadClip(WalkPath, "Walk");
        AnimationClip punch = LoadClip(PunchPath, "Punch");
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }

        controller.parameters = new[]
        {
            FloatParameter("Speed", 0f),
            FloatParameter("LocomotionPlayback", 0.65f),
            BoolParameter("Grounded", true),
            TriggerParameter("Attack"),
            TriggerParameter("Hit"),
            TriggerParameter("Die"),
        };

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        foreach (ChildAnimatorState child in machine.states.ToArray())
        {
            machine.RemoveState(child.state);
        }

        foreach (AnimatorStateTransition transition in machine.anyStateTransitions.ToArray())
        {
            machine.RemoveAnyStateTransition(transition);
        }

        AnimatorState idleState = AddState(machine, "Idle", idle, true, 1f);
        AnimatorState walkState = AddState(machine, "Walk", walk, true, 1f);
        walkState.speedParameterActive = true;
        walkState.speedParameter = "LocomotionPlayback";
        AnimatorState attackState = AddState(machine, "Attack", punch, false, 0.72f);
        AnimatorState hitState = AddState(machine, "Hit Reaction", punch, false, 0.48f);
        AnimatorState deathState = AddState(machine, "Death", idle, false, 0.35f);
        machine.defaultState = idleState;

        AddFloatTransition(idleState, walkState, "Speed", AnimatorConditionMode.Greater, 0.04f, 0.10f);
        AddFloatTransition(walkState, idleState, "Speed", AnimatorConditionMode.Less, 0.03f, 0.12f);
        AddTriggerTransition(machine, attackState, "Attack", 0.05f);
        AddTriggerTransition(machine, hitState, "Hit", 0.04f);
        AddTriggerTransition(machine, deathState, "Die", 0.04f);
        AddExitTransition(attackState, idleState, 0.92f, 0.10f);
        AddExitTransition(hitState, idleState, 0.88f, 0.10f);

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static Material BuildMaterial(string path, string texturePath, string materialName, bool doubleSided)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("HDRP/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("HDRP/Lit shader is unavailable.");
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture == null)
        {
            throw new InvalidOperationException($"Texture is missing at {texturePath}.");
        }

        material.name = materialName;
        material.SetTexture("_BaseColorMap", texture);
        SetFloatIfPresent(material, "_Metallic", 0f);
        SetFloatIfPresent(material, "_Smoothness", doubleSided ? 0.28f : 0.22f);
        SetFloatIfPresent(material, "_DoubleSidedEnable", doubleSided ? 1f : 0f);
        SetFloatIfPresent(material, "_DoubleSidedNormalMode", 1f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject BuildPrefab(AnimatorController controller, Material bodyMaterial)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null)
        {
            throw new InvalidOperationException($"The prepared model cannot be loaded from {ModelPath}.");
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
        instance.name = "ShadowWarden";
        Animator animator = instance.GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            UnityEngine.Object.DestroyImmediate(instance);
            throw new InvalidOperationException("The prepared Warden model has no Animator.");
        }

        // The third-party Humanoid clips contain root/bone axes that are not
        // compatible with this generated rig. Keep the character in its valid
        // bind pose until a native-bone walk clip is authored; otherwise Unity
        // can propel the visual mesh far above the NavMesh while its capsule
        // remains at the origin.
        animator.runtimeAnimatorController = controller;
        animator.enabled = false;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.updateMode = AnimatorUpdateMode.Normal;

        SkinnedMeshRenderer bodyRenderer = FindRenderer(instance, "ShadowWarden_OriginalMesh");
        bodyRenderer.sharedMaterial = bodyMaterial;
        GetOrAdd<CharacterAnimationEventRelay>(animator.gameObject);
        AlignVisualFeetToNavMesh(instance.transform);
        GetOrAdd<ShadowWardenNativeWalk>(instance);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
        UnityEngine.Object.DestroyImmediate(instance);
        return prefab;
    }

    private static GameObject ReplaceSceneInstance(GameObject prefab)
    {
        GameObject existing = GameObject.Find("ShadowWarden");
        if (existing != null)
        {
            UnityEngine.Object.DestroyImmediate(existing);
        }

        GameObject warden = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        warden.name = "ShadowWarden";
        warden.transform.SetPositionAndRotation(new Vector3(-4f, 0.02f, 2f), Quaternion.Euler(0f, 180f, 0f));

        CapsuleCollider capsule = GetOrAdd<CapsuleCollider>(warden);
        capsule.center = new Vector3(0f, 0.95f, 0f);
        capsule.radius = 0.42f;
        capsule.height = 1.9f;

        NavMeshAgent agent = GetOrAdd<NavMeshAgent>(warden);
        agent.radius = 0.42f;
        agent.height = 1.75f;
        agent.baseOffset = -0.14f;
        agent.speed = 1.35f;
        agent.angularSpeed = 320f;
        agent.acceleration = 6f;

        GetOrAdd<EnemyHealth>(warden);
        GetOrAdd<CombatHitReaction>(warden);
        ShadowWardenAI ai = GetOrAdd<ShadowWardenAI>(warden);
        PlayerVitals vitals = GameObject.Find("GameRoot/PlayerRoot")?.GetComponent<PlayerVitals>();
        if (vitals == null)
        {
            throw new InvalidOperationException("PlayerVitals is missing from GameRoot/PlayerRoot.");
        }

        ai.ConfigureTarget(vitals.transform, vitals);
        return warden;
    }

    private static void AlignVisualFeetToNavMesh(Transform root)
    {
        // This source mesh's imported bind pose sits 14 cm above its animation
        // root. Offset only the visual hierarchy so navigation stays grounded
        // while the boots contact the baked NavMesh.
        const float visualGroundOffset = 1.10f;
        foreach (Transform child in root)
        {
            child.localPosition += Vector3.up * visualGroundOffset;
        }
    }

    private static void ConfigureCloth(SkinnedMeshRenderer renderer, Animator animator)
    {
        Cloth cloth = GetOrAdd<Cloth>(renderer.gameObject);
        Mesh mesh = renderer.sharedMesh;
        Vector3 upAxis = FindLocalUpAxis(renderer.transform);
        float minHeight = float.PositiveInfinity;
        float maxHeight = float.NegativeInfinity;
        foreach (Vector3 vertex in mesh.vertices)
        {
            float height = Vector3.Dot(vertex, upAxis);
            minHeight = Mathf.Min(minHeight, height);
            maxHeight = Mathf.Max(maxHeight, height);
        }

        float range = Mathf.Max(0.0001f, maxHeight - minHeight);
        ClothSkinningCoefficient[] coefficients = new ClothSkinningCoefficient[mesh.vertexCount];
        for (int index = 0; index < mesh.vertexCount; index++)
        {
            float normalizedHeight = Mathf.InverseLerp(minHeight, maxHeight, Vector3.Dot(mesh.vertices[index], upAxis));
            float maxDistance = normalizedHeight >= 0.84f
                ? 0f
                : Mathf.Lerp(0.30f, 0.045f, Mathf.InverseLerp(0f, 0.84f, normalizedHeight));
            coefficients[index] = new ClothSkinningCoefficient
            {
                maxDistance = maxDistance,
                collisionSphereDistance = 0.012f,
            };
        }

        cloth.coefficients = coefficients;
        cloth.useGravity = true;
        cloth.damping = 0.36f;
        cloth.friction = 0.20f;
        cloth.worldVelocityScale = 0.16f;
        cloth.worldAccelerationScale = 0.06f;
        cloth.clothSolverFrequency = 90f;
        cloth.sleepThreshold = 0.015f;
        cloth.enableContinuousCollision = true;
        cloth.useVirtualParticles = 1f;
        cloth.selfCollisionDistance = 0.018f;
        cloth.selfCollisionStiffness = 0.65f;
        cloth.sphereColliders = new[]
        {
            new ClothSphereColliderPair(CreateSphereCollider(animator, HumanBodyBones.Hips, "ClothCollision_Hips", 0.31f)),
            new ClothSphereColliderPair(CreateSphereCollider(animator, HumanBodyBones.Spine, "ClothCollision_Spine", 0.26f)),
            new ClothSphereColliderPair(CreateSphereCollider(animator, HumanBodyBones.LeftUpperLeg, "ClothCollision_LeftThigh", 0.21f)),
            new ClothSphereColliderPair(CreateSphereCollider(animator, HumanBodyBones.RightUpperLeg, "ClothCollision_RightThigh", 0.21f)),
        };
    }

    private static Vector3 FindLocalUpAxis(Transform transform)
    {
        Vector3[] candidates = { Vector3.right, Vector3.up, Vector3.forward, Vector3.left, Vector3.down, Vector3.back };
        return candidates.OrderByDescending(candidate => Vector3.Dot(transform.TransformDirection(candidate).normalized, Vector3.up)).First();
    }

    private static SphereCollider CreateSphereCollider(Animator animator, HumanBodyBones bone, string name, float radius)
    {
        Transform parent = animator.GetBoneTransform(bone);
        if (parent == null)
        {
            throw new InvalidOperationException($"Required Humanoid bone {bone} is not mapped.");
        }

        Transform existing = parent.Find(name);
        GameObject child = existing != null ? existing.gameObject : new GameObject(name);
        child.transform.SetParent(parent, false);
        SphereCollider collider = GetOrAdd<SphereCollider>(child);
        collider.radius = radius;
        collider.center = Vector3.zero;
        return collider;
    }

    private static SkinnedMeshRenderer FindRenderer(GameObject root, string name)
    {
        SkinnedMeshRenderer renderer = root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
            .FirstOrDefault(candidate => candidate.name == name);
        if (renderer == null)
        {
            throw new InvalidOperationException($"Skinned renderer '{name}' is missing from the prepared model.");
        }

        return renderer;
    }

    private static AnimationClip LoadClip(string path, string name)
    {
        AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<AnimationClip>()
            .FirstOrDefault(candidate => candidate.name == name);
        if (clip == null)
        {
            throw new InvalidOperationException($"Animation clip '{name}' is missing at {path}.");
        }

        return clip;
    }

    private static AnimatorState AddState(AnimatorStateMachine machine, string name, Motion motion, bool loop, float speed)
    {
        AnimatorState state = machine.AddState(name);
        state.motion = motion;
        state.speed = speed;
        state.iKOnFeet = true;
        state.writeDefaultValues = true;
        return state;
    }

    private static void AddFloatTransition(AnimatorState from, AnimatorState to, string parameter, AnimatorConditionMode mode, float threshold, float duration)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = duration;
        transition.AddCondition(mode, threshold, parameter);
    }

    private static void AddTriggerTransition(AnimatorStateMachine machine, AnimatorState to, string parameter, float duration)
    {
        AnimatorStateTransition transition = machine.AddAnyStateTransition(to);
        transition.hasExitTime = false;
        transition.duration = duration;
        transition.canTransitionToSelf = false;
        transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
    }

    private static void AddExitTransition(AnimatorState from, AnimatorState to, float exitTime, float duration)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = true;
        transition.exitTime = exitTime;
        transition.duration = duration;
    }

    private static AnimatorControllerParameter FloatParameter(string name, float value) => new AnimatorControllerParameter
    {
        name = name,
        type = AnimatorControllerParameterType.Float,
        defaultFloat = value,
    };

    private static AnimatorControllerParameter BoolParameter(string name, bool value) => new AnimatorControllerParameter
    {
        name = name,
        type = AnimatorControllerParameterType.Bool,
        defaultBool = value,
    };

    private static AnimatorControllerParameter TriggerParameter(string name) => new AnimatorControllerParameter
    {
        name = name,
        type = AnimatorControllerParameterType.Trigger,
    };

    private static void SetFloatIfPresent(Material material, string property, float value)
    {
        if (material.HasProperty(property))
        {
            material.SetFloat(property, value);
        }
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T existing = target.GetComponent<T>();
        if (existing != null)
        {
            return existing;
        }

        // Editor Undo creates a fully registered component immediately. This is
        // important for Unity's legacy Cloth component, whose native state can
        // otherwise be deferred when added to an imported model instance.
        T added = Undo.AddComponent(target, typeof(T)) as T;
        if (added == null || target.GetComponent<T>() == null)
        {
            throw new InvalidOperationException($"Unity did not attach {typeof(T).Name} to {target.name}.");
        }

        return added;
    }

    private static void Require(bool condition, string message, ICollection<string> failures)
    {
        if (!condition)
        {
            failures.Add(message);
        }
    }
}
