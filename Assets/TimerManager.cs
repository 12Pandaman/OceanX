using UnityEngine;
using TMPro;

/// <summary>
/// TimerManager
/// นับเวลาแบบ Count Up (เพิ่มทีละวินาที)
/// แสดงผลในรูปแบบ MM:SS เหมือนในหน้า Victory "03:00"
///
/// วิธีใช้ (Prefab-friendly):
/// 1. สร้าง Empty GameObject ชื่อ "TimerManager" → ติด Script นี้ → ทำเป็น Prefab
/// 2. ตั้งชื่อ TMP Text ที่แสดงเวลาในเกมว่า "TimerText" (ใช้ Tag หรือ Name)
///    Script จะหาให้เองอัตโนมัติตอน Awake — ไม่ต้องลาก reference!
/// 3. (ไม่บังคับ) ถ้าอยากลาก reference เองก็ยังทำได้เหมือนเดิม
/// </summary>
public class TimerManager : MonoBehaviour
{
    [Header("UI Reference (ไม่บังคับ — ถ้าว่างจะหาจาก Tag 'TimerText' อัตโนมัติ)")]
    [Tooltip("TextMeshPro ที่แสดงเวลา เช่น '03:00' — ถ้าไม่ลากจะหาจาก GameObject.FindWithTag")]
    public TMP_Text timerText;

    [Header("Settings")]
    [Tooltip("Tag ของ TMP Text ที่ใช้แสดงเวลา (ใช้เมื่อไม่ได้ลาก reference)")]
    public string timerTextTag = "TimerText";

    [Tooltip("เริ่มนับเวลาอัตโนมัติตอน Start")]
    public bool autoStart = true;

    // เวลาที่ผ่านไปในหน่วยวินาที
    private float elapsedTime = 0f;
    private bool isRunning = false;

    private void Awake()
    {
        // ถ้าไม่ได้ลาก reference → หา TMP Text จาก Tag อัตโนมัติ
        if (timerText == null && !string.IsNullOrEmpty(timerTextTag))
        {
            GameObject found = GameObject.FindWithTag(timerTextTag);
            if (found != null)
                timerText = found.GetComponent<TMP_Text>();

            if (timerText == null)
                Debug.LogWarning($"[TimerManager] ไม่พบ TMP_Text ที่มี Tag '{timerTextTag}' — ตรวจสอบ Tag ใน Inspector ของ Text GameObject");
        }
    }

    private void Start()
    {
        if (autoStart)
            StartTimer();
    }

    private void Update()
    {
        if (!isRunning) return;

        elapsedTime += Time.deltaTime;
        UpdateTimerDisplay();
    }

    /// <summary>
    /// เริ่มนับเวลา
    /// </summary>
    public void StartTimer()
    {
        isRunning = true;
    }

    /// <summary>
    /// หยุดนับเวลา
    /// </summary>
    public void StopTimer()
    {
        isRunning = false;
    }

    /// <summary>
    /// Reset เวลากลับเป็น 0
    /// </summary>
    public void ResetTimer()
    {
        elapsedTime = 0f;
        isRunning = false;
        UpdateTimerDisplay();
    }

    /// <summary>
    /// คืนค่าเวลาที่ผ่านไป (วินาที)
    /// </summary>
    public float GetElapsedTime()
    {
        return elapsedTime;
    }

    /// <summary>
    /// คืนค่าเวลาในรูปแบบ string MM:SS
    /// </summary>
    public string GetFormattedTime()
    {
        return FormatTime(elapsedTime);
    }

    /// <summary>
    /// อัปเดต UI Text
    /// </summary>
    private void UpdateTimerDisplay()
    {
        if (timerText != null)
            timerText.text = FormatTime(elapsedTime);
    }

    /// <summary>
    /// แปลงวินาทีเป็น MM:SS
    /// </summary>
    private string FormatTime(float seconds)
    {
        int mins = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{mins:00}:{secs:00}";
    }
}
