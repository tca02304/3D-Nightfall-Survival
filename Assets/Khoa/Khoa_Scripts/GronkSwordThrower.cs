using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Ném đại kiếm (Cylinder) của Gronk trong state Ranged_Hogoblin.
/// Spawn bản sao tại đúng vị trí/góc kiếm trên tay theo thứ tự tay, kiếm trên tay ẩn đi và hiện lại khi animation kết thúc.
/// Bản sao bay thẳng về phía trước mặt boss tối đa `range`, xuyên qua mọi vật thể; hết tầm thì rơi xuống;
/// dừng lại (cắm xuống) khi chạm Terrain.
/// Mặc định GronkBossAI tự gọi ném theo mốc thời gian `throwTimes`; nếu bật useAnimationEvents
/// thì thêm Animation Event "ThrowNextSword" vào clip Ranged_Hogoblin.
/// </summary>
public class GronkSwordThrower : MonoBehaviour
{
    [Header("Kiếm trên tay (theo thứ tự ném) - để trống sẽ tự tìm: tay phải rồi tay trái")]
    public Transform[] swords;

    [Header("Thời điểm ném")]
    [Tooltip("Mốc normalized time (0-1) của Ranged_Hogoblin cho từng thanh kiếm")]
    public float[] throwTimes = { 0.3f, 0.55f };
    [Tooltip("Bật nếu muốn tự đặt Animation Event 'ThrowNextSword' trong clip thay cho throwTimes")]
    public bool useAnimationEvents = false;

    [Header("Bay")]
    public float range = 20f;
    public float speed = 30f;
    [Tooltip("Tốc độ xoay (độ/giây) quanh trục ngang của boss")]
    public float spinSpeed = 900f;
    [Tooltip("Gia tốc rơi sau khi bay hết tầm")]
    public float gravity = 40f;
    public float maxLifetime = 6f;

    [Header("Chạm Terrain")]
    public LayerMask groundMask = ~0;
    [Tooltip("Bật: chỉ dừng khi chạm TerrainCollider. Tắt: dừng khi chạm bất kỳ collider nào thuộc groundMask")]
    public bool onlyTerrainCollider = true;
    [Tooltip("Thời gian kiếm cắm trên đất trước khi biến mất")]
    public float stuckTime = 2f;
    public GameObject impactFx;

    [Header("Trúng mục tiêu trên đường bay (xuyên qua, mỗi mục tiêu 1 lần)")]
    public string targetTag = "Player";
    public float hitRadius = 1.5f;
    public UnityEvent<GameObject> onHitTarget;

    [Header("Kiếm trên tay hiện lại")]
    public float reappearDuration = 0.25f;

    int nextIndex;
    readonly List<GameObject> live = new List<GameObject>();
    Renderer[][] handRenderers;   // renderer mesh của từng kiếm (cả object con, trừ TrailRenderer)
    bool[] hidden;
    Vector3[] handScales;
    Transform[] trailTemplates;

    void Awake()
    {
        if (swords == null || swords.Length == 0)
        {
            var list = new List<Transform>();
            foreach (var n in new[] { "GoblinSword (1)", "GoblinSword" }) // Hand.R trước, Hand.L sau
            {
                var parent = FindDeep(transform, n);
                var cyl = parent ? parent.Find("Cylinder") : null;
                if (cyl) list.Add(cyl);
            }
            swords = list.ToArray();
            if (swords.Length == 0) Debug.LogWarning("[GronkSwordThrower] Không tìm thấy Cylinder dưới GoblinSword.", this);
        }
        handRenderers = new Renderer[swords.Length][];
        hidden = new bool[swords.Length];
        handScales = new Vector3[swords.Length];
        trailTemplates = new Transform[swords.Length];
        for (int i = 0; i < swords.Length; i++)
        {
            if (!swords[i]) { Debug.LogWarning($"[GronkSwordThrower] Swords[{i}] đang trống.", this); handRenderers[i] = new Renderer[0]; continue; }
            for (int j = 0; j < i; j++)
                if (swords[j] == swords[i])
                    Debug.LogWarning($"[GronkSwordThrower] Swords[{j}] và Swords[{i}] cùng là một object '{swords[i].name}'. Hãy kéo kiếm tay phải và kiếm tay trái riêng.", this);

            var list = new List<Renderer>();
            foreach (var r in swords[i].GetComponentsInChildren<Renderer>(true))
                if (!(r is TrailRenderer) && !(r is ParticleSystemRenderer)) list.Add(r);
            handRenderers[i] = list.ToArray();
            if (list.Count == 0) Debug.LogWarning($"[GronkSwordThrower] '{swords[i].name}' và các object con không có MeshRenderer nào.", this);
            handScales[i] = swords[i].localScale;

            // Trail: nếu kiếm đã chứa TrailRenderer bên trong thì bản sao tự có; nếu không thì lấy 'Trail' anh em (cấu trúc GoblinSword/Cylinder + Trail)
            bool hasOwnTrail = swords[i].GetComponentsInChildren<TrailRenderer>(true).Length > 0;
            trailTemplates[i] = !hasOwnTrail && swords[i].parent ? swords[i].parent.Find("Trail") : null;
        }
        if (!GetComponent<GronkBossAI>() && !useAnimationEvents)
            Debug.LogWarning("[GronkSwordThrower] Không có GronkBossAI trên cùng GameObject nên sẽ không tự ném. Gắn 2 script lên cùng object Gronk, hoặc bật Use Animation Events.", this);
    }

    void SetHandVisible(int i, bool visible)
    {
        foreach (var r in handRenderers[i]) if (r) r.enabled = visible;
        hidden[i] = !visible;
    }

    // ---------- gọi từ GronkBossAI ----------
    /// Bắt đầu chuỗi ném. stateLength = thời lượng state Ranged (giây).
    public void BeginThrowSequence(float stateLength)
    {
        StopPendingThrows();
        nextIndex = 0;
        if (useAnimationEvents || throwTimes == null || throwTimes.Length == 0) return;
        for (int i = 0; i < swords.Length; i++)
        {
            float t = i < throwTimes.Length ? throwTimes[i] : throwTimes[throwTimes.Length - 1];
            pending.Add(StartCoroutine(ThrowAfter(Mathf.Clamp01(t) * stateLength)));
        }
    }

    /// Kết thúc animation: ném nốt thanh còn sót (nếu có) rồi cho kiếm trên tay hiện lại.
    public void EndThrowSequence(bool throwRemaining = true)
    {
        StopPendingThrows();
        if (throwRemaining) while (nextIndex < swords.Length) ThrowNextSword();
        nextIndex = swords.Length;
        for (int i = 0; i < swords.Length; i++)
            if (hidden[i]) StartCoroutine(Reappear(i));
    }

    readonly List<Coroutine> pending = new List<Coroutine>();

    IEnumerator ThrowAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        ThrowNextSword();
    }

    void StopPendingThrows()
    {
        foreach (var c in pending) if (c != null) StopCoroutine(c);
        pending.Clear();
    }

    /// Animation Event: ném thanh kiếm kế tiếp theo thứ tự.
    public void ThrowNextSword()
    {
        if (nextIndex >= swords.Length) return;
        Throw(nextIndex++);
    }

    // ---------- ném ----------
    void Throw(int i)
    {
        Transform src = swords[i];
        if (!src) return;

        var clone = Instantiate(src.gameObject, src.position, src.rotation);
        clone.name = src.name + "_Thrown";
        clone.transform.localScale = src.lossyScale;
        foreach (var c in clone.GetComponentsInChildren<Collider>()) Destroy(c); // xuyên qua mọi vật thể
        foreach (var r in clone.GetComponentsInChildren<Renderer>(true)) if (!(r is TrailRenderer)) r.enabled = true;
        foreach (var tr in clone.GetComponentsInChildren<TrailRenderer>(true)) { tr.Clear(); tr.emitting = true; }

        if (trailTemplates[i])
        {
            var trail = Instantiate(trailTemplates[i].gameObject, trailTemplates[i].position, trailTemplates[i].rotation, clone.transform);
            foreach (var tr in trail.GetComponentsInChildren<TrailRenderer>()) { tr.Clear(); tr.emitting = true; }
        }

        SetHandVisible(i, false);
        live.Add(clone);

        Vector3 fwd = transform.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
        StartCoroutine(Fly(clone, fwd.normalized));
    }

    IEnumerator Fly(GameObject clone, Vector3 dir)
    {
        Transform t = clone.transform;
        Vector3 spinAxis = Vector3.Cross(Vector3.up, dir); // trục ngang -> xoay lộn về phía trước
        Vector3 velocity = dir * speed;
        float travelled = 0f, life = 0f;
        var hitOnce = new HashSet<GameObject>();

        while (life < maxLifetime && clone)
        {
            float dt = Time.deltaTime;
            life += dt;
            if (travelled >= range) velocity += Vector3.down * gravity * dt; // hết tầm: rơi xuống

            Vector3 from = t.position;
            Vector3 step = velocity * dt;
            if (travelled < range) // không bay quá 20 theo phương ngang
            {
                float remain = range - travelled;
                Vector3 flat = new Vector3(step.x, 0f, step.z);
                if (flat.magnitude > remain) step = step * (remain / Mathf.Max(flat.magnitude, 0.0001f));
                travelled += new Vector3(step.x, 0f, step.z).magnitude;
            }
            Vector3 to = from + step;

            DamageAlong(from, to, hitOnce);

            if (GroundHit(from, to, out RaycastHit hit))
            {
                t.position = hit.point;
                StopTrails(clone);
                if (impactFx) Instantiate(impactFx, hit.point, Quaternion.LookRotation(hit.normal));
                yield return new WaitForSeconds(stuckTime);
                break;
            }

            t.position = to;
            t.Rotate(spinAxis, spinSpeed * dt, Space.World);
            yield return null;
        }
        if (clone) { live.Remove(clone); Destroy(clone); }
    }

    bool GroundHit(Vector3 from, Vector3 to, out RaycastHit best)
    {
        best = default;
        Vector3 d = to - from;
        float len = d.magnitude;
        if (len < 0.0001f) return false;
        bool found = false;
        foreach (var h in Physics.RaycastAll(from, d / len, len, groundMask, QueryTriggerInteraction.Ignore))
        {
            if (onlyTerrainCollider && !(h.collider is TerrainCollider)) continue;
            if (!found || h.distance < best.distance) { best = h; found = true; }
        }
        return found;
    }

    void DamageAlong(Vector3 from, Vector3 to, HashSet<GameObject> hitOnce)
    {
        if (string.IsNullOrEmpty(targetTag) || hitRadius <= 0f) return;
        Vector3 d = to - from;
        float len = d.magnitude;
        var hits = len > 0.0001f
            ? Physics.SphereCastAll(from, hitRadius, d / len, len, ~0, QueryTriggerInteraction.Collide)
            : new RaycastHit[0];
        foreach (var h in hits)
        {
            GameObject go = h.collider.attachedRigidbody ? h.collider.attachedRigidbody.gameObject : h.collider.gameObject;
            if (!go.CompareTag(targetTag) && !h.collider.CompareTag(targetTag)) continue;
            if (hitOnce.Add(go)) onHitTarget?.Invoke(go);
        }
    }

    static void StopTrails(GameObject go)
    {
        foreach (var tr in go.GetComponentsInChildren<TrailRenderer>()) tr.emitting = false;
    }

    IEnumerator Reappear(int i)
    {
        Transform s = swords[i];
        if (s) s.localScale = Vector3.zero;
        SetHandVisible(i, true);
        float t = 0f;
        while (t < reappearDuration && s)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / reappearDuration);
            s.localScale = handScales[i] * k;
            yield return null;
        }
        if (s) s.localScale = handScales[i];
    }

    void OnDisable()
    {
        StopAllCoroutines();
        pending.Clear();
        if (handRenderers != null)
            for (int i = 0; i < swords.Length; i++)
            {
                SetHandVisible(i, true);
                if (swords[i]) swords[i].localScale = handScales[i];
            }
        foreach (var c in live) if (c) Destroy(c);
        live.Clear();
    }

    static Transform FindDeep(Transform p, string name)
    {
        if (p.name == name) return p;
        foreach (Transform c in p) { var r = FindDeep(c, name); if (r) return r; }
        return null;
    }
}
