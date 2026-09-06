using UnityEngine;

/// <summary>Small dodge streak and pooled contact sparks; no camera shake or time freeze.</summary>
public sealed class PlayerCombatFeedback : MonoBehaviour
{
    [SerializeField] private Material effectMaterial;
    private PlayerJoystickMovement movement;
    private PlayerSwordAttack attack;
    private TrailRenderer trail;
    private LineRenderer[] sparks;
    private float hitAt = -10f;
    private Vector3 hitPoint;
    private readonly Vector3[] sparkDirections = { new Vector3(1,1,0), new Vector3(-1,0.6f,0), new Vector3(0,1,1), new Vector3(0,0.4f,-1) };

    private void Awake()
    {
        movement = GetComponent<PlayerJoystickMovement>();
        attack = GetComponent<PlayerSwordAttack>();
        if (effectMaterial == null) { enabled = false; return; }
        var streak = new GameObject("DodgeStreak");
        streak.transform.SetParent(transform,false); streak.transform.localPosition = Vector3.up * 0.7f;
        trail = streak.AddComponent<TrailRenderer>();
        trail.sharedMaterial = effectMaterial; trail.time = 0.13f; trail.minVertexDistance = 0.07f;
        trail.startWidth = 0.12f; trail.endWidth = 0f; trail.emitting = false;
        trail.startColor = new Color(0.5f,0.85f,1f,0.7f); trail.endColor = new Color(0.5f,0.85f,1f,0f);
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        sparks = new LineRenderer[4];
        for (int i=0;i<sparks.Length;i++)
        {
            var spark = new GameObject("ContactSpark"+i); spark.transform.SetParent(transform,false);
            var line = spark.AddComponent<LineRenderer>(); sparks[i]=line;
            line.sharedMaterial=effectMaterial; line.positionCount=2; line.useWorldSpace=true;
            line.widthCurve=AnimationCurve.Linear(0f,1f,1f,0f); line.widthMultiplier=0.035f; line.enabled=false;
            line.startColor=new Color(1f,0.8f,0.35f,1f); line.endColor=new Color(1f,0.6f,0.2f,0f);
            line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
    private void OnEnable() { if (attack != null) attack.HitConfirmed += OnHit; }
    private void OnDisable()
    {
        if (attack != null) attack.HitConfirmed -= OnHit;
        if (trail != null) { trail.emitting=false; trail.Clear(); }
        if (sparks != null) foreach(var spark in sparks) spark.enabled=false;
        hitAt=-10f;
    }
    private void OnHit(Vector3 position) { hitPoint=position; hitAt=Time.time; }
    private void LateUpdate()
    {
        if (trail == null) return;
        trail.emitting=movement != null && movement.IsInvulnerable;
        float age=Time.time-hitAt;
        for(int i=0;i<sparks.Length;i++)
        {
            bool visible=age>=0f && age<0.14f;
            sparks[i].enabled=visible;
            if(!visible)continue;
            Vector3 direction=sparkDirections[i].normalized;
            sparks[i].SetPosition(0,hitPoint+direction*age);
            sparks[i].SetPosition(1,hitPoint+direction*(0.15f+age*2f));
            sparks[i].widthMultiplier=0.035f*(1f-age/0.14f);
        }
    }
}
