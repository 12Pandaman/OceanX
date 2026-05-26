using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// ชิ้นจิกซอว์แบบ drag & drop
/// รองรับทั้ง FishJigsawBoard (ระบบปลา 3 อัน) และ FishMinigameManager (ระบบ quiz เดิม)
/// </summary>
public class JigsawPiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;
    private Vector2 currentStartPosition;
    private Canvas parentCanvas;

    [Header("Jigsaw Settings")]
    [Tooltip("The target slot (shadow) where this piece must be placed.")]
    public RectTransform targetSlot;

    [Tooltip("The distance to allow snapping to the target (larger values make it easier).")]
    public float snapDistance = 50f;

    public bool isLocked = false;
    public RectTransform CurrentSlot { get; private set; } = null; // เพิ่มเพื่อติดตามว่าชิ้นส่วนนี้อยู่บน slot ไหน

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        parentCanvas = GetComponentInParent<Canvas>();

        if (rectTransform != null)
        {
            originalPosition = rectTransform.anchoredPosition;
            currentStartPosition = originalPosition;
        }
    }

    private void Start()
    {
        // Auto-detect targetSlot จาก sibling ถ้ายังไม่ได้ set ใน Inspector
        // (ใน Hierarchy: Slot 1, Piece1, Slot 2, Piece2 ... → sibling บนเสมอคือ slot ของมัน)
        if (targetSlot == null)
        {
            int siblingIndex = transform.GetSiblingIndex();
            if (siblingIndex > 0)
            {
                Transform sibling = transform.parent.GetChild(siblingIndex - 1);
                RectTransform candidate = sibling.GetComponent<RectTransform>();
                if (candidate != null && sibling.GetComponent<JigsawPiece>() == null)
                    targetSlot = candidate;
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isLocked) return;
        canvasGroup.alpha = 0.8f;
        canvasGroup.blocksRaycasts = false;
        
        // เก็บตำแหน่งเริ่มต้นก่อนลาก
        transform.SetAsLastSibling();
        currentStartPosition = rectTransform.anchoredPosition;

        // แจ้ง Manager ว่าชิ้นส่วนนี้กำลังถูกยกออกจาก slot เดิม (ถ้ามี)
        FishMinigameManager manager = GetComponentInParent<FishMinigameManager>();
        if (manager != null && CurrentSlot != null) {
            manager.FreeSlot(CurrentSlot, this);
            CurrentSlot = null;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isLocked || parentCanvas == null || rectTransform == null) return;
        rectTransform.anchoredPosition += eventData.delta / parentCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isLocked || rectTransform == null) return;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // ตรวจสอบว่าชิ้นส่วนนี้อยู่ภายใต้ FishJigsawBoard หรือ FishMinigameManager
        FishJigsawBoard board = GetComponentInParent<FishJigsawBoard>();
        FishMinigameManager manager = GetComponentInParent<FishMinigameManager>();

        // Get all available slots from the current context (manager or board)
        RectTransform[] allSlots = null;
        if (manager != null)
        {
            allSlots = manager.allJigsawSlots;
        }
        else if (board != null)
        {
            allSlots = board.allJigsawSlots;
        }

        bool placed = false;

        if (allSlots != null && allSlots.Length > 0)
        {
            // Find the closest slot
            RectTransform closestSlot = null;
            float minDistance = float.MaxValue;

            foreach (var slot in allSlots)
            {
                if (slot == null) continue;
                float dist = Vector2.Distance(rectTransform.anchoredPosition, slot.anchoredPosition);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestSlot = slot;
                }
            }

            // Check if the closest slot is within snap distance
            if (closestSlot != null && minDistance <= snapDistance)
            {
                // If a manager exists, use it to check if the slot is already occupied.
                if (manager != null)
                {
                    if (manager.TryOccupySlot(closestSlot, this))
                    {
                        // Successfully occupied the correct slot.
                        rectTransform.anchoredPosition = closestSlot.anchoredPosition;
                        CurrentSlot = closestSlot;
                        placed = true;
                    }
                    // If TryOccupySlot returns false, 'placed' remains false, and the piece will snap back.
                }
                else if (board != null)
                {
                    // Fallback to old system (FishJigsawBoard), which has no overlap check.
                    rectTransform.anchoredPosition = closestSlot.anchoredPosition;
                    CurrentSlot = closestSlot;
                    placed = true;
                }
                // If neither a manager nor a board is found, 'placed' will remain false, and the piece will snap back.
            }
        }

        // If not placed (too far, or slot was occupied), snap back to where it was dragged from.
        if (!placed)
        {
            rectTransform.anchoredPosition = currentStartPosition;
            CurrentSlot = null; // Ensure it's not considered to be in a slot
        }

        // ─── แจ้งตรวจสอบว่า puzzle สำเร็จหรือยัง ─────────────────────
        if (board != null) board.CheckJigsawCompletion();
        if (manager != null) manager.CheckJigsawCompletion();
    }

    public void ResetPiece()
    {
        isLocked = false;
        if (rectTransform != null) rectTransform.anchoredPosition = originalPosition;
        CurrentSlot = null; // รีเซ็ต slot ที่ครอบครอง
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }
    }

    public void Scatter(float radius)
    {
        if (rectTransform != null)
        {
            Vector2 randomOffset = new Vector2(Random.Range(-radius, radius), Random.Range(-radius, radius));
            currentStartPosition = originalPosition + randomOffset;
            rectTransform.anchoredPosition = currentStartPosition;
        }
        CurrentSlot = null; // รีเซ็ต slot ที่ครอบครอง
    }
}
