using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// JigsawPuzzleManager
/// จัดการ logic หลักของ Jigsaw Puzzle:
/// - ลงทะเบียน slot และชิ้น puzzle
/// - ตรวจสอบว่า puzzle เสร็จครบแล้วหรือยัง
/// - แจ้ง VictoryScreenManager เมื่อ puzzle เสร็จ
///
/// วิธีใช้:
/// 1. สร้าง Empty GameObject ชื่อ "JigsawManager" ใน Scene
/// 2. ลาก Script นี้ใส่
/// 3. กำหนด totalPieces ให้ตรงกับจำนวนชิ้น (เช่น 9)
/// 4. อ้างอิง VictoryScreenManager
/// </summary>
public class JigsawPuzzleManager : MonoBehaviour
{
    public static JigsawPuzzleManager Instance { get; private set; }

    [Header("Puzzle Settings")]
    [Tooltip("จำนวนชิ้น puzzle ทั้งหมด")]
    public int totalPieces = 9;

    [Header("References")]
    public VictoryScreenManager victoryScreen;
    public TimerManager timerManager;

    // ติดตามว่า slot ไหนถูกวางถูกต้องแล้วบ้าง
    private HashSet<int> solvedSlots = new HashSet<int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        solvedSlots.Clear();
        // เริ่มนับเวลา
        if (timerManager != null)
            timerManager.StartTimer();
    }

    /// <summary>
    /// เรียกจาก PuzzleSlot เมื่อชิ้น puzzle ถูกวางถูกต้อง
    /// </summary>
    /// <param name="slotIndex">หมายเลข slot ที่แก้ได้แล้ว (0-based)</param>
    public void OnPieceSolved(int slotIndex)
    {
        if (solvedSlots.Contains(slotIndex)) return;

        solvedSlots.Add(slotIndex);
        Debug.Log($"[Jigsaw] Slot {slotIndex + 1} solved! ({solvedSlots.Count}/{totalPieces})");

        // ตรวจสอบว่า puzzle เสร็จทั้งหมดหรือยัง
        if (solvedSlots.Count >= totalPieces)
        {
            OnPuzzleComplete();
        }
    }

    /// <summary>
    /// เรียกเมื่อ puzzle เสร็จสมบูรณ์
    /// </summary>
    private void OnPuzzleComplete()
    {
        Debug.Log("[Jigsaw] Puzzle Complete!");

        // หยุดจับเวลา
        if (timerManager != null)
            timerManager.StopTimer();

        // แสดงหน้า Victory
        if (victoryScreen != null)
            victoryScreen.ShowVictory(timerManager != null ? timerManager.GetElapsedTime() : 0f);
    }

    /// <summary>
    /// Reset puzzle ใหม่ทั้งหมด (ใช้กับ Play Again ภายใน Scene เดียวกัน)
    /// </summary>
    public void ResetPuzzle()
    {
        solvedSlots.Clear();
        if (timerManager != null)
            timerManager.ResetTimer();
    }
}
