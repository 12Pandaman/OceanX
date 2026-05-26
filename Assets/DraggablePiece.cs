using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// DraggablePiece
/// ติดกับ GameObject ของชิ้น puzzle แต่ละชิ้น
/// จัดการ drag & drop ผ่าน Unity EventSystem
///
/// วิธีใช้:
/// 1. ติด Script นี้กับ GameObject ชิ้น puzzle (แต่ละชิ้น)
/// 2. กำหนด pieceIndex ให้ตรงกับหมายเลขชิ้น (0-based, ดังนั้น ชิ้น "1" = index 0)
/// 3. ต้องมี Canvas ที่มี GraphicRaycaster และ EventSystem ใน Scene
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class DraggablePiece : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Piece Settings")]
    [Tooltip("หมายเลข index ของชิ้นนี้ (0-based): ชิ้นเลข 1 = 0, ชิ้นเลข 2 = 1 ฯลฯ")]
    public int pieceIndex;

    [Header("Snap Settings")]
    [Tooltip("ระยะ snap เมื่อวางใกล้ slot (units ใน Canvas)")]
    public float snapDistance = 60f;

    // ตำแหน่งเดิมก่อน drag (เพื่อ reset กรณีวางผิด)
    private Vector3 originalPosition;
    private Transform originalParent;
    private CanvasGroup canvasGroup;
    private Canvas rootCanvas;

    // สถานะว่าชิ้นนี้วางถูกต้องแล้วหรือยัง
    private bool isSolved = false;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rootCanvas = GetComponentInParent<Canvas>();
        originalPosition = transform.position;
        originalParent = transform.parent;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isSolved) return; // ถ้าวางถูกแล้วให้ lock ไม่ให้ขยับ

        originalPosition = transform.position;

        // ทำให้ raycast ผ่านชิ้นนี้ได้ตอน drag (ไม่งั้น slot ข้างล่างไม่ detect)
        canvasGroup.blocksRaycasts = false;

        // ยก layer ขึ้นมาบนสุด
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isSolved) return;

        // ขยับตาม pointer
        if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            transform.position = eventData.position;
        }
        else
        {
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                rootCanvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector3 worldPos);
            transform.position = worldPos;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isSolved) return;

        canvasGroup.blocksRaycasts = true;

        // ค้นหา slot ที่ใกล้ที่สุด
        PuzzleSlot nearestSlot = FindNearestSlot();

        if (nearestSlot != null && nearestSlot.AcceptPiece(this))
        {
            // snap เข้า slot
            transform.SetParent(nearestSlot.transform);
            transform.localPosition = Vector3.zero;
            isSolved = true;
            canvasGroup.blocksRaycasts = false; // ล็อค interaction
        }
        else
        {
            // คืนตำแหน่งเดิม
            transform.position = originalPosition;
        }
    }

    /// <summary>
    /// หา PuzzleSlot ที่อยู่ใกล้ที่สุดและยังว่างอยู่
    /// </summary>
    private PuzzleSlot FindNearestSlot()
    {
        PuzzleSlot[] allSlots = FindObjectsByType<PuzzleSlot>(FindObjectsSortMode.None);
        PuzzleSlot nearest = null;
        float minDist = snapDistance;

        foreach (var slot in allSlots)
        {
            if (slot.IsOccupied) continue;

            float dist = Vector3.Distance(transform.position, slot.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = slot;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Reset ชิ้นกลับไปตำแหน่งเริ่มต้น (เรียกจาก JigsawPuzzleManager.ResetPuzzle)
    /// </summary>
    public void ResetPiece()
    {
        isSolved = false;
        canvasGroup.blocksRaycasts = true;
        transform.SetParent(originalParent);
        transform.position = originalPosition;
    }
}
