using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// VictoryScreenManager
/// จัดการหน้า Victory Screen:
/// - แสดง Panel "VICTORY"
/// - แสดง Score Time ในรูปแบบ MM:SS
/// - ปุ่ม "Play Again" reload Scene ใหม่
///
/// วิธีใช้:
/// 1. สร้าง Panel ใน Canvas ชื่อ "VictoryPanel" (ซ่อนไว้ก่อน: SetActive false)
/// 2. ใส่ TMP Text สำหรับ score time (ดูตัวอย่างใน Inspector)
/// 3. ติด Script นี้กับ GameObject ใดก็ได้
/// 4. ลาก victoryPanel, scoreTimeText, และ playAgainButton ใส่ใน Inspector
/// 5. ผูก PlayAgain() กับปุ่ม Play Again
/// </summary>
public class VictoryScreenManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Panel หลักของหน้า Victory (ปกติ SetActive = false)")]
    public GameObject victoryPanel;

    [Tooltip("Text แสดงเวลา Score เช่น '03:00'")]
    public TMP_Text scoreTimeText;

    [Header("Settings")]
    [Tooltip("ชื่อ Scene ที่จะ reload เมื่อกด Play Again (ถ้าว่างจะ reload Scene ปัจจุบัน)")]
    public string playAgainSceneName = "";

    [Tooltip("หน่วงเวลาก่อนแสดงหน้า Victory (วินาที)")]
    public float showDelay = 0.5f;

    private void Start()
    {
        // ซ่อน Victory Panel ตอนเริ่ม
        if (victoryPanel != null)
            victoryPanel.SetActive(false);
    }

    /// <summary>
    /// เรียกจาก JigsawPuzzleManager เมื่อ puzzle เสร็จ
    /// </summary>
    /// <param name="elapsedSeconds">เวลาที่ใช้ (วินาที)</param>
    public void ShowVictory(float elapsedSeconds)
    {
        if (showDelay > 0f)
            Invoke(nameof(DoShowVictory), showDelay);
        else
            DoShowVictory();

        pendingElapsedSeconds = elapsedSeconds;
    }

    private float pendingElapsedSeconds;

    private void DoShowVictory()
    {
        if (victoryPanel != null)
            victoryPanel.SetActive(true);

        if (scoreTimeText != null)
            scoreTimeText.text = FormatTime(pendingElapsedSeconds);

        Debug.Log($"[Victory] Score Time: {FormatTime(pendingElapsedSeconds)}");
    }

    /// <summary>
    /// เรียกจากปุ่ม "Play Again"
    /// ผูกใน Button's OnClick() ใน Inspector
    /// </summary>
    public void PlayAgain()
    {
        string sceneName = string.IsNullOrEmpty(playAgainSceneName)
            ? SceneManager.GetActiveScene().name
            : playAgainSceneName;

        SceneManager.LoadScene(sceneName);
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
