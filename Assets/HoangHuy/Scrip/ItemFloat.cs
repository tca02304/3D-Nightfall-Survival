using UnityEngine;

public class ItemFloat : MonoBehaviour
{
    [Header("Cấu hình hiệu ứng lơ lửng")]
    public float amplitude = 0.2f;   // Biên độ (độ cao lên xuống nhiều hay ít)
    public float frequency = 2f;     // Tốc độ dao động nhanh hay chậm
    public float rotationSpeed = 50f; // Tốc độ xoay tròn quanh trục Y (tùy chọn)

    private Vector3 startPos;

    void Start()
    {
        // Lưu lại vị trí ban đầu khi vừa sinh ra
        startPos = transform.localPosition;
    }

    void Update()
    {
        // Dùng hàm Mathf.Sin để tạo chuyển động lên xuống mượt mà
        float newY = startPos.y + Mathf.Sin(Time.time * frequency) * amplitude;
        transform.localPosition = new Vector3(startPos.x, newY, startPos.z);

        // Xoay tròn vật phẩm quanh trục Y để bắt mắt hơn
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }
}