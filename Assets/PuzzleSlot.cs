using UnityEngine;

/// <summary>
/// PuzzleSlot
/// ติดกับ GameObject ของ slot แต่ละช่อง
/// ตรวจสอบว่าชิ้น puzzle ที่วางมาถูกต้องหรือไม่
///
/// วิธีใช้:
/// 1. สร้าง UI Image/Panel สำหรับ slot แต่ละช่อง
/// 2. ติด Script นี้กับ slot GameObject
/// 3. กำหนด correctPieceIndex ให้ตรงกับ pieceIndex ของชิ้นที่ควรมาอยู่ใน slot นี้
/// </summary>
public class PuzzleSlot : MonoBehaviour
{
    [Header("Slot Settings")]
    [Tooltip("pieceIndex ของชิ้น puzzle ที่ถูกต้องสำหรับ slot นี้")]
    public int correctPieceIndex;

    public bool IsOccupied { get; private set; } = false;

    /// <summary>
    /// รับชิ้น puzzle เข้ามาในช่อง
    /// </summary>
    /// <returns>true ถ้า index ถูกต้อง</returns>
    public bool AcceptPiece(DraggablePiece piece)
    {
        if (IsOccupied) return false;

        if (piece.pieceIndex == correctPieceIndex)
        {
            IsOccupied = true;
            // แจ้ง Manager ว่า slot นี้แก้แล้ว
            JigsawPuzzleManager.Instance?.OnPieceSolved(correctPieceIndex);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Reset slot (เรียกจาก JigsawPuzzleManager.ResetPuzzle)
    /// </summary>
    public void ResetSlot()
    {
        IsOccupied = false;
    }
}
