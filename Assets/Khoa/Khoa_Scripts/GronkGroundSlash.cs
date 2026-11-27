using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Kỹ năng "tụ lực - chém bổ rẽ đất" của Gronk (state Ranged_Hogoblin1).
/// Được gọi bằng các Animation Event có sẵn trong clip Ranged_Hogoblin1:
///   t = 0.00s  SpawnSound -> BeginCharge(): hạt sáng hút vào 2 thanh kiếm + ánh sáng mạnh dần (tụ lực)
///   t = 2.23s  SpawnFx    -> Slash(): chém xuống, vết nứt rực sáng chạy dọc mặt đất về phía trước,
///                            đá + tia lửa bắn lên, gây sát thương mục tiêu trên đường đi (mỗi mục tiêu 1 lần)
///   t = 2.25s  SpawnProjectilePredictNextPosition -> bỏ qua (event cũ của asset gốc, giữ để không báo lỗi)
/// Nếu không gán prefab/material, toàn bộ FX được tạo bằng code nên dùng được ngay.
/// </summary>
public class GronkGroundSlash : MonoBehaviour
{
    [Header("Điểm bắt đầu vết nứt (để trống: trước mặt boss)")]
    public Transform origin;
    [Tooltip("Khoảng cách từ boss tới điểm bắt đầu nứt khi không gán Origin")]
    public float startOffset = 2f;

    [Header("Vết nứt")]
    public float length = 18f;
    public float travelSpeed = 28f;
    [Tooltip("Khoảng cách giữa 2 điểm nứt / 2 lần bắn đá")]
    public float segment = 0.8f;
    [Tooltip("Độ lệch ngang ngẫu nhiên của vết nứt (zig-zag)")]
    public float jagged = 0.35f;
    public float crackWidth = 0.5f;
    [Tooltip("Thời gian vết nứt còn sáng sau khi chạy xong")]
    public float crackLifetime = 1.5f;
    public Color crackColor = new Color(1f, 0.45f, 0.1f, 1f);
    public Color debrisColor = new Color(0.35f, 0.27f, 0.2f, 1f);

    [Header("Mặt đất")]
    public LayerMask groundMask = ~0;
    public float groundProbe = 6f;

    [Header("Sát thương")]
    public string targetTag = "Player";
    public float damage = 35f;
    [Tooltip("Bán kính gây sát thương quanh đầu vết nứt")]
    public float hitRadius = 1.5f;
    public UnityEvent<GameObject> onHitTarget;

    [Header("Tụ lực")]
    [Tooltip("Để trống: dùng 2 thanh kiếm của GronkSwordThrower")]
    public Transform[] chargePoints;
    public Color chargeColor = new Color(1f, 0.3f, 0.05f, 1f);
    [Tooltip("Thời gian tụ lực (khớp mốc SpawnFx 2.23s trong clip)")]
    public float chargeDuration = 2.2f;
    public float chargeLightIntensity = 6f;

    [Header("FX tùy chọn (để trống sẽ tự tạo bằng code)")]
    public GameObject chargeFxPrefab;
    [Tooltip("Spawn ở mỗi đoạn nứt")]
    public GameObject impactFxPrefab;
    [Tooltip("Material additive cho tia lửa / vết nứt. Nên gán khi build để chắc chắn shader không bị strip")]
    public Material glowMaterial;
    [Tooltip("Material alpha-blend cho đá / bụi")]
    public Material dustMaterial;

    readonly List<GameObject> chargeFx = new List<GameObject>();
    readonly List<GameObject> spawned = new List<GameObject>();
    Coroutine chargeRoutine;
    ParticleSystem sparks, debris;
    static Texture2D softTex;

    // ---------- Animation Events ----------
    public void SpawnSound() => BeginCharge();
    public void SpawnFx() => Slash();
    public void SpawnProjectilePredictNextPosition() { }

    // ---------- Tụ lực ----------
    public void BeginCharge()
    {
        ClearCharge();
        foreach (var p in GetChargePoints())
        {
            var fx = chargeFxPrefab ? Instantiate(chargeFxPrefab, p.position, p.rotation, p) : CreateChargeFx(p);
            chargeFx.Add(fx);
        }
        if (chargeRoutine != null) StopCoroutine(chargeRoutine);
        chargeRoutine = StartCoroutine(ChargeRamp());
    }

    IEnumerator ChargeRamp()
    {
        float t = 0f;
        while (t < chargeDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / chargeDuration);
            foreach (var fx in chargeFx)
                if (fx && fx.TryGetComponent(out Light l)) l.intensity = chargeLightIntensity * k * k;
            yield return null;
        }
        chargeRoutine = null;
    }

    void ClearCharge()
    {
        if (chargeRoutine != null) { StopCoroutine(chargeRoutine); chargeRoutine = null; }
        foreach (var fx in chargeFx) if (fx) Destroy(fx);
        chargeFx.Clear();
    }

    Transform[] GetChargePoints()
    {
        if (chargePoints != null && chargePoints.Length > 0) return chargePoints;
        var thrower = GetComponent<GronkSwordThrower>();
        if (thrower && thrower.swords != null && thrower.swords.Length > 0) return thrower.swords;
        return new[] { transform };
    }

    // ---------- Chém bổ đất ----------
    public void Slash()
    {
        ClearCharge();
        Vector3 dir = transform.forward; dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
        dir.Normalize();

        Vector3 start = origin ? origin.position : transform.position + dir * startOffset;
        start = Ground(start);
        StartCoroutine(RunCrack(start, dir));
    }

    IEnumerator RunCrack(Vector3 start, Vector3 dir)
    {
        EnsureParticles();
        Vector3 side = Vector3.Cross(Vector3.up, dir);

        var line = CreateCrackLine();
        var points = new List<Vector3> { start + Vector3.up * 0.05f };
        line.positionCount = 1;
        line.SetPosition(0, points[0]);

        Flash(start);
        Burst(start, 25, 1.5f);

        var hitOnce = new HashSet<GameObject>();
        float travelled = 0f, nextPoint = segment;
        while (travelled < length)
        {
            travelled = Mathf.Min(length, travelled + travelSpeed * Time.deltaTime);
            Vector3 front = Ground(start + dir * travelled);
            DamageAt(front, hitOnce);

            while (nextPoint <= travelled)
            {
                Vector3 p = Ground(start + dir * nextPoint + side * Random.Range(-jagged, jagged));
                points.Add(p + Vector3.up * 0.05f);
                line.positionCount = points.Count;
                line.SetPosition(points.Count - 1, points[points.Count - 1]);
                Burst(p, 6, 1f);
                if (impactFxPrefab) Track(Instantiate(impactFxPrefab, p, Quaternion.LookRotation(dir)), 3f);
                nextPoint += segment;
            }
            yield return null;
        }

        // vết nứt mờ dần rồi biến mất
        float t = 0f;
        while (t < crackLifetime && line)
        {
            t += Time.deltaTime;
            float a = 1f - t / crackLifetime;
            Color c = crackColor; c.a = a;
            line.startColor = c; line.endColor = new Color(c.r, c.g, c.b, a * 0.4f);
            line.widthMultiplier = crackWidth * (0.5f + 0.5f * a);
            yield return null;
        }
        if (line) { spawned.Remove(line.gameObject); Destroy(line.gameObject); }
    }

    void DamageAt(Vector3 point, HashSet<GameObject> hitOnce)
    {
        if (string.IsNullOrEmpty(targetTag) || hitRadius <= 0f) return;
        foreach (var c in Physics.OverlapSphere(point + Vector3.up, hitRadius, ~0, QueryTriggerInteraction.Collide))
        {
            if (c.transform.IsChildOf(transform)) continue;
            GameObject go = c.attachedRigidbody ? c.attachedRigidbody.gameObject : c.gameObject;
            if (!go.CompareTag(targetTag) && !c.CompareTag(targetTag)) continue;
            if (!hitOnce.Add(go)) continue;
            GronkDamage.Apply(go, damage);
            onHitTarget?.Invoke(go);
        }
    }

    /// Đặt điểm xuống mặt đất (bỏ qua collider của chính Gronk và mục tiêu).
    Vector3 Ground(Vector3 p)
    {
        float bestDist = float.MaxValue; Vector3 best = p;
        foreach (var h in Physics.RaycastAll(p + Vector3.up * groundProbe, Vector3.down, groundProbe * 2f, groundMask, QueryTriggerInteraction.Ignore))
        {
            if (h.collider.transform.IsChildOf(transform)) continue;
            if (!string.IsNullOrEmpty(targetTag) && h.collider.CompareTag(targetTag)) continue;
            if (h.distance < bestDist) { bestDist = h.distance; best = h.point; }
        }
        return best;
    }

    // ---------- Tạo FX bằng code ----------
    GameObject CreateChargeFx(Transform parent)
    {
        var go = new GameObject("GronkChargeFx");
        go.transform.SetParent(parent, false);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = chargeDuration;
        main.loop = true;
        main.startLifetime = 0.5f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        main.startColor = chargeColor;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 200;

        var em = ps.emission; em.rateOverTime = 60f;
        var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 1.5f;
        var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local;
        vel.radial = -3f; // hút vào tâm kiếm
        var col = ps.colorOverLifetime; col.enabled = true; col.color = FadeInOut(chargeColor);
        var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0.2f));
        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = GetMaterial(true);
        ps.Play();

        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = chargeColor;
        light.range = 6f;
        light.intensity = 0f;
        return go;
    }

    LineRenderer CreateCrackLine()
    {
        var go = new GameObject("GronkGroundCrack");
        Track(go, -1f);
        var line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.sharedMaterial = GetMaterial(true);
        line.widthMultiplier = crackWidth;
        line.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.3f));
        line.startColor = crackColor;
        line.endColor = new Color(crackColor.r, crackColor.g, crackColor.b, 0.4f);
        line.numCapVertices = 2;
        line.alignment = LineAlignment.TransformZ;
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // trục Z hướng xuống -> vết nứt nằm phẳng trên mặt đất
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return line;
    }

    void Flash(Vector3 at)
    {
        var go = new GameObject("GronkSlashFlash");
        go.transform.position = at + Vector3.up;
        Track(go, 0.5f);
        var l = go.AddComponent<Light>();
        l.type = LightType.Point; l.color = crackColor; l.range = 10f; l.intensity = chargeLightIntensity * 1.5f;
        StartCoroutine(FadeLight(l, 0.4f));
    }

    IEnumerator FadeLight(Light l, float time)
    {
        float start = l ? l.intensity : 0f, t = 0f;
        while (l && t < time) { t += Time.deltaTime; l.intensity = start * (1f - t / time); yield return null; }
    }

    /// Tạo (1 lần) 2 hệ hạt world-space dùng chung: tia lửa (additive) + đá vụn (alpha).
    void EnsureParticles()
    {
        if (!sparks) sparks = CreateBurstSystem("GronkSlashSparks", GetMaterial(true), crackColor, 0.6f, new Vector2(0.08f, 0.18f), 1f);
        if (!debris) debris = CreateBurstSystem("GronkSlashDebris", GetMaterial(false), debrisColor, 1.1f, new Vector2(0.15f, 0.4f), 2.5f);
    }

    ParticleSystem CreateBurstSystem(string name, Material mat, Color color, float lifetime, Vector2 size, float gravity)
    {
        var go = new GameObject(name);
        Track(go, -1f);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true; // chạy liên tục (emission tắt) để các hạt Emit() luôn được mô phỏng
        main.playOnAwake = false;
        main.startLifetime = lifetime;
        main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
        main.startColor = color;
        main.gravityModifier = gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 1000;
        var em = ps.emission; em.enabled = false;
        var shape = ps.shape; shape.enabled = false;
        var col = ps.colorOverLifetime; col.enabled = true; col.color = FadeInOut(color);
        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
        ps.Play();
        return ps;
    }

    void Burst(Vector3 at, int count, float power)
    {
        var p = new ParticleSystem.EmitParams();
        for (int i = 0; i < count; i++)
        {
            p.position = at + Random.insideUnitSphere * 0.3f;
            Vector3 v = Random.insideUnitSphere * 2f; v.y = Mathf.Abs(v.y) + 4f;
            p.velocity = v * power;
            p.applyShapeToPosition = false;
            debris.Emit(p, 1);
            p.velocity = v * power * 1.5f;
            sparks.Emit(p, 1);
        }
    }

    static Gradient FadeInOut(Color c)
    {
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
        return g;
    }

    Material glowRuntime, dustRuntime;

    Material GetMaterial(bool additive)
    {
        if (additive && glowMaterial) return glowMaterial;
        if (!additive && dustMaterial) return dustMaterial;
        ref Material m = ref additive ? ref glowRuntime : ref dustRuntime;
        if (m) return m;

        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (!sh) sh = Shader.Find("Sprites/Default");
        m = new Material(sh) { name = additive ? "GronkGlow (runtime)" : "GronkDust (runtime)" };
        m.mainTexture = SoftTexture();
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", SoftTexture());
        if (m.HasProperty("_Surface"))
        {
            // URP Particles/Unlit: Transparent, Additive hoặc Alpha
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", additive ? 2f : 0f);
            m.SetFloat("_SrcBlend", additive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", additive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        return m;
    }

    /// Texture tròn mềm 32x32 để hạt không bị hình vuông.
    static Texture2D SoftTexture()
    {
        if (softTex) return softTex;
        const int n = 32;
        softTex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "GronkSoftDot" };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(n - 1, n - 1) * 0.5f) / (n * 0.5f);
                float a = Mathf.Clamp01(1f - d); a *= a;
                softTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        softTex.Apply();
        return softTex;
    }

    void Track(GameObject go, float life)
    {
        spawned.Add(go);
        if (life > 0f) Destroy(go, life);
    }

    void OnDisable()
    {
        StopAllCoroutines();
        chargeRoutine = null;
        ClearCharge();
        foreach (var go in spawned) if (go) Destroy(go);
        spawned.Clear();
        sparks = debris = null;
    }

    void OnDestroy()
    {
        if (glowRuntime) Destroy(glowRuntime);
        if (dustRuntime) Destroy(dustRuntime);
    }

    void OnDrawGizmosSelected()
    {
        Vector3 dir = transform.forward; dir.y = 0f;
        Vector3 start = origin ? origin.position : transform.position + dir.normalized * startOffset;
        Gizmos.color = new Color(1f, 0.5f, 0.1f);
        Gizmos.DrawLine(start, start + dir.normalized * length);
        Gizmos.DrawWireSphere(start + dir.normalized * length, hitRadius);
    }
}
