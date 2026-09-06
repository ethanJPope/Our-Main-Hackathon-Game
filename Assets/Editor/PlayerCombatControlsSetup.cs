using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PlayerCombatControlsSetup
{
    [MenuItem("Tools/Player/Install Combat Controls in PlayerScene")]
    public static void Install()
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().path != "Assets/PlayerScene.unity")
            throw new System.InvalidOperationException("Open PlayerScene in Edit Mode first.");
        GameObject player = GameObject.Find("GameRoot/PlayerRoot");
        Transform hud = GameObject.Find("GameRoot/MobileHUD").transform;
        var input = Add<PlayerCombatInput>(player);
        var targeting = Add<PlayerCombatTargeting>(player);
        var movement = player.GetComponent<PlayerJoystickMovement>();
        var attack = player.GetComponent<PlayerSwordAttack>();
        var vitals = player.GetComponent<PlayerVitals>();
        var camera = Camera.main;
        var joystick = hud.GetComponentInChildren<MobileJoystick>(true);
        var attackButton = hud.GetComponentInChildren<MobileAttackButton>(true);
        var dodgeButton = hud.GetComponentInChildren<MobileDodgeButton>(true);
        RectTransform safe = Rect(hud, "CombatSafeArea");
        safe.anchorMin = Vector2.zero; safe.anchorMax = Vector2.one;
        safe.offsetMin = safe.offsetMax = Vector2.zero;
        Reparent(hud.Find("JoystickRoot") ?? safe.Find("JoystickRoot"), safe);
        Reparent(attackButton.transform, safe);
        Reparent(dodgeButton.transform, safe);
        Place((RectTransform)joystick.transform.parent, new Vector2(0,0), new Vector2(185,185), new Vector2(300,300));
        Place((RectTransform)attackButton.transform, new Vector2(1,0), new Vector2(-300,285), new Vector2(180,180));
        Place((RectTransform)dodgeButton.transform, new Vector2(1,0), new Vector2(-145,130), new Vector2(190,190));
        RectTransform lockRoot = Rect(safe, "LockButtonRoot");
        Place(lockRoot, new Vector2(1,0), new Vector2(-135,420), new Vector2(120,120));
        var lockButton = Add<CombatTouchButton>(lockRoot.gameObject);
        Image lockImage = Add<Image>(lockRoot.gameObject);
        lockImage.sprite = dodgeButton.GetComponent<Image>().sprite;
        lockImage.color = new Color(0.12f,0.14f,0.17f,0.85f);
        lockImage.raycastTarget = true;
        Text lockLabel = Label(lockRoot, "LockLabel", "LOCK ON", 18);
        Text attackLabel = attackButton.transform.Find("Label").GetComponent<Text>();
        Text dodgeLabel = dodgeButton.transform.Find("Label").GetComponent<Text>();
        attackLabel.fontSize = dodgeLabel.fontSize = 16;
        attackLabel.rectTransform.sizeDelta = dodgeLabel.rectTransform.sizeDelta = new Vector2(176,36);
        attackButton.transform.Find("DodgeArrow").GetComponent<Text>().text = "ATK";
        dodgeButton.transform.Find("DodgeArrow").GetComponent<Text>().text = ">>";
        var hotbar = hud.Find("HotbarRoot");
        if (hotbar != null) { Undo.RecordObject(hotbar.gameObject,"Hide unused hotbar"); hotbar.gameObject.SetActive(false); }
        Text hint = Label(safe, "ControlsHint", "WASD  Move   /   LMB  Attack   /   SPACE  Dodge   /   TAB  Lock   /   RMB drag  Camera\nTouch: left stick + action buttons; swipe clear space to orbit", 18);
        Place(hint.rectTransform, new Vector2(0.5f,0), new Vector2(0,40), new Vector2(1050,65));
        Text reticle = Label(hud, "LockReticle", "+", 32);
        reticle.color = new Color(1f,0.8f,0.4f,1f);
        reticle.rectTransform.anchorMin = reticle.rectTransform.anchorMax = new Vector2(0.5f,0.5f);
        reticle.rectTransform.sizeDelta = new Vector2(45,45);
        reticle.gameObject.SetActive(false);
        Ref(input,"joystick",joystick); Ref(input,"attackButton",attackButton); Ref(input,"dodgeButton",dodgeButton);
        Ref(input,"lockButton",lockButton); Ref(input,"viewCamera",camera);
        Ref(targeting,"viewCamera",camera); Ref(targeting,"cameraTarget",player.transform.Find("CameraTarget"));
        Float(movement,"acceleration",35f); Float(movement,"deceleration",45f); Float(movement,"turnAcceleration",55f);
        Float(movement,"rotationSpeed",18f); Float(movement,"dodgeDistance",3.2f); Float(movement,"dodgeDuration",0.32f);
        Float(movement,"dodgeCooldown",0.52f); Float(movement,"dodgeStaminaCost",18f);
        Float(movement,"invulnerabilityStart",0.04f); Float(movement,"invulnerabilityEnd",0.22f);
        Float(vitals,"staminaRegenerationPerSecond",24f);
        var feedback = Add<PlayerCombatHUD>(hud.gameObject);
        Ref(feedback,"vitals",vitals); Ref(feedback,"movement",movement); Ref(feedback,"attack",attack);
        Ref(feedback,"targeting",targeting); Ref(feedback,"input",input); Ref(feedback,"safeArea",safe);
        Ref(feedback,"attackFill",attackButton.transform.Find("GoldRim").GetComponent<Image>());
        Ref(feedback,"dodgeFill",dodgeButton.transform.Find("GoldRim").GetComponent<Image>());
        Ref(feedback,"attackStatus",attackLabel); Ref(feedback,"dodgeStatus",dodgeLabel); Ref(feedback,"lockStatus",lockLabel);
        Ref(feedback,"reticle",reticle.rectTransform);
        ConfigureAnimationAndEffects(player);
        AddPracticeTarget();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }
    private static void AddPracticeTarget()
    {
        if(GameObject.Find("PlayerPracticeTarget")!=null)return;
        var root=new GameObject("PlayerPracticeTarget");
        Undo.RegisterCreatedObjectUndo(root,"Add player practice target");
        root.transform.SetParent(GameObject.Find("GameRoot/World").transform,false);
        root.transform.position=new Vector3(0,0.05f,3f);
        var health=Undo.AddComponent<EnemyHealth>(root);
        Float(health,"maximumHealth",1000f); Float(health,"startingHealth",1000f);
        var collider=Undo.AddComponent<CapsuleCollider>(root); collider.center=Vector3.up; collider.height=2f; collider.radius=0.4f;
        const string path="Assets/PlayerCombat/MAT_PracticeTarget.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null)
        {
            material=new Material(Shader.Find("HDRP/Lit")); material.SetColor("_BaseColor",new Color(0.34f,0.18f,0.07f));
            material.SetFloat("_Smoothness",0.15f); AssetDatabase.CreateAsset(material,path);
        }
        PracticePart(root.transform,"Body",PrimitiveType.Cylinder,new Vector3(0,0.95f,0),new Vector3(0.7f,0.65f,0.7f),material);
        PracticePart(root.transform,"Head",PrimitiveType.Cube,new Vector3(0,1.8f,0),new Vector3(0.48f,0.48f,0.48f),material);
        PracticePart(root.transform,"Arms",PrimitiveType.Cube,new Vector3(0,1.25f,0),new Vector3(1.6f,0.2f,0.25f),material);
        PracticePart(root.transform,"Stand",PrimitiveType.Cylinder,new Vector3(0,0.08f,0),new Vector3(1f,0.08f,1f),material);
        AssetDatabase.SaveAssets();
    }
    private static void PracticePart(Transform root,string name,PrimitiveType type,Vector3 position,Vector3 scale,Material material)
    {
        var go=GameObject.CreatePrimitive(type); Undo.RegisterCreatedObjectUndo(go,"Build practice target");
        go.name=name; go.transform.SetParent(root,false); go.transform.localPosition=position; go.transform.localScale=scale;
        Undo.DestroyObjectImmediate(go.GetComponent<Collider>()); go.GetComponent<Renderer>().sharedMaterial=material;
    }
    private static void ConfigureAnimationAndEffects(GameObject player)
    {
        const string folder="Assets/PlayerCombat";
        if(!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets","PlayerCombat");
        var animator=player.GetComponentInChildren<Animator>();
        string path=folder+"/AC_PlayerCombat.controller";
        if(AssetDatabase.LoadAssetAtPath<AnimatorController>(path)==null)
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(animator.runtimeAnimatorController),path);
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        foreach(var child in controller.layers[0].stateMachine.states)
        {
            var state=child.state;
            if(state.motion is not AnimationClip clip) continue;
            if(state.name=="Dodge" || state.name=="Forward Punch")
            {
                Undo.RecordObject(state,"Match combat animation timing");
                state.speed=clip.length/(state.name=="Dodge"?0.48f:0.72f);
                foreach(var transition in state.transitions)
                {
                    Undo.RecordObject(transition,"Match recovery timing");
                    transition.hasExitTime=true; transition.exitTime=0.95f;
                    transition.hasFixedDuration=true; transition.duration=0.035f;
                }
            }
        }
        Undo.RecordObject(animator,"Assign combat animation controller"); animator.runtimeAnimatorController=controller;
        var material=AssetDatabase.LoadAssetAtPath<Material>(folder+"/MAT_CombatVFX.mat");
        if(material==null)
        {
            material=new Material(Shader.Find("MainHackathonGame/CombatVFX"));
            AssetDatabase.CreateAsset(material,folder+"/MAT_CombatVFX.mat");
        }
        Ref(Add<PlayerCombatFeedback>(player),"effectMaterial",material);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
    }
    private static T Add<T>(GameObject go) where T : Component => go.GetComponent<T>() ?? Undo.AddComponent<T>(go);
    private static RectTransform Rect(Transform parent,string name)
    {
        var existing=parent.Find(name) as RectTransform;
        if(existing!=null)return existing;
        var go=new GameObject(name,typeof(RectTransform)); Undo.RegisterCreatedObjectUndo(go,"Create combat HUD");
        go.transform.SetParent(parent,false); return (RectTransform)go.transform;
    }
    private static void Reparent(Transform child,Transform parent) { if(child!=null) Undo.SetTransformParent(child,parent,"Arrange controls"); }
    private static void Place(RectTransform rect,Vector2 anchor,Vector2 position,Vector2 size)
    {
        Undo.RecordObject(rect,"Arrange combat controls"); rect.anchorMin=rect.anchorMax=anchor; rect.pivot=new Vector2(0.5f,0.5f);
        rect.anchoredPosition=position; rect.sizeDelta=size; rect.localScale=Vector3.one;
    }
    private static Text Label(Transform parent,string name,string value,int size)
    {
        var rect=Rect(parent,name); var text=Add<Text>(rect.gameObject); Undo.RecordObject(text,"Label controls");
        text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.text=value; text.fontSize=size;
        text.alignment=TextAnchor.MiddleCenter; text.color=new Color(0.9f,0.92f,0.94f,0.9f); text.raycastTarget=false;
        rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero;
        return text;
    }
    private static void Ref(Object owner,string name,Object value) { var so=new SerializedObject(owner); so.FindProperty(name).objectReferenceValue=value; so.ApplyModifiedProperties(); }
    private static void Float(Object owner,string name,float value) { var so=new SerializedObject(owner); so.FindProperty(name).floatValue=value; so.ApplyModifiedProperties(); }
}
