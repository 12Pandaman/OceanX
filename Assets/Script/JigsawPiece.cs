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

        FishJigsawBoard board = GetComponentInParent<FishJigsawBoard>();
        FishMinigameManager manager = GetComponentInParent<FishMinigameManager>();

        RectTransform[] allSlots = null;
        if (board != null) allSlots = board.allJigsawSlots;
        else if (manager != null) allSlots = manager.allJigsawSlots;

        // ─── หาช่องที่ใกล้ที่สุด เพื่อให้วางตรงไหนก็ได้ ─────────────────────────
        if (allSlots != null && allSlots.Length > 0)
        {
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

            if (closestSlot != null && minDistance <= snapDistance)
                rectTransform.anchoredPosition = closestSlot.anchoredPosition;
            else
                rectTransform.anchoredPosition = currentStartPosition;
        }
        else
        {
            // Fallback (ถ้าไม่ได้รวม array slots ไว้)
            if (targetSlot != null && Vector2.Distance(rectTransform.anchoredPosition, targetSlot.anchoredPosition) <= snapDistance)
                rectTransform.anchoredPosition = targetSlot.anchoredPosition;
            else
                rectTransform.anchoredPosition = currentStartPosition;
        }

        // ─── แจ้งตรวจสอบว่า puzzle สำเร็จหรือยัง ─────────────────────
        if (board != null) board.CheckJigsawCompletion();
        if (manager != null) manager.CheckJigsawCompletion();
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
