using UnityEngine;

public class FallDebugger : MonoBehaviour
{
    [SerializeField] private float fallThresholdY = -2f;

    private CharacterController cc;
    private Rigidbody rb;
    private Vector3 lastSafePos;
    private string debugReport = "Đang theo dõi...";
    private bool hasFallen = false;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();
        lastSafePos = transform.position;
    }

    private void Update()
    {
        // Luôn cập nhật thông số trực tiếp lên màn hình
        string ccInfo = cc != null ? $"CC: Enabled={cc.enabled} | Grounded={cc.isGrounded} | Center={cc.center}" : "Không có CC";
        string rbInfo = rb != null ? $"RB: Kinematic={rb.isKinematic} | Gravity={rb.useGravity} | Vel={rb.linearVelocity}" : "Không có RB";

        // Bắn tia kiểm tra xem ngay dưới chân Player có mặt đất có Collider không
        bool hasGroundBelow = Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 5f);
        string groundInfo = hasGroundBelow ? $"Đất dưới chân: {hit.collider.name} (cách {hit.distance:F2}m)" : "CẢNH BÁO: KHÔNG CÓ ĐẤT DƯỚI CHÂN!";

        if (!hasFallen)
        {
            debugReport = $"Pos: {transform.position}\n{ccInfo}\n{rbInfo}\n{groundInfo}";
        }

        if (transform.position.y >= fallThresholdY)
        {
            lastSafePos = transform.position;
        }
        else if (!hasFallen)
        {
            hasFallen = true;
            debugReport = $"[ĐÃ RƠI XUYÊN MAP!]\nVị trí an toàn cuối: {lastSafePos}\nVị trí rơi: {transform.position}\n{ccInfo}\n{rbInfo}\n{groundInfo}";
            Debug.Break(); // Tự động Pause game
        }
    }

    // In thẳng bảng thông báo lên góc trái màn hình Game (không cần mở Console)
    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 16;
        style.alignment = TextAnchor.UpperLeft;
        style.normal.textColor = hasFallen ? Color.red : Color.yellow;

        GUI.Box(new Rect(15, 15, 550, 130), debugReport, style);
    }
}