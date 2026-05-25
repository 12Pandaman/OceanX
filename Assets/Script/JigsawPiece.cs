using UnityEngine;
using UnityEngine.EventSystems; // Required for Drag & Drop system

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
    
    public bool isLocked = false; // Tracks if the piece has been placed correctly.

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        // Automatically create a CanvasGroup if it doesn't exist (used for transparency and raycast blocking).
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        parentCanvas = GetComponentInParent<Canvas>();
    }

    private void Start()
    {
        // Store the starting position to return to if placed incorrectly.
        if (rectTransform != null)
        {
            originalPosition = rectTransform.anchoredPosition;
            currentStartPosition = originalPosition;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isLocked) return;
        
        canvasGroup.alpha = 0.8f; // Make slightly transparent while dragging.
        canvasGroup.blocksRaycasts = false; // Allow mouse raycasts to pass through to the target slot behind.
        transform.SetAsLastSibling(); // Bring this piece to the front to ensure it's not obscured by others.
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isLocked || parentCanvas == null || rectTransform == null) return;
        
        // Move the piece with the mouse.
        rectTransform.anchoredPosition += eventData.delta / parentCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isLocked || rectTransform == null) return;
        
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        if (targetSlot != null)
        {
            FishMinigameManager manager = GetComponentInParent<FishMinigameManager>();
            
            if (manager != null && manager.allJigsawSlots != null && manager.allJigsawSlots.Length > 0)
            {
                RectTransform closestSlot = null;
                float minDistance = float.MaxValue;

                // ค้นหาช่อง (Slot) ที่อยู่ใกล้กับชิ้นส่วนมากที่สุด และต้องไม่มีชิ้นอื่นวางอยู่
                foreach (var slot in manager.allJigsawSlots)
                {
                    if (slot == null) continue;
                    
                    // เช็คว่ามีชิ้นส่วนอื่นวางอยู่ตรงช่องนี้ไหม
                    bool isOccupied = false;
                    if (manager.jigsawPieces != null)
                    {
                        foreach (var otherPiece in manager.jigsawPieces)
                        {
                            if (otherPiece == null || otherPiece == this) continue;
                            
                            RectTransform otherRect = otherPiece.GetComponent<RectTransform>();
                            if (otherRect != null && Vector2.Distance(otherRect.anchoredPosition, slot.anchoredPosition) < 5f)
                            {
                                isOccupied = true;
                                break;
                            }
                        }
                    }
                    
                    if (isOccupied) continue; // ถ้ามีชิ้นอื่นวางอยู่ ให้ข้ามช่องนี้ไปเลย

                    float dist = Vector2.Distance(rectTransform.anchoredPosition, slot.anchoredPosition);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestSlot = slot;
                    }
                }

                // ถ้าอยู่ใกล้ช่องใดช่องหนึ่ง ให้ดูดติดช่องนั้น (ไม่ว่าจะถูกหรือผิด)
                if (closestSlot != null && minDistance <= snapDistance)
                {
                    rectTransform.anchoredPosition = closestSlot.anchoredPosition;
                }
                else
                {
                    rectTransform.anchoredPosition = currentStartPosition; // วางผิดที่ ให้เด้งกลับไปจุดที่สุ่มเกิด
                }
                
                manager.CheckJigsawCompletion();
            }
            else
            {
                // Fallback ถ้าไม่ได้ตั้งค่า allJigsawSlots
                float distance = Vector2.Distance(rectTransform.anchoredPosition, targetSlot.anchoredPosition);
                if (distance <= snapDistance) { rectTransform.anchoredPosition = targetSlot.anchoredPosition; manager?.CheckJigsawCompletion(); }
                else { rectTransform.anchoredPosition = currentStartPosition; }
            }
        }
    }

    // Function to reset the jigsaw piece to be playable again on the next interaction.
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

    // Function to scatter the piece randomly
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