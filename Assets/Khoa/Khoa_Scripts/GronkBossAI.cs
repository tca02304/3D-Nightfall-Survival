using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Hành vi boss Gronk:
///  State 1 - Chase  : target Player, chạy tới (Blend Tree -> Run_Hogoblin).
///  State 2 - Melee  : Player trong phạm vi meleeRange (5) -> Attack1_hogblin.
///  State 3 - Ranged : Player ngoài phạm vi rangedMinDistance (20) VÀ đã đánh gần >= meleeCountToUnlockRanged (2) lần -> Ranged_Hogoblin.
/// Mỗi đòn luôn chạy hết thời lượng animation rồi mới sang state khác, sau đó chờ hồi chiêu.
/// Sát thương / bắn đạn vẫn do Animation Event trong clip gọi sang các script có sẵn trên Gronk.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class GronkBossAI : MonoBehaviour
{
    public enum State { Chase, MeleeAttack, RangedAttack }

    [Header("Target")]
    [Tooltip("Để trống: tự tìm GameObject có tag Player")]
    public Transform player;
    public string playerTag = "Player";
    [Tooltip("Mỗi bao nhiêu giây tìm lại Player gần nhất")]
    public float retargetInterval = 1f;

    [Header("Khoảng cách")]
    public float meleeRange = 5f;
    public float rangedMinDistance = 20f;

    [Header("Điều kiện tấn công xa")]
    public int meleeCountToUnlockRanged = 2;
    [Tooltip("Bật: sau mỗi lần tấn công xa phải đánh gần đủ số lần lại mới được bắn tiếp")]
    public bool resetMeleeCountAfterRanged = false;

    [Header("Hồi chiêu (giây, tính từ lúc animation đánh kết thúc)")]
    [Tooltip("Hồi chiêu chung: sau BẤT KỲ đòn nào phải chờ chừng này mới được ra đòn tiếp")]
    public float attackCooldown = 1.5f;
    [Tooltip("Hồi chiêu riêng của đòn đánh gần")]
    public float meleeCooldown = 2f;
    [Tooltip("Hồi chiêu riêng của đòn tấn công xa")]
    public float rangedCooldown = 5f;
    public float turnSpeed = 540f;

    [Header("Animator - di chuyển")]
    public string locomotionState = "Blend Tree";
    [Tooltip("Tham số float điều khiển blend tree (Idle -> Run_Hogoblin)")]
    public string speedParameter = "Speed";
    [Tooltip("Giá trị tham số ứng với Run_Hogoblin trong blend tree")]
    public float runBlendValue = 1f;
    public float blendDamp = 0.1f;

    [Header("Animator - tấn công (layer được tự dò)")]
    public string meleeState = "Attack1_hogblin";
    public string rangedState = "Ranged_Hogoblin";
    public float crossFade = 0.1f;

    [Header("Debug (chỉ đọc)")]
    [SerializeField] State state = State.Chase;
    [SerializeField] int meleeCount;
    [SerializeField] float cooldownLeft;
    [Tooltip("Lý do Gronk đang làm gì / vì sao đứng im")]
    [SerializeField] string status;
    [SerializeField] float playerDistance;

    NavMeshAgent agent;
    Animator animator;
    GronkSwordThrower swordThrower;
    bool hasSpeedParam;
    int meleeLayer = -1, rangedLayer = -1;
    float nextAttackTime, nextMeleeTime, nextRangedTime;
    Coroutine attackRoutine;
    float nextRetargetTime;
    readonly System.Collections.Generic.HashSet<string> warned = new System.Collections.Generic.HashSet<string>();

    void WarnOnce(string key, string msg)
    {
        status = msg;
        if (warned.Add(key)) Debug.LogWarning("[GronkBossAI] " + msg, this);
    }

    public State CurrentState => state;
    public int MeleeCount => meleeCount;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        swordThrower = GetComponent<GronkSwordThrower>();
    }

    void Start()
    {
        // stoppingDistance gốc của prefab là 7 > meleeRange 5 -> Gronk sẽ dừng trước khi vào tầm đánh.
        agent.stoppingDistance = Mathf.Min(agent.stoppingDistance, meleeRange * 0.8f);
        ValidateAnimator();
        FindPlayer();
    }

    // ---------- Animator setup ----------
    void ValidateAnimator()
    {
        if (!animator) { Debug.LogError("[GronkBossAI] Không tìm thấy Animator.", this); return; }

        foreach (var p in animator.parameters)
            if (p.name == speedParameter && p.type == AnimatorControllerParameterType.Float) hasSpeedParam = true;
        if (!hasSpeedParam)
        {
            var names = string.Join(", ", System.Array.ConvertAll(animator.parameters, p => p.name + "(" + p.type + ")"));
            Debug.LogWarning($"[GronkBossAI] Animator không có tham số float '{speedParameter}'. Các tham số hiện có: {names}.", this);
        }

        meleeLayer = FindLayerOf(meleeState);
        rangedLayer = FindLayerOf(rangedState);
        if (FindLayerOf(locomotionState) < 0)
            Debug.LogWarning($"[GronkBossAI] Không có state '{locomotionState}' ở layer nào của Animator.", this);
    }

    /// Trả về layer đầu tiên chứa state, -1 nếu không có (kèm cảnh báo).
    int FindLayerOf(string stateName)
    {
        if (string.IsNullOrEmpty(stateName) || !animator) return -1;
        int hash = Animator.StringToHash(stateName);
        for (int l = 0; l < animator.layerCount; l++)
        {
            if (!animator.HasState(l, hash)) continue;
            if (l > 0 && animator.GetLayerWeight(l) <= 0f)
                Debug.LogWarning($"[GronkBossAI] State '{stateName}' nằm ở layer '{animator.GetLayerName(l)}' nhưng layer này có Weight = 0 nên sẽ không thấy animation.", this);
            return l;
        }
        if (stateName != locomotionState)
            Debug.LogWarning($"[GronkBossAI] Không tìm thấy state '{stateName}' ở layer nào (kiểm tra lại chính tả, phân biệt hoa/thường).", this);
        return -1;
    }

    /// Chọn Player gần nhất có tag playerTag (hỗ trợ nhiều Player / Player spawn sau).
    void FindPlayer()
    {
        nextRetargetTime = Time.time + retargetInterval;
        GameObject[] all;
        try { all = GameObject.FindGameObjectsWithTag(playerTag); }
        catch (UnityException) { WarnOnce("tag", $"Tag '{playerTag}' chưa được khai báo trong Tags & Layers."); return; }

        Transform best = null; float bestD = float.MaxValue;
        foreach (var go in all)
        {
            if (!go.activeInHierarchy || go.transform.IsChildOf(transform)) continue;
            float d = FlatDistance(go.transform.position);
            if (d < bestD) { bestD = d; best = go.transform; }
        }
        if (best) player = best;
    }

    // ---------- Main loop ----------
    void Update()
    {
        cooldownLeft = Mathf.Max(0f, nextAttackTime - Time.time);
        if (state != State.Chase) return; // đang ra đòn: coroutine điều khiển cho tới khi animation xong

        if (!player || Time.time >= nextRetargetTime) FindPlayer();
        if (!player)
        {
            WarnOnce("noplayer", $"Không tìm thấy GameObject nào đang bật có tag '{playerTag}'. Gắn tag đó cho Player hoặc kéo Player vào ô Player.");
            SetRunBlend(false);
            return;
        }

        float dist = FlatDistance(player.position);
        playerDistance = dist;
        bool globalReady = Time.time >= nextAttackTime;

        if (dist <= meleeRange && meleeLayer < 0)
            WarnOnce("nomelee", $"Player trong tầm đánh nhưng không có state '{meleeState}' trong Animator.");
        if (globalReady && dist <= meleeRange && Time.time >= nextMeleeTime && meleeLayer >= 0)
        {
            status = "Đánh gần";
            attackRoutine = StartCoroutine(Attack(State.MeleeAttack, meleeState, meleeLayer));
            return;
        }
        if (globalReady && dist > rangedMinDistance && meleeCount >= meleeCountToUnlockRanged
            && Time.time >= nextRangedTime && rangedLayer >= 0)
        {
            status = "Tấn công xa";
            attackRoutine = StartCoroutine(Attack(State.RangedAttack, rangedState, rangedLayer));
            return;
        }
        Chase(dist);
    }

    // ---------- State 1: Chase ----------
    void Chase(float dist)
    {
        if (!agent.enabled)
        {
            WarnOnce("agentoff", "NavMeshAgent đang bị tắt (có thể do script AI cũ). Gronk không thể di chuyển.");
            FacePlayer();
            return;
        }
        if (!agent.isOnNavMesh)
        {
            // thử kéo Gronk về điểm NavMesh gần nhất
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 10f, NavMesh.AllAreas)) agent.Warp(hit.position);
            else WarnOnce("nonavmesh", "Gronk không đứng trên NavMesh (chưa Bake NavMesh, hoặc NavMesh không khớp Agent Type/kích thước của Gronk).");
            FacePlayer();
            return;
        }

        if (dist <= meleeRange)
        {
            status = meleeLayer < 0 ? status : $"Trong tầm đánh, đang hồi chiêu ({cooldownLeft:0.0}s)";
            // trong tầm đánh nhưng đang hồi chiêu: đứng yên, quay mặt về Player
            agent.isStopped = true;
            agent.updateRotation = false;
            FacePlayer();
            SetRunBlend(false);
            return;
        }
        agent.isStopped = false;
        agent.updateRotation = true;
        if (!agent.SetDestination(player.position))
            WarnOnce("nopath", "SetDestination thất bại: vị trí Player không tới được trên NavMesh.");
        else status = agent.pathPending ? "Đang tính đường" : $"Đuổi theo {player.name} ({dist:0.0}m)";
        SetRunBlend(agent.velocity.sqrMagnitude > 0.01f);
    }

    // ---------- State 2 & 3: Attack ----------
    IEnumerator Attack(State attackState, string animState, int layer)
    {
        state = attackState;
        if (attackState == State.MeleeAttack) meleeCount++;

        if (agent.isOnNavMesh) { agent.isStopped = true; agent.ResetPath(); }
        agent.updateRotation = false;
        SetRunBlend(false, true);

        // quay mặt về Player trước khi ra đòn
        float t = 0f;
        while (t < 0.15f) { FacePlayer(); t += Time.deltaTime; yield return null; }

        int hash = Animator.StringToHash(animState);
        animator.CrossFadeInFixedTime(hash, crossFade, layer);
        yield return null;

        // chờ Animator vào state (tối đa 1s)
        float timeout = 1f;
        while (timeout > 0f && !IsIn(hash, layer)) { timeout -= Time.deltaTime; yield return null; }

        // chạy HẾT animation: đợi đủ thời lượng clip, kể cả khi controller có transition thoát sớm
        float length = IsIn(hash, layer) ? StateInfo(layer).length : 0f;
        if (attackState == State.RangedAttack && swordThrower) swordThrower.BeginThrowSequence(length);
        float elapsed = 0f;
        while (elapsed < length)
        {
            FacePlayer(); // kiếm ném ra theo hướng mặt boss -> luôn hướng về Player
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (attackState == State.RangedAttack && swordThrower) swordThrower.EndThrowSequence();

        // hồi chiêu bắt đầu tính từ lúc animation kết thúc
        nextAttackTime = Time.time + attackCooldown;
        if (attackState == State.MeleeAttack) nextMeleeTime = Time.time + meleeCooldown;
        else
        {
            nextRangedTime = Time.time + rangedCooldown;
            if (resetMeleeCountAfterRanged) meleeCount = 0;
        }

        // nếu controller không tự thoát khỏi state đánh thì đưa layer đó về Blend Tree
        if (IsIn(hash, layer) && animator.HasState(layer, Animator.StringToHash(locomotionState)))
            animator.CrossFadeInFixedTime(locomotionState, crossFade, layer);

        agent.updateRotation = true;
        state = State.Chase;
        attackRoutine = null;
    }

    AnimatorStateInfo StateInfo(int layer) =>
        animator.IsInTransition(layer) ? animator.GetNextAnimatorStateInfo(layer)
                                       : animator.GetCurrentAnimatorStateInfo(layer);

    bool IsIn(int hash, int layer) => StateInfo(layer).shortNameHash == hash;

    // ---------- helpers ----------
    void SetRunBlend(bool running, bool instant = false)
    {
        if (!hasSpeedParam) return;
        float target = running ? runBlendValue : 0f;
        if (instant) animator.SetFloat(speedParameter, target);
        else animator.SetFloat(speedParameter, target, blendDamp, Time.deltaTime);
    }

    void FacePlayer()
    {
        Vector3 dir = player.position - transform.position; dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
    }

    float FlatDistance(Vector3 p)
    {
        Vector3 d = p - transform.position; d.y = 0f;
        return d.magnitude;
    }

    void OnDisable()
    {
        if (attackRoutine != null) StopCoroutine(attackRoutine);
        attackRoutine = null;
        if (state == State.RangedAttack && swordThrower) swordThrower.EndThrowSequence(false);
        state = State.Chase;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, meleeRange);
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, rangedMinDistance);
    }
}
