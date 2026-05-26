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

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        parentCanvas = GetComponentInParent<Canvas>();
    }

    private void Start()
    {
        if (rectTransform != null)
        {
            originalPosition = rectTransform.anchoredPosition;
            currentStartPosition = originalPosition;
        }

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
        transform.SetAsLastSibling();
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

        // ─── snap เฉพาะ targetSlot ของตัวเอง ────────────────────────────────────
        // ไม่ snap ไปช่องอื่น เพื่อให้ CheckJigsawCompletion ทำงานถูกต้อง
        if (targetSlot == null)
        {
            rectTransform.anchoredPosition = currentStartPosition;
        }
        else
        {
            float distance = Vector2.Distance(rectTransform.anchoredPosition, targetSlot.anchoredPosition);
            if (distance <= snapDistance)
                rectTransform.anchoredPosition = targetSlot.anchoredPosition;
            else
                rectTransform.anchoredPosition = currentStartPosition;
        }

        // ─── แจ้ง manager ให้ตรวจสอบว่า puzzle สำเร็จหรือยัง ─────────────────────
        FishJigsawBoard board = GetComponentInParent<FishJigsawBoard>();
        if (board != null)
        {
            board.CheckJigsawCompletion();
            return;
        }

        FishMinigameManager manager = GetComponentInParent<FishMinigameManager>();
        if (manager != null) manager.CheckJigsawCompletion();
    }

    /// <summary>หาช่องว่างที่ใกล้ที่สุดแล้ว snap ไป (ใช้กับทั้ง board และ manager)</summary>
    private void HandleSnapWithBoard(RectTransform[] slots, JigsawPiece[] pieces)
    {
        if (slots == null || slots.Length == 0) return;

        RectTransform closestSlot = null;
        float minDistance = float.MaxValue;

        foreach (var slot in slots)
        {
            if (slot == null) continue;

            // เช็คว่ามีชิ้นส่วนอื่นวางอยู่แล้วหรือเปล่า
            bool isOccupied = false;
            if (pieces != null)
            {
                foreach (var other in pieces)
                {
                    if (other == null || other == this) continue;
                    RectTransform otherRect = other.GetComponent<RectTransform>();
                    if (otherRect != null && Vector2.Distance(otherRect.anchoredPosition, slot.anchoredPosition) < 5f)
                    {
                        isOccupied = true;
                        break;
                    }
                }
            }
            if (isOccupied) continue;

            float dist = Vector2.Distance(rectTransform.anchoredPosition, slot.anchoredPosition);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestSlot = slot;
            }
        }

        if (closestSlot != null && minDistance <= snapDistance)
            rectTransform.anchoredPosition = closestSlot.anchoredPosition;
        else
            rectTransform.anchoredPosition = currentStartPosition;
    }

    public void ResetPiece()
    {
        isLocked = false;
        if (rectTransform != null) rectTransform.anchoredPosition = originalPosition;
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
    }
}
