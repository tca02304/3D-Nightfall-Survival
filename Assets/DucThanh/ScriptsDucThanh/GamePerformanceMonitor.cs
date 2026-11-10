using UnityEngine;
using UnityEngine.Profiling;
using System;
using Process = System.Diagnostics.Process;

public class SystemInfoDisplay : MonoBehaviour
{
    [Header("Performance Settings")]
    public float updateInterval = 0.5f;
    public float spikeThreshold = 33.3f;

    [Header("Minimum FPS Settings")]
    public float startRecordTime = 5f;
    public float maxValidFrameTime = 0.1f;

    private Process process;

    // CPU
    private float cpuUsage;
    private TimeSpan lastCpuTime;
    private float lastCpuUpdateTime;

    // RAM
    private float ramUsage;
    private float ramMB;

    // FPS và Frame Time
    private float fps;
    private float frameTime;

    // FPS thấp nhất
    private float minFPS = float.MaxValue;
    private float elapsedRecordTime = 0f;
    private bool isRecordingMinFPS = false;

    // Unity Memory
    private float unityAllocatedMB;
    private float unityReservedMB;

    // GC
    private int lastGC0;
    private int lastGC1;
    private int lastGC2;
    private float lastGCTime = -999f;

    // Frame Spike
    private float performanceTimer;
    private float nextLogTime;

    void Start()
    {
        try
        {
            process = Process.GetCurrentProcess();
            process.Refresh();

            lastCpuTime = process.TotalProcessorTime;
            lastCpuUpdateTime = Time.realtimeSinceStartup;

            ramMB = process.WorkingSet64 / (1024f * 1024f);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Không thể lấy thông tin Process: " + e.Message);
        }

        lastGC0 = GC.CollectionCount(0);
        lastGC1 = GC.CollectionCount(1);
        lastGC2 = GC.CollectionCount(2);
    }

    void Update()
    {
        // FPS và Frame Time
        frameTime = Time.unscaledDeltaTime * 1000f;

        fps = frameTime > 0 ? 1000f / frameTime : 0;

        // Cập nhật FPS thấp nhất
        UpdateMinFPS();

        // Kiểm tra GC
        CheckGarbageCollection();

        // Kiểm tra Frame Spike
        CheckFrameSpike();

        // Cập nhật CPU và RAM định kỳ
        performanceTimer += Time.unscaledDeltaTime;

        if (performanceTimer >= updateInterval)
        {
            UpdateSystemInfo();
            performanceTimer = 0f;
        }
    }

    // Ghi nhận FPS thấp nhất sau 5 giây
    private void UpdateMinFPS()
    {
        // Chờ game khởi động ổn định
        if (!isRecordingMinFPS)
        {
            elapsedRecordTime += Time.unscaledDeltaTime;

            if (elapsedRecordTime >= startRecordTime)
            {
                isRecordingMinFPS = true;
                minFPS = float.MaxValue;

                Debug.Log("Bắt đầu ghi nhận Lowest FPS!");
            }

            return;
        }

        // Bỏ qua frame quá chậm do tải tài nguyên
        if (Time.unscaledDeltaTime > maxValidFrameTime)
            return;

        // Ghi nhận FPS thấp nhất
        if (fps > 0 && fps < minFPS)
        {
            minFPS = fps;
        }
    }

    // Cập nhật CPU và RAM
    void UpdateSystemInfo()
    {
        try
        {
            if (process != null)
            {
                process.Refresh();

                // RAM của tiến trình game
                ramMB = process.WorkingSet64 / (1024f * 1024f);

                float totalRAM = SystemInfo.systemMemorySize;

                if (totalRAM > 0)
                {
                    ramUsage = ramMB / (totalRAM * 1024f) * 100f;
                }

                // CPU của tiến trình game
                TimeSpan currentCpuTime = process.TotalProcessorTime;

                float currentTime = Time.realtimeSinceStartup;

                float elapsedTime = currentTime - lastCpuUpdateTime;

                if (elapsedTime > 0.001f)
                {
                    float cpuTime = (float)(
                        (currentCpuTime - lastCpuTime).TotalSeconds
                    );

                    cpuUsage = (
                        cpuTime /
                        (elapsedTime * SystemInfo.processorCount)
                    ) * 100f;

                    cpuUsage = Mathf.Clamp(cpuUsage, 0f, 100f);
                }

                lastCpuTime = currentCpuTime;
                lastCpuUpdateTime = currentTime;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("Lỗi đọc CPU/RAM: " + e.Message);
        }

        // Bộ nhớ Unity
        unityAllocatedMB =
            Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);

        unityReservedMB =
            Profiler.GetTotalReservedMemoryLong() / (1024f * 1024f);
    }

    // Kiểm tra Garbage Collection
    void CheckGarbageCollection()
    {
        int gc0 = GC.CollectionCount(0);
        int gc1 = GC.CollectionCount(1);
        int gc2 = GC.CollectionCount(2);

        if (gc0 != lastGC0 || gc1 != lastGC1 || gc2 != lastGC2)
        {
            lastGCTime = Time.unscaledTime;
        }

        lastGC0 = gc0;
        lastGC1 = gc1;
        lastGC2 = gc2;
    }

    // Kiểm tra Frame Spike
    void CheckFrameSpike()
    {
        if (frameTime >= spikeThreshold &&
            Time.unscaledTime >= nextLogTime)
        {
            bool gcRecently =
                Time.unscaledTime - lastGCTime <= 1f;

            Debug.LogWarning(
                "===== FRAME SPIKE DETECTED =====\n" +
                "Frame Time: " + frameTime.ToString("F2") + " ms\n" +
                "FPS: " + fps.ToString("F1") + "\n" +
                "Lowest FPS: " +
                (minFPS == float.MaxValue
                    ? "Waiting..."
                    : minFPS.ToString("F1")) + "\n" +
                "CPU: " + cpuUsage.ToString("F2") + "%\n" +
                "RAM: " + ramMB.ToString("F2") + " MB\n" +
                "GC Recently: " + (gcRecently ? "YES" : "NO") + "\n" +
                "Unity Memory: " + unityAllocatedMB.ToString("F2") + " MB\n" +
                "================================"
            );

            nextLogTime = Time.unscaledTime + 1f;
        }
    }

    // Hiển thị thông số trên màn hình
    void OnGUI()
    {
        GUI.Box(new Rect(10, 10, 330, 320), "GAME PERFORMANCE");

        GUI.Label(new Rect(25, 40, 300, 22),
            $"CPU Usage: {cpuUsage:F2}%");

        GUI.Label(new Rect(25, 65, 300, 22),
            $"RAM Usage: {ramUsage:F2}%");

        GUI.Label(new Rect(25, 90, 300, 22),
            $"RAM Used: {ramMB:F2} MB");

        GUI.Label(new Rect(25, 120, 300, 22),
            $"FPS: {fps:F1}");

        GUI.Label(new Rect(25, 145, 300, 22),
            "Lowest FPS: " +
            (isRecordingMinFPS && minFPS != float.MaxValue
                ? minFPS.ToString("F1")
                : "Waiting..."));

        GUI.Label(new Rect(25, 170, 300, 22),
            $"Frame Time: {frameTime:F2} ms");

        GUI.Label(new Rect(25, 195, 300, 22),
            $"Unity Allocated: {unityAllocatedMB:F2} MB");

        GUI.Label(new Rect(25, 220, 300, 22),
            $"Unity Reserved: {unityReservedMB:F2} MB");

        bool gcRecently =
            Time.unscaledTime - lastGCTime <= 1f;

        GUI.Label(new Rect(25, 245, 300, 22),
            $"GC Recently: {(gcRecently ? "YES" : "NO")}");

        GUI.Label(new Rect(25, 270, 300, 22),
            $"GPU: {SystemInfo.graphicsDeviceName}");

        GUI.Label(new Rect(25, 295, 300, 22),
            $"VRAM: {SystemInfo.graphicsMemorySize} MB");
    }

    void OnDestroy()
    {
        process?.Dispose();
    }
}