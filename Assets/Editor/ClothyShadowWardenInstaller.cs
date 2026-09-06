using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Repeatably builds the authored Shadow Warden model into a mobile-oriented
/// cloth prefab, then installs the configured gameplay instance in TestScene.
/// </summary>
public static class ClothyShadowWardenInstaller
{
    private const string ScenePath = "Assets/TestScene.unity";
    private const string Root = "Assets/Art/Characters/MiniBosses/ShadowWarden";
    private const string ModelPath = Root + "/Models/SM_ShadowWarden.fbx";
    private const string ControllerPath = Root + "/Animations/AC_ShadowWarden.controller";
    private const string BodyMaterialPath = Root + "/Materials/MAT_ShadowWarden.mat";
    private const string ClothMaterialPath = Root + "/Materials/MAT_ShadowWarden_Cloth.mat";
    private const string ReadableBodyMaterialPath = Root + "/Materials/MAT_ShadowWarden_Readable.mat";
    private const string ReadableClothMaterialPath = Root + "/Materials/MAT_ShadowWarden_Cloth_Readable.mat";
    private const string BackRepairMeshPath = Root + "/Models/ShadowWarden_BackOpeningRepair.asset";
    private const string StraightBodyMeshPath = Root + "/Models/ShadowWarden_Body_StraightBack.asset";
    private const string PrefabPath = Root + "/Prefabs/P_ShadowWarden.prefab";
    private const string BodyTexturePath = Root + "/Textures/T_ShadowWarden_Cohesion_BaseColor.png";
    private const string ClothTexturePath = Root + "/Textures/T_ShadowWarden_Cloth_BaseColor.png";
    private const string IdlePath = "Assets/Models/Hy3D/Animations/Idle.fbx";
    private const string WalkPath = "Assets/Models/Hy3D/Animations/Walk.fbx";
    private const string PunchPath = "Assets/Models/Hy3D/Animations/Punch.fbx";
    private const string BodyRendererName = "ShadowWarden_Body";
    private const string ClothRendererName = "ShadowWarden_ClothLayers";
    private const string CollisionPrefix = "CapeCollision_";
    private const int ExpectedClothParticleCount = 5594;
    private const int ExpectedClothIslandCount = 3;
    private const float VisualGroundOffset = 1.18f;

    [MenuItem("Tools/Main Hackathon Game/Install Clothy Shadow Warden")]
    public static void Install()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            throw new InvalidOperationException($"Open {ScenePath} before installing the Clothy Shadow Warden.");
        }

        EnsureFolders();
        ConfigureModelImporter();
        AnimatorController controller = BuildController();
        Material bodyBaseMaterial = BuildMaterial(BodyMaterialPath, BodyTexturePath, "MAT_ShadowWarden", false);
        Material clothBaseMaterial = BuildMaterial(ClothMaterialPath, ClothTexturePath, "MAT_ShadowWarden_Cloth", true);
        Material bodyMaterial = BuildReadableMaterial(bodyBaseMaterial, ReadableBodyMaterialPath, "MAT_ShadowWarden_Readable", false);
        Material clothMaterial = BuildReadableMaterial(clothBaseMaterial, ReadableClothMaterialPath, "MAT_ShadowWarden_Cloth_Readable", true);
        GameObject prefab = BuildPrefab(controller, bodyMaterial, clothMaterial);
        Animator prefabAnimator = prefab.GetComponentInChildren<Animator>(true);
        if (prefabAnimator == null)
        {
            throw new InvalidOperationException("The rebuilt Warden prefab has no Animator.");
        }
        prefabAnimator.enabled = true;
        EditorUtility.SetDirty(prefabAnimator);

        // Do not disturb the playable scene until all source and prefab work has
        // succeeded, then retain a recoverable copy immediately before replacing.
        SaveSafetyCopy(scene);
        GameObject sceneWarden = ReplaceSceneInstance(prefab);
        Animator sceneAnimator = sceneWarden.GetComponentInChildren<Animator>(true);
        sceneAnimator.enabled = true;
        PrefabUtility.RecordPrefabInstancePropertyModifications(sceneAnimator);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
        ValidateInstall();
        Debug.Log($"[ClothyShadowWarden] Installed '{sceneWarden.name}' with {ExpectedClothParticleCount} welded particles across {ExpectedClothIslandCount} pinned garment islands.");
    }

    [MenuItem("Tools/Main Hackathon Game/Rebuild + Validate Shadow Warden Cape")]
    public static void RebuildAndValidate()
    {
        Install();
        PrepareVisualCheckpoint();
    }

    [MenuItem("Tools/Main Hackathon Game/Validate Clothy Shadow Warden")]
    public static void ValidateInstall()
    {
        List<string> failures = new List<string>();
        ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        Require(importer != null && importer.animationType == ModelImporterAnimationType.Human,
            "The Warden model is not Humanoid.", failures);
        Require(importer != null && importer.importAnimation,
            "The Warden native animation takes are not imported.", failures);
        Require(AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null,
            "The Warden Animator Controller is missing.", failures);
        Require(AssetDatabase.LoadAssetAtPath<Material>(BodyMaterialPath) != null,
            "The Warden body material is missing.", failures);
        Require(AssetDatabase.LoadAssetAtPath<Material>(ClothMaterialPath) != null,
            "The Warden cloth material is missing.", failures);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Require(prefab != null, "The Warden prefab is missing.", failures);
        if (prefab != null)
        {
            ValidateWarden(prefab, "Prefab", failures, false);
        }

        GameObject warden = GameObject.Find("ShadowWarden");
        Require(warden != null, "ShadowWarden is missing from the active scene.", failures);
        if (warden != null)
        {
            ValidateWarden(warden, "Scene", failures, true);
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException("Clothy Shadow Warden validation failed:\n- " + string.Join("\n- ", failures));
        }

        Debug.Log($"[ClothyShadowWarden] VALIDATION PASSED: one attached {ExpectedClothParticleCount}-particle cape, all three islands pinned, isolated collision proxies, valid materials/rig, grounded feet, and complete gameplay wiring.");
    }

    [MenuItem("Tools/Main Hackathon Game/Self-Test Shadow Warden Cape Validator")]
    public static void SelfTestValidator()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            throw new InvalidOperationException("Install the Warden prefab before running its validator self-test.");
        }

        Scene previewScene = EditorSceneManager.NewPreviewScene();
        try
        {
            AssertValidatorAccepts(previewScene, prefab);
            AssertValidatorRejects(previewScene, prefab, "duplicate Cloth", "exactly one Cloth", root =>
            {
                GameObject duplicate = new GameObject("DuplicateCapeForValidation");
                duplicate.transform.SetParent(root.transform, false);
                duplicate.AddComponent<SkinnedMeshRenderer>();
                duplicate.AddComponent<Cloth>();
            });
            AssertValidatorRejects(previewScene, prefab, "missing collision proxy", "six isolated cape collision proxies", root =>
            {
                SphereCollider proxy = root.GetComponentsInChildren<SphereCollider>(true)
                    .First(collider => collider.name.StartsWith(CollisionPrefix, StringComparison.Ordinal));
                UnityEngine.Object.DestroyImmediate(proxy.gameObject);
            });
            AssertValidatorRejects(previewScene, prefab, "disabled collision proxy", "enabled", root =>
                root.GetComponentsInChildren<SphereCollider>(true)
                    .First(collider => collider.name.StartsWith(CollisionPrefix, StringComparison.Ordinal)).enabled = false);
            AssertValidatorRejects(previewScene, prefab, "badly scaled collision proxy", "world-space radius", root =>
                root.GetComponentsInChildren<SphereCollider>(true)
                    .First(collider => collider.name.StartsWith(CollisionPrefix, StringComparison.Ordinal)).transform.localScale = Vector3.one * 10f);
            AssertValidatorRejects(previewScene, prefab, "missing cape root bone", "garment bone binding", root =>
                FindRenderer(root, ClothRendererName).rootBone = null);
            AssertValidatorRejects(previewScene, prefab, "missing lifecycle component", "lifecycle/reset component", root =>
                UnityEngine.Object.DestroyImmediate(root.GetComponentInChildren<ShadowWardenCapeRuntime>(true)));
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(previewScene);
        }

        Debug.Log("[ClothyShadowWarden] VALIDATOR SELF-TEST PASSED: duplicate cloth, missing proxy, missing bone, and missing lifecycle wiring were all rejected without touching TestScene.");
    }

    [MenuItem("Tools/Main Hackathon Game/Frame Shadow Warden Cape Checkpoint")]
    public static void PrepareVisualCheckpoint()
    {
        GameObject warden = GameObject.Find("ShadowWarden");
        if (warden == null)
        {
            throw new InvalidOperationException("ShadowWarden is missing from the active scene.");
        }

        // Frame the complete gameplay root so the checkpoint proves lower-body
        // geometry and feet are present, not just the cape bounds.
        Selection.activeGameObject = warden;
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null)
        {
            sceneView.FrameSelected();
            sceneView.Repaint();
        }

        Debug.Log("[ClothyShadowWarden] Cape checkpoint framed. Enter Play Mode to inspect chase, sharp turns, attacks, impacts, and recovery.");
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
            if (AssetDatabase.IsValidFolder(folder))
            {
                continue;
            }

            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }

    private static void ConfigureModelImporter()
    {
        ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (importer == null)
        {
            throw new InvalidOperationException($"The prepared model is missing at {ModelPath}.");
        }

        bool changed = importer.animationType != ModelImporterAnimationType.Human
            || importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel
            || !importer.importAnimation
            || importer.optimizeGameObjects
            || !importer.isReadable
            || importer.importCameras
            || importer.importLights;
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.optimizeGameObjects = false;
        importer.isReadable = true;
        importer.importCameras = false;
        importer.importLights = false;
        if (changed)
        {
            importer.SaveAndReimport();
        }
    }

    private static AnimatorController BuildController()
    {
        AnimationClip idle = LoadNativeClip("SW_Idle") ?? LoadClip(IdlePath, "Idle");
        AnimationClip walk = LoadNativeClip("SW_Walk") ?? LoadClip(WalkPath, "Walk");
        AnimationClip attack = LoadNativeClip("SW_Flex_Test") ?? LoadClip(PunchPath, "Punch");
        AnimationClip hit = LoadNativeClip("SW_Flex_Test") ?? LoadClip(PunchPath, "Punch");
        AnimationClip death = LoadNativeClip("SW_Turn") ?? idle;
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

        AnimatorState idleState = AddState(machine, "Idle", idle, 1f);
        AnimatorState walkState = AddState(machine, "Walk", walk, 1f);
        walkState.speedParameterActive = true;
        walkState.speedParameter = "LocomotionPlayback";
        AnimatorState attackState = AddState(machine, "Attack", attack, 0.72f);
        AnimatorState hitState = AddState(machine, "Hit Reaction", hit, 0.90f);
        AnimatorState deathState = AddState(machine, "Death", death, 0.90f);
        machine.defaultState = idleState;

        AddFloatTransition(idleState, walkState, "Speed", AnimatorConditionMode.Greater, 0.04f, 0.10f);
        AddFloatTransition(walkState, idleState, "Speed", AnimatorConditionMode.Less, 0.03f, 0.12f);
        AddTriggerTransition(machine, attackState, "Attack", 0.05f);
        AddTriggerTransition(machine, hitState, "Hit", 0.04f);
        AddTriggerTransition(machine, deathState, "Die", 0.04f);
        AddExitTransition(attackState, idleState, 0.92f, 0.10f);
        AddExitTransition(hitState, idleState, 0.88f, 0.10f);
        return controller;
    }

    private static AnimationClip LoadNativeClip(string clipName)
    {
        return AssetDatabase.LoadAllAssetsAtPath(ModelPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(clip => clip.name == clipName);
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
        SetFloatIfPresent(material, "_Smoothness", doubleSided ? 0.10f : 0.20f);
        SetFloatIfPresent(material, "_DoubleSidedEnable", doubleSided ? 1f : 0f);
        SetFloatIfPresent(material, "_DoubleSidedNormalMode", 1f);
        SetFloatIfPresent(material, "_TransmissionEnable", 0f);
        material.doubleSidedGI = doubleSided;
        if (doubleSided)
        {
            material.EnableKeyword("_DOUBLESIDED_ON");
        }
        else
        {
            material.DisableKeyword("_DOUBLESIDED_ON");
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material BuildReadableMaterial(Material source, string path, string materialName, bool cloth)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(source);
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.CopyPropertiesFromMaterial(source);
        }

        material.name = materialName;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", cloth
                ? new Color(1.65f, 1.85f, 2.25f, 1f)
                : new Color(2.15f, 1.90f, 1.65f, 1f));
        }
        SetFloatIfPresent(material, "_DoubleSidedEnable", 1f);
        SetFloatIfPresent(material, "_DoubleSidedNormalMode", 1f);
        material.doubleSidedGI = true;
        material.EnableKeyword("_DOUBLESIDED_ON");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject BuildPrefab(AnimatorController controller, Material bodyMaterial, Material clothMaterial)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null)
        {
            throw new InvalidOperationException($"The prepared model cannot be loaded from {ModelPath}.");
        }

        GameObject wrapper = new GameObject("ShadowWarden");
        try
        {
            GameObject visual = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (visual == null)
            {
                throw new InvalidOperationException("Unity could not instantiate the prepared Warden model.");
            }

            // The FBX arrives as a nested prefab whose Animator can carry the
            // source asset's disabled edit-mode state. Unpack this generated
            // derivative before authoring overrides so the enabled controller
            // and repair meshes persist in our own prefab safely.
            PrefabUtility.UnpackPrefabInstance(visual, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            visual.name = "ShadowWarden_Visual";
            visual.transform.SetParent(wrapper.transform, false);
            visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            visual.transform.localScale = Vector3.one;

            Animator animator = visual.GetComponentInChildren<Animator>(true);
            if (animator == null || !animator.isHuman || animator.avatar == null || !animator.avatar.isValid)
            {
                throw new InvalidOperationException("The prepared Warden model has no valid Humanoid Animator.");
            }

            // The source model carries its own full-body Humanoid takes. Keep
            // the controller live so hips, legs, and feet participate in
            // locomotion instead of falling back to an arms-only approximation.
            animator.runtimeAnimatorController = controller;
            // Native embedded takes use a different bind pose and displace
            // the lower body laterally. Keep the controller assigned for
            // inspection, but use the source-rig-safe procedural fallback.
            animator.enabled = false;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;

            SkinnedMeshRenderer bodyRenderer = FindRenderer(visual, BodyRendererName);
            SkinnedMeshRenderer clothRenderer = FindRenderer(visual, ClothRendererName);
            bodyRenderer.sharedMesh = BuildStraightBodyMesh(bodyRenderer.sharedMesh);
            bodyRenderer.sharedMaterial = bodyMaterial;
            clothRenderer.sharedMaterial = clothMaterial;
            clothRenderer.enabled = true;
            clothRenderer.updateWhenOffscreen = true;
            clothRenderer.shadowCastingMode = ShadowCastingMode.On;

            GetOrAdd<CharacterAnimationEventRelay>(animator.gameObject);
            AlignVisualFeetToNavMesh(visual.transform);
            CreateHoodInteriorLiner(animator, bodyMaterial);
            CreateBackOpeningRepair(visual.transform, animator, bodyMaterial);
            ConfigureCloth(clothRenderer, animator);
            GetOrAdd<ShadowWardenCapeRuntime>(clothRenderer.gameObject);
            GetOrAdd<ShadowWardenNativeWalk>(wrapper);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(wrapper, PrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Unity could not save the Warden prefab at {PrefabPath}.");
            }

            return prefab;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(wrapper);
        }
    }

    private static Mesh BuildStraightBodyMesh(Mesh source)
    {
        Mesh derivative = AssetDatabase.LoadAssetAtPath<Mesh>(StraightBodyMeshPath);
        if (derivative == null)
        {
            derivative = UnityEngine.Object.Instantiate(source);
            derivative.name = "ShadowWarden_Body_StraightBack";
            AssetDatabase.CreateAsset(derivative, StraightBodyMeshPath);
        }

        Vector3[] vertices = source.vertices;
        int corrected = 0;
        for (int index = 0; index < vertices.Length; index++)
        {
            if (vertices[index].y > -0.55f && vertices[index].y < 0.95f && vertices[index].z < -0.10f)
            {
                vertices[index].z += Mathf.Min(0.14f, (-0.10f - vertices[index].z) * 0.38f);
                corrected++;
            }
        }
        derivative.vertices = vertices;
        derivative.RecalculateNormals();
        derivative.RecalculateBounds();
        EditorUtility.SetDirty(derivative);
        return derivative;
    }

    private static void CreateHoodInteriorLiner(Animator animator, Material material)
    {
        Transform neck = animator.GetBoneTransform(HumanBodyBones.Neck);
        if (neck == null)
        {
            throw new InvalidOperationException("The Warden has no mapped neck bone for the hood liner.");
        }

        Transform old = neck.Find("HoodInteriorLiner");
        if (old != null)
        {
            UnityEngine.Object.DestroyImmediate(old.gameObject);
        }

        GameObject liner = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        liner.name = "HoodInteriorLiner";
        liner.transform.SetParent(neck, false);
        liner.transform.localPosition = new Vector3(0f, 0.14f, -0.015f);
        liner.transform.localRotation = Quaternion.identity;
        liner.transform.localScale = new Vector3(0.38f, 0.28f, 0.32f);
        UnityEngine.Object.DestroyImmediate(liner.GetComponent<Collider>());

        MeshRenderer renderer = liner.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static void CreateBackOpeningRepair(Transform visual, Animator animator, Material material)
    {
        Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        if (hips == null)
        {
            throw new InvalidOperationException("The Warden has no mapped hips bone for the back repair.");
        }

        Mesh repairMesh = AssetDatabase.LoadAssetAtPath<Mesh>(BackRepairMeshPath);
        if (repairMesh == null)
        {
            repairMesh = new Mesh { name = "ShadowWarden_BackOpeningRepair" };
            AssetDatabase.CreateAsset(repairMesh, BackRepairMeshPath);
        }

        const float minX = 0.445f;
        const float maxX = 0.640f;
        const float minY = -0.795f;
        const float maxY = -0.530f;
        const float bevel = 0.030f;
        const float frontZ = -0.230f;
        const float backZ = -0.195f;
        Vector3[] perimeter =
        {
            new Vector3(minX + bevel, minY, frontZ),
            new Vector3(maxX - bevel, minY, frontZ),
            new Vector3(maxX, minY + bevel, frontZ),
            new Vector3(maxX, maxY - bevel, frontZ),
            new Vector3(maxX - bevel, maxY, frontZ),
            new Vector3(minX + bevel, maxY, frontZ),
            new Vector3(minX, maxY - bevel, frontZ),
            new Vector3(minX, minY + bevel, frontZ),
        };
        Vector3[] vertices = new Vector3[perimeter.Length * 2];
        for (int index = 0; index < perimeter.Length; index++)
        {
            Vector3 world = visual.TransformPoint(perimeter[index]);
            vertices[index] = hips.InverseTransformPoint(world);
            world = visual.TransformPoint(new Vector3(perimeter[index].x, perimeter[index].y, backZ));
            vertices[index + perimeter.Length] = hips.InverseTransformPoint(world);
        }

        int[] triangles = new int[84];
        int triangleIndex = 0;
        for (int index = 0; index < perimeter.Length; index++)
        {
            int next = (index + 1) % perimeter.Length;
            triangles[triangleIndex++] = index;
            triangles[triangleIndex++] = next;
            triangles[triangleIndex++] = index + perimeter.Length;
            triangles[triangleIndex++] = next;
            triangles[triangleIndex++] = next + perimeter.Length;
            triangles[triangleIndex++] = index + perimeter.Length;
        }
        triangles[triangleIndex++] = 0;
        triangles[triangleIndex++] = 7;
        triangles[triangleIndex++] = 6;
        triangles[triangleIndex++] = 0;
        triangles[triangleIndex++] = 6;
        triangles[triangleIndex++] = 5;
        triangles[triangleIndex++] = 0;
        triangles[triangleIndex++] = 5;
        triangles[triangleIndex++] = 4;
        triangles[triangleIndex++] = 0;
        triangles[triangleIndex++] = 4;
        triangles[triangleIndex++] = 3;
        triangles[triangleIndex++] = 0;
        triangles[triangleIndex++] = 3;
        triangles[triangleIndex++] = 2;
        triangles[triangleIndex++] = 0;
        triangles[triangleIndex++] = 2;
        triangles[triangleIndex++] = 1;
        triangles[triangleIndex++] = perimeter.Length;
        triangles[triangleIndex++] = perimeter.Length + 1;
        triangles[triangleIndex++] = perimeter.Length + 2;
        triangles[triangleIndex++] = perimeter.Length;
        triangles[triangleIndex++] = perimeter.Length + 2;
        triangles[triangleIndex++] = perimeter.Length + 3;
        triangles[triangleIndex++] = perimeter.Length;
        triangles[triangleIndex++] = perimeter.Length + 3;
        triangles[triangleIndex++] = perimeter.Length + 4;
        triangles[triangleIndex++] = perimeter.Length;
        triangles[triangleIndex++] = perimeter.Length + 4;
        triangles[triangleIndex++] = perimeter.Length + 5;
        triangles[triangleIndex++] = perimeter.Length;
        triangles[triangleIndex++] = perimeter.Length + 5;
        triangles[triangleIndex++] = perimeter.Length + 6;
        triangles[triangleIndex++] = perimeter.Length;
        triangles[triangleIndex++] = perimeter.Length + 6;
        triangles[triangleIndex++] = perimeter.Length + 7;
        repairMesh.Clear();
        repairMesh.vertices = vertices;
        repairMesh.triangles = triangles;
        repairMesh.RecalculateNormals();
        repairMesh.RecalculateBounds();
        EditorUtility.SetDirty(repairMesh);

        Transform old = hips.Find("ShadowWarden_BackOpeningRepair");
        if (old != null)
        {
            UnityEngine.Object.DestroyImmediate(old.gameObject);
        }

        GameObject repair = new GameObject("ShadowWarden_BackOpeningRepair");
        repair.transform.SetParent(hips, false);
        MeshFilter filter = repair.AddComponent<MeshFilter>();
        filter.sharedMesh = repairMesh;
        MeshRenderer renderer = repair.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static void ConfigureCloth(SkinnedMeshRenderer renderer, Animator animator)
    {
        Cloth cloth = GetOrAdd<Cloth>(renderer.gameObject);
        CapeTopology topology = BuildTopology(renderer, cloth);
        Vector3[] particlePositions = cloth.vertices;
        Vector3 localUp = FindLocalUpAxis(renderer.transform);
        ClothSkinningCoefficient[] coefficients = new ClothSkinningCoefficient[particlePositions.Length];

        for (int islandIndex = 0; islandIndex < topology.Islands.Count; islandIndex++)
        {
            List<int> island = topology.Islands[islandIndex];
            float minHeight = island.Min(index => Vector3.Dot(particlePositions[index], localUp));
            float maxHeight = island.Max(index => Vector3.Dot(particlePositions[index], localUp));
            float height = Mathf.Max(0.001f, maxHeight - minHeight);
            float freeDistance = Mathf.Clamp(height * 0.19f, 0.16f, 0.30f);
            foreach (int particleIndex in island)
            {
                float down = (maxHeight - Vector3.Dot(particlePositions[particleIndex], localUp)) / height;
                // The largest island is the broad rear cape. The two smaller
                // coat-tail panels stay skinned to the rig, removing 2,619
                // active particles while retaining the authored silhouette.
                bool fixedAnchor = islandIndex > 0 || down <= 0.07f;
                coefficients[particleIndex] = new ClothSkinningCoefficient
                {
                    maxDistance = fixedAnchor
                        ? 0f
                        : freeDistance * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.07f, 0.70f, down)),
                    collisionSphereDistance = fixedAnchor ? 0f : 0.01f,
                };
            }
        }

        cloth.coefficients = coefficients;
        cloth.useGravity = true;
        cloth.useTethers = true;
        cloth.stretchingStiffness = 0.90f;
        cloth.bendingStiffness = 0.25f;
        cloth.damping = 0.48f;
        cloth.friction = 0.20f;
        cloth.collisionMassScale = 0.20f;
        cloth.worldVelocityScale = 0.24f;
        cloth.worldAccelerationScale = 0.08f;
        cloth.clothSolverFrequency = 60f;
        cloth.stiffnessFrequency = 10f;
        cloth.sleepThreshold = 0.025f;
        cloth.enableContinuousCollision = true;
        cloth.useVirtualParticles = 0f;
        cloth.selfCollisionDistance = 0f;
        cloth.selfCollisionStiffness = 0f;
        cloth.externalAcceleration = Vector3.zero;
        cloth.randomAcceleration = Vector3.zero;
        cloth.SetSelfAndInterCollisionIndices(new List<uint>());

        SphereCollider upperChest = CreateSphereCollider(animator, HumanBodyBones.UpperChest, CollisionPrefix + "UpperChest", 0.19f);
        SphereCollider hips = CreateSphereCollider(animator, HumanBodyBones.Hips, CollisionPrefix + "Hips", 0.25f);
        SphereCollider leftUpperLeg = CreateSphereCollider(animator, HumanBodyBones.LeftUpperLeg, CollisionPrefix + "LeftUpperLeg", 0.15f);
        SphereCollider leftLowerLeg = CreateSphereCollider(animator, HumanBodyBones.LeftLowerLeg, CollisionPrefix + "LeftLowerLeg", 0.11f);
        SphereCollider rightUpperLeg = CreateSphereCollider(animator, HumanBodyBones.RightUpperLeg, CollisionPrefix + "RightUpperLeg", 0.15f);
        SphereCollider rightLowerLeg = CreateSphereCollider(animator, HumanBodyBones.RightLowerLeg, CollisionPrefix + "RightLowerLeg", 0.11f);
        cloth.sphereColliders = new[]
        {
            new ClothSphereColliderPair(upperChest, hips),
            new ClothSphereColliderPair(leftUpperLeg, leftLowerLeg),
            new ClothSphereColliderPair(rightUpperLeg, rightLowerLeg),
        };
        cloth.capsuleColliders = Array.Empty<CapsuleCollider>();
        EditorUtility.SetDirty(cloth);
    }

    private static CapeTopology BuildTopology(SkinnedMeshRenderer renderer, Cloth cloth)
    {
        Mesh mesh = renderer.sharedMesh;
        if (mesh == null || !mesh.isReadable)
        {
            throw new InvalidOperationException("The Warden cloth mesh is missing or not readable.");
        }

        Vector3[] renderVertices = mesh.vertices;
        Dictionary<Vector3, int> particleByPosition = new Dictionary<Vector3, int>();
        int[] renderToParticle = new int[renderVertices.Length];
        for (int index = 0; index < renderVertices.Length; index++)
        {
            if (!particleByPosition.TryGetValue(renderVertices[index], out int particleIndex))
            {
                particleIndex = particleByPosition.Count;
                particleByPosition.Add(renderVertices[index], particleIndex);
            }

            renderToParticle[index] = particleIndex;
        }

        int particleCount = particleByPosition.Count;
        if (particleCount != cloth.vertices.Length || particleCount != ExpectedClothParticleCount)
        {
            throw new InvalidOperationException($"Cloth weld mismatch: exact mesh weld={particleCount}, Unity Cloth={cloth.vertices.Length}, expected={ExpectedClothParticleCount}.");
        }

        DisjointSet islands = new DisjointSet(particleCount);
        int[] triangles = mesh.triangles;
        for (int index = 0; index + 2 < triangles.Length; index += 3)
        {
            int a = renderToParticle[triangles[index]];
            int b = renderToParticle[triangles[index + 1]];
            int c = renderToParticle[triangles[index + 2]];
            islands.Union(a, b);
            islands.Union(b, c);
            islands.Union(c, a);
        }

        List<List<int>> groups = Enumerable.Range(0, particleCount)
            .GroupBy(islands.Find)
            .Select(group => group.ToList())
            .OrderByDescending(group => group.Count)
            .ToList();
        if (groups.Count != ExpectedClothIslandCount)
        {
            throw new InvalidOperationException($"Expected {ExpectedClothIslandCount} welded garment islands but found {groups.Count}.");
        }

        return new CapeTopology(groups);
    }

    private static SphereCollider CreateSphereCollider(Animator animator, HumanBodyBones bone, string name, float radius)
    {
        Transform parent = animator.GetBoneTransform(bone);
        if (parent == null)
        {
            throw new InvalidOperationException($"Required Humanoid bone {bone} is not mapped.");
        }

        GameObject child = new GameObject(name);
        child.layer = 2;
        child.transform.SetParent(parent, false);
        SphereCollider collider = child.AddComponent<SphereCollider>();
        collider.radius = radius;
        collider.center = Vector3.zero;
        collider.isTrigger = true;
        return collider;
    }

    private static GameObject ReplaceSceneInstance(GameObject prefab)
    {
        GameObject existing = GameObject.Find("ShadowWarden");
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
        }

        GameObject warden = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (warden == null)
        {
            throw new InvalidOperationException("Unity could not instantiate the rebuilt Warden prefab.");
        }

        warden.name = "ShadowWarden";
        warden.transform.SetPositionAndRotation(new Vector3(-4f, 0.02f, 2f), Quaternion.Euler(0f, 180f, 0f));
        warden.transform.localScale = Vector3.one;

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
            UnityEngine.Object.DestroyImmediate(warden);
            throw new InvalidOperationException("PlayerVitals is missing from GameRoot/PlayerRoot.");
        }

        ai.ConfigureTarget(vitals.transform, vitals);
        return warden;
    }

    private static void AlignVisualFeetToNavMesh(Transform visualRoot)
    {
        visualRoot.localPosition += Vector3.up * VisualGroundOffset;
    }

    private static void ValidateWarden(GameObject root, string label, ICollection<string> failures, bool requireGameplay)
    {
        Animator animator = root.GetComponentInChildren<Animator>(true);
        SkinnedMeshRenderer body = TryFindRenderer(root, BodyRendererName);
        SkinnedMeshRenderer clothRenderer = TryFindRenderer(root, ClothRendererName);
        Cloth[] clothComponents = root.GetComponentsInChildren<Cloth>(true);
        SphereCollider[] proxies = root.GetComponentsInChildren<SphereCollider>(true)
            .Where(collider => collider.name.StartsWith(CollisionPrefix, StringComparison.Ordinal))
            .ToArray();

        Require(animator != null && animator.isHuman && animator.avatar != null && animator.avatar.isValid,
            $"{label}: Animator is not using a valid Humanoid Avatar.", failures);
        Require(animator != null && animator.transform != root.transform,
            $"{label}: visual rig is not separated from the navigation root.", failures);
        Require(animator != null && animator.runtimeAnimatorController == AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath),
            $"{label}: Animator is not using its dedicated controller.", failures);
        Require(animator != null && animator.runtimeAnimatorController != null,
            $"{label}: dedicated Animator controller is missing.", failures);
        Require(body != null && body.sharedMaterial == AssetDatabase.LoadAssetAtPath<Material>(ReadableBodyMaterialPath),
            $"{label}: body renderer/material is missing or incorrect.", failures);
        Require(body != null && body.sharedMesh == AssetDatabase.LoadAssetAtPath<Mesh>(StraightBodyMeshPath),
            $"{label}: straight-back derivative body mesh is missing or unassigned.", failures);
        Require(body != null && body.GetComponent<Cloth>() == null,
            $"{label}: rigid body renderer must never carry Cloth.", failures);
        Require(clothRenderer != null && clothRenderer.enabled,
            $"{label}: authored garment renderer is missing or disabled.", failures);
        Require(clothRenderer != null && clothRenderer.sharedMaterial == AssetDatabase.LoadAssetAtPath<Material>(ReadableClothMaterialPath),
            $"{label}: cloth material is missing or incorrect.", failures);
        Transform neck = animator != null ? animator.GetBoneTransform(HumanBodyBones.Neck) : null;
        Transform hips = animator != null ? animator.GetBoneTransform(HumanBodyBones.Hips) : null;
        Require(neck != null && neck.Find("HoodInteriorLiner") != null,
            $"{label}: hood interior liner is missing.", failures);
        Require(hips != null && hips.Find("ShadowWarden_BackOpeningRepair") != null,
            $"{label}: rear opening repair mesh is missing.", failures);
        Require(clothRenderer != null && clothRenderer.updateWhenOffscreen,
            $"{label}: cloth must continue simulating while briefly offscreen.", failures);
        Require(clothRenderer != null && clothRenderer.rootBone != null && clothRenderer.bones != null && clothRenderer.bones.Length > 0,
            $"{label}: garment bone binding is missing.", failures);
        Require(clothComponents.Length == 1,
            $"{label}: expected exactly one Cloth component, found {clothComponents.Length}.", failures);
        Require(clothRenderer != null && clothComponents.Length == 1 && clothRenderer.GetComponent<Cloth>() == clothComponents[0],
            $"{label}: the single Cloth component is not attached to {ClothRendererName}.", failures);
        ShadowWardenCapeRuntime[] capeRuntimeComponents = root.GetComponentsInChildren<ShadowWardenCapeRuntime>(true);
        Require(clothRenderer != null && capeRuntimeComponents.Length == 1
            && clothRenderer.GetComponent<ShadowWardenCapeRuntime>() == capeRuntimeComponents[0],
            $"{label}: cape lifecycle/reset component is missing.", failures);
        Require(root.GetComponent<ShadowWardenNativeWalk>() != null,
            $"{label}: native Warden presentation component is missing.", failures);
        Require(proxies.Length == 6,
            $"{label}: expected six isolated cape collision proxies, found {proxies.Length}.", failures);
        Require(proxies.All(proxy => proxy.enabled),
            $"{label}: every cape collision proxy must be enabled.", failures);
        Require(proxies.All(proxy => proxy.isTrigger && proxy.gameObject.layer == 2 && proxy.transform.IsChildOf(root.transform)),
            $"{label}: cape collision proxies must be trigger-only, Ignore Raycast children.", failures);
        Require(proxies.All(proxy => proxy.radius >= 0.10f && proxy.radius <= 0.26f),
            $"{label}: one or more cape collision proxies are oversized or degenerate.", failures);

        if (animator != null)
        {
            ValidateCollisionProxy(proxies, animator, CollisionPrefix + "UpperChest", HumanBodyBones.UpperChest, 0.19f, label, failures);
            ValidateCollisionProxy(proxies, animator, CollisionPrefix + "Hips", HumanBodyBones.Hips, 0.25f, label, failures);
            ValidateCollisionProxy(proxies, animator, CollisionPrefix + "LeftUpperLeg", HumanBodyBones.LeftUpperLeg, 0.15f, label, failures);
            ValidateCollisionProxy(proxies, animator, CollisionPrefix + "LeftLowerLeg", HumanBodyBones.LeftLowerLeg, 0.11f, label, failures);
            ValidateCollisionProxy(proxies, animator, CollisionPrefix + "RightUpperLeg", HumanBodyBones.RightUpperLeg, 0.15f, label, failures);
            ValidateCollisionProxy(proxies, animator, CollisionPrefix + "RightLowerLeg", HumanBodyBones.RightLowerLeg, 0.11f, label, failures);
        }

        if (clothRenderer != null && clothComponents.Length == 1)
        {
            Cloth cloth = clothComponents[0];
            ClothSkinningCoefficient[] coefficients = cloth.coefficients;
            Require(coefficients.Length == ExpectedClothParticleCount,
                $"{label}: Cloth has {coefficients.Length} coefficients instead of {ExpectedClothParticleCount}.", failures);
            Require(coefficients.All(coefficient => float.IsFinite(coefficient.maxDistance)
                && coefficient.maxDistance < float.MaxValue
                && coefficient.maxDistance <= 0.301f),
                $"{label}: garment contains invalid or excessive movement limits.", failures);
            Require(cloth.enableContinuousCollision && cloth.useVirtualParticles <= 0.001f
                && cloth.clothSolverFrequency <= 60.01f && Mathf.Abs(cloth.stiffnessFrequency - 10f) <= 0.01f,
                $"{label}: mobile cloth preset drifted.", failures);
            Require(cloth.sphereColliders != null && cloth.sphereColliders.Length == 3,
                $"{label}: Cloth collider-pair wiring is incomplete.", failures);
            if (cloth.sphereColliders != null && cloth.sphereColliders.Length == 3)
            {
                SphereCollider[] wiredEndpoints = cloth.sphereColliders
                    .SelectMany(pair => new[] { pair.first, pair.second })
                    .ToArray();
                Require(wiredEndpoints.All(endpoint => endpoint != null)
                    && wiredEndpoints.Distinct().Count() == 6
                    && wiredEndpoints.All(endpoint => proxies.Contains(endpoint)),
                    $"{label}: Cloth collider-pair endpoints are null, duplicated, or stale.", failures);
            }

            List<uint> selfCollisionIndices = new List<uint>();
            cloth.GetSelfAndInterCollisionIndices(selfCollisionIndices);
            Require(selfCollisionIndices.Count == 0,
                $"{label}: self/inter-collision must remain disabled for the mobile preset.", failures);

            try
            {
                CapeTopology topology = BuildTopology(clothRenderer, cloth);
                for (int islandIndex = 0; islandIndex < topology.Islands.Count; islandIndex++)
                {
                    List<int> island = topology.Islands[islandIndex];
                    int fixedCount = island.Count(index => coefficients[index].maxDistance <= 0.0001f);
                    int movableCount = island.Count - fixedCount;
                    bool validDistribution = islandIndex == 0
                        ? fixedCount >= 3 && movableCount >= island.Count / 4
                        : fixedCount == island.Count;
                    Require(validDistribution,
                        $"{label}: garment island {islandIndex} has unsafe anchoring (particles={island.Count}, fixed={fixedCount}, movable={movableCount}).", failures);
                }
            }
            catch (Exception exception)
            {
                failures.Add($"{label}: topology validation failed: {exception.Message}");
            }
        }

        if (animator != null)
        {
            Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            Require(leftFoot != null && rightFoot != null,
                $"{label}: Humanoid foot mapping is incomplete.", failures);
            if (leftFoot != null && rightFoot != null)
            {
                float lowestFoot = Mathf.Min(leftFoot.position.y, rightFoot.position.y) - root.transform.position.y;
                Require(lowestFoot >= -0.08f && lowestFoot <= 0.28f,
                    $"{label}: feet are outside the grounded tolerance ({lowestFoot:0.000}m from root plane).", failures);
            }
        }

        if (requireGameplay)
        {
            Require(root.GetComponent<NavMeshAgent>() != null, $"{label}: NavMeshAgent is missing.", failures);
            Require(root.GetComponent<ShadowWardenAI>() != null, $"{label}: ShadowWardenAI is missing.", failures);
            Require(root.GetComponent<EnemyHealth>() != null, $"{label}: EnemyHealth is missing.", failures);
            Require(root.GetComponent<CombatHitReaction>() != null, $"{label}: CombatHitReaction is missing.", failures);
        }
    }

    private static void ValidateCollisionProxy(
        SphereCollider[] proxies,
        Animator animator,
        string name,
        HumanBodyBones expectedBone,
        float expectedRadius,
        string label,
        ICollection<string> failures)
    {
        SphereCollider proxy = proxies.FirstOrDefault(candidate => candidate.name == name);
        Transform expectedParent = animator.GetBoneTransform(expectedBone);
        Require(proxy != null && expectedParent != null && proxy.transform.parent == expectedParent,
            $"{label}: {name} is not parented to the correct Humanoid bone ({expectedBone}).", failures);
        if (proxy == null)
        {
            return;
        }

        Vector3 scale = proxy.transform.lossyScale;
        float maximumScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        float worldRadius = proxy.radius * maximumScale;
        Require(float.IsFinite(worldRadius) && Mathf.Abs(worldRadius - expectedRadius) <= 0.015f,
            $"{label}: {name} has an invalid world-space radius ({worldRadius:0.000}m; expected {expectedRadius:0.000}m).", failures);
    }

    private static void AssertValidatorAccepts(Scene previewScene, GameObject prefab)
    {
        GameObject clone = PrefabUtility.InstantiatePrefab(prefab, previewScene) as GameObject;
        if (clone == null)
        {
            throw new InvalidOperationException("Could not instantiate validator baseline fixture.");
        }

        try
        {
            List<string> failures = new List<string>();
            ValidateWarden(clone, "Self-test baseline", failures, false);
            if (failures.Count > 0)
            {
                throw new InvalidOperationException("Cape validator baseline is invalid:\n- " + string.Join("\n- ", failures));
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(clone);
        }
    }

    private static void AssertValidatorRejects(
        Scene previewScene,
        GameObject prefab,
        string scenario,
        string expectedFailureFragment,
        Action<GameObject> mutate)
    {
        GameObject clone = PrefabUtility.InstantiatePrefab(prefab, previewScene) as GameObject;
        if (clone == null)
        {
            throw new InvalidOperationException("Could not instantiate validator self-test fixture.");
        }

        try
        {
            // Mutations in rejection fixtures must target the clone, not a
            // nested prefab instance, so the fixture remains isolated.
            PrefabUtility.UnpackPrefabInstance(clone, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            mutate(clone);
            List<string> failures = new List<string>();
            ValidateWarden(clone, "Self-test", failures, false);
            if (!failures.Any(failure => failure.IndexOf(expectedFailureFragment, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                throw new InvalidOperationException($"Cape validator failed to reject {scenario} for the expected reason. Actual failures:\n- {string.Join("\n- ", failures)}");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(clone);
        }
    }

    private static Vector3 FindLocalUpAxis(Transform transform)
    {
        Vector3[] candidates = { Vector3.right, Vector3.up, Vector3.forward, Vector3.left, Vector3.down, Vector3.back };
        return candidates.OrderByDescending(candidate => Vector3.Dot(transform.TransformDirection(candidate).normalized, Vector3.up)).First();
    }

    private static SkinnedMeshRenderer FindRenderer(GameObject root, string name)
    {
        SkinnedMeshRenderer renderer = TryFindRenderer(root, name);
        if (renderer == null)
        {
            throw new InvalidOperationException($"Skinned renderer '{name}' is missing from the prepared model.");
        }

        return renderer;
    }

    private static SkinnedMeshRenderer TryFindRenderer(GameObject root, string name)
    {
        return root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
            .FirstOrDefault(candidate => candidate.name == name);
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

    private static AnimatorState AddState(AnimatorStateMachine machine, string name, Motion motion, float speed)
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

        T added = target.AddComponent<T>();
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

    private sealed class CapeTopology
    {
        public CapeTopology(List<List<int>> islands)
        {
            Islands = islands;
        }

        public List<List<int>> Islands { get; }
    }

    private sealed class DisjointSet
    {
        private readonly int[] parent;
        private readonly byte[] rank;

        public DisjointSet(int size)
        {
            parent = Enumerable.Range(0, size).ToArray();
            rank = new byte[size];
        }

        public int Find(int value)
        {
            if (parent[value] != value)
            {
                parent[value] = Find(parent[value]);
            }

            return parent[value];
        }

        public void Union(int left, int right)
        {
            int leftRoot = Find(left);
            int rightRoot = Find(right);
            if (leftRoot == rightRoot)
            {
                return;
            }

            if (rank[leftRoot] < rank[rightRoot])
            {
                parent[leftRoot] = rightRoot;
            }
            else if (rank[leftRoot] > rank[rightRoot])
            {
                parent[rightRoot] = leftRoot;
            }
            else
            {
                parent[rightRoot] = leftRoot;
                rank[leftRoot]++;
            }
        }
    }
}
