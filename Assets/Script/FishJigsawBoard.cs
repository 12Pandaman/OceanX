using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// จัดการจิกซอปลา 1 ตัวต่อ 1 object
/// สร้าง GameObject 3 อัน แต่ละอันใส่ script นี้ แล้วกำหนด fishTexture คนละรูป
///
/// Hierarchy ที่แนะนำ:
///   FishPuzzle_Tuna          (FishJigsawBoard)
///     ├── SlotsContainer     (Panel - ช่องวางของจิกซอ)
///     └── PiecesContainer    (Panel - ที่กระจายชิ้นส่วน)
/// </summary>
public class FishJigsawBoard : MonoBehaviour
{
    // ──────────────────────────────────────────
    // Inspector Fields
    // ──────────────────────────────────────────

    [Header("Fish Images")]
    [Tooltip("ใส่รูปปลาหลายๆ รูปตรงนี้ ระบบจะสุ่มเลือกมา 1 รูปตอนสร้างจิ๊กซอว์")]
    public Texture2D[] fishTextures;

    [Header("ขนาด Grid")]
    public int rows = 3;
    public int cols = 3;
    public Vector2 pieceSize = new Vector2(120f, 120f);

    [Header("UI Containers — ลาก Assign ใน Inspector")]
    [Tooltip("Panel ที่ใช้เป็นช่องรับจิกซอ (slots)")]
    public RectTransform slotsContainer;

    [Tooltip("Panel ที่ใช้กระจายชิ้นจิกซอก่อนเล่น")]
    public RectTransform piecesContainer;

    [Header("การกระจายชิ้น")]
    [Tooltip("รัศมีการสุ่มตำแหน่งของชิ้นส่วน (pixel)")]
    public float scatterRadius = 200f;

    [Header("Debug")]
    public bool autoGenerateOnStart = true;

    // ──────────────────────────────────────────
    // Public — ใช้โดย JigsawPiece
    // ──────────────────────────────────────────

    /// <summary>ช่องทั้งหมดบน grid (ใช้โดย JigsawPiece)</summary>
    [HideInInspector] public RectTransform[] allJigsawSlots;

    /// <summary>ชิ้นจิกซอทั้งหมด (ใช้โดย JigsawPiece)</summary>
    [HideInInspector] public JigsawPiece[] jigsawPieces;

    // ──────────────────────────────────────────
    // Private
    // ──────────────────────────────────────────

    private bool puzzleComplete = false;

    // ──────────────────────────────────────────
    // Unity Lifecycle
    // ──────────────────────────────────────────

    void Start()
    {
        if (autoGenerateOnStart)
            GeneratePuzzle();
    }

    // ──────────────────────────────────────────
    // Generate
    // ──────────────────────────────────────────

    /// <summary>
    /// สร้าง Slot และ Piece ทั้งหมดบน board นี้
    /// เรียกได้จาก Inspector (public) เช่น ตอน Reset puzzle
    /// </summary>
    public void GeneratePuzzle()
    {
        if (fishTextures == null || fishTextures.Length == 0)
        {
            Debug.LogWarning($"[FishJigsawBoard] '{name}' : ยังไม่ได้ใส่ fishTextures!");
            return;
        }

        if (slotsContainer == null || piecesContainer == null)
        {
            Debug.LogError($"[FishJigsawBoard] '{name}' : กรุณา Assign slotsContainer และ piecesContainer ใน Inspector");
            return;
        }

        // ล้างของเก่า
        ClearChildren(slotsContainer);
        ClearChildren(piecesContainer);

        int total = rows * cols;
        allJigsawSlots = new RectTransform[total];
        jigsawPieces   = new JigsawPiece[total];
        puzzleComplete = false;
        
        // สุ่มเลือกรูปปลาจาก Array
        Texture2D selectedTexture = fishTextures[Random.Range(0, fishTextures.Length)];

        // คำนวณ offset เพื่อ center grid
        Vector2 gridOffset = new Vector2(
            -(cols - 1) * pieceSize.x * 0.5f,
             (rows - 1) * pieceSize.y * 0.5f
        );

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int index = r * cols + c;

                // ─── สร้าง Slot ───────────────────────────────
                GameObject slotGO = new GameObject($"Slot_{r}_{c}");
                slotGO.transform.SetParent(slotsContainer, false);

                RectTransform slotRect = slotGO.AddComponent<RectTransform>();
                slotRect.sizeDelta = pieceSize;
                slotRect.anchoredPosition = gridOffset + new Vector2(c * pieceSize.x, -r * pieceSize.y);

                // พื้นหลัง slot (สีเข้ม semi-transparent)
                Image slotBg = slotGO.AddComponent<Image>();
                slotBg.color = new Color(0.08f, 0.08f, 0.15f, 0.65f);

                // เส้นขอบ slot ด้วย Outline component
                Outline slotOutline = slotGO.AddComponent<Outline>();
                slotOutline.effectColor = new Color(0.4f, 0.8f, 1f, 0.6f);
                slotOutline.effectDistance = new Vector2(2f, -2f);

                allJigsawSlots[index] = slotRect;

                // ─── สร้าง Piece ──────────────────────────────
                GameObject pieceGO = new GameObject($"FishPiece_{r}_{c}");
                pieceGO.transform.SetParent(piecesContainer, false);

                RectTransform pieceRect = pieceGO.AddComponent<RectTransform>();
                pieceRect.sizeDelta = pieceSize;

                // RawImage แสดงส่วนของรูปปลาที่ถูกต้อง
                RawImage rawImg = pieceGO.AddComponent<RawImage>();
                rawImg.texture = selectedTexture;

                // คำนวณ UV rect — แต่ละชิ้นเห็นเฉพาะส่วนของตัวเอง
                // UV origin อยู่ที่ bottom-left  →  ต้อง flip แกน Y
                float uvX = (float)c / cols;
                float uvY = (float)(rows - 1 - r) / rows;
                rawImg.uvRect = new Rect(uvX, uvY, 1f / cols, 1f / rows);

                // เส้นขอบชิ้นจิกซอ
                Outline pieceOutline = pieceGO.AddComponent<Outline>();
                pieceOutline.effectColor = new Color(1f, 1f, 1f, 0.8f);
                pieceOutline.effectDistance = new Vector2(1.5f, -1.5f);

                // CanvasGroup — จำเป็นสำหรับ drag & drop ของ JigsawPiece
                pieceGO.AddComponent<CanvasGroup>();

                // JigsawPiece component
                JigsawPiece piece = pieceGO.AddComponent<JigsawPiece>();
                piece.targetSlot   = slotRect;
                piece.snapDistance = Mathf.Min(pieceSize.x, pieceSize.y) * 0.5f;

                // กระจายชิ้นส่วน
                float px = Random.Range(-scatterRadius, scatterRadius);
                float py = Random.Range(-scatterRadius * 0.5f, scatterRadius * 0.5f);
                pieceRect.anchoredPosition = new Vector2(px, py);

                jigsawPieces[index] = piece;
            }
        }

        Debug.Log($"[FishJigsawBoard] '{name}' สร้างจิกซอ {rows}×{cols} = {total} ชิ้น เสร็จแล้ว ✓");
    }

    // ──────────────────────────────────────────
    // Completion Check  (เรียกจาก JigsawPiece.OnEndDrag)
    // ──────────────────────────────────────────

    /// <summary>
    /// ตรวจว่าจิกซอทุกชิ้นอยู่บน slot ที่ถูกต้องหรือยัง
    /// ชิ้นที่ i ต้องอยู่บน allJigsawSlots[i] ถึงจะนับว่าถูก
    /// </summary>
    public void CheckJigsawCompletion()
    {
        if (puzzleComplete) return;

        int correct = 0;
        for (int i = 0; i < jigsawPieces.Length; i++)
        {
            if (jigsawPieces[i] == null || allJigsawSlots[i] == null) continue;

            RectTransform pr = jigsawPieces[i].GetComponent<RectTransform>();
            if (pr == null) continue;

            // ชิ้นนั้นต้องอยู่ใกล้ slot ของตัวเองภายใน 10px
            if (Vector2.Distance(pr.anchoredPosition, allJigsawSlots[i].anchoredPosition) < 10f)
                correct++;
        }

        if (correct >= jigsawPieces.Length)
        {
            puzzleComplete = true;
            OnPuzzleComplete();
        }
    }

    // ──────────────────────────────────────────
    // Events
    // ──────────────────────────────────────────

    void OnPuzzleComplete()
    {
        Debug.Log($"[FishJigsawBoard] 🐟 '{gameObject.name}' จิกซอสมบูรณ์!");
        // TODO: เชื่อมกับ reward system / UI ของเกม เช่น:
        // FishMinigameManager.Instance?.OnBoardComplete(this);
    }

    // ──────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────

    void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    /// <summary>Reset จิกซอ board นี้กลับไปเป็นสถานะเริ่มต้น</summary>
    public void ResetBoard()
    {
        GeneratePuzzle();
    }
}
