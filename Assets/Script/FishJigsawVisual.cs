using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ใส่รูปปลาลงบน Piece1-9 ที่มีอยู่แล้วใน JigsawPanel
/// ใช้ Image + Sprite.Create แทน RawImage เพื่อความเสถียรกับ Unity UI Canvas
///
/// วิธีใช้:
///   1. Attach script นี้บน JigsawPanel
///   2. ลาก Piece1-9 ใส่ช่อง pieces[] ใน Inspector
///   3. ลากรูปปลา 3 รูป ใส่ช่อง fishTextures[] ใน Inspector
///   4. เรียก ShowRandomFish() หรือ ShowFish(index) จาก FishMinigameInteractable
/// </summary>
public class FishJigsawVisual : MonoBehaviour
{
    [Header("Piece ทั้ง 9 — ลาก Piece1 ถึง Piece9 ตามลำดับ")]
    public RectTransform[] pieces;

    [Header("รูปปลา 3 ตัว — index ต้องตรงกับ FishMinigameInteractable")]
    [Tooltip("[0]=ปลาตัวที่ 1, [1]=ปลาตัวที่ 2, [2]=ปลาตัวที่ 3")]
    public Texture2D[] fishTextures;

    [Header("Grid (ต้องตรงกับ Slot layout)")]
    public int rows = 3;
    public int cols = 3;

    [Header("Options")]
    [Tooltip("ซ่อนตัวเลข Text(TMP) บน piece เมื่อแสดงรูปปลา")]
    public bool hideNumberText = true;

    [Header("Shuffle")]
    [Tooltip("สับสลับ UV slice ของชิ้นจิ๊กซอ — ปิดไว้เพราะจะทำให้ targetSlot ผิด\n" +
             "การสุ่มรูปปลา (ShowRandomFish) ทำงานอยู่แล้วโดยไม่ต้องเปิด option นี้")]
    public bool shufflePieces = false;   // ปิดไว้ — ใช้ ShowRandomFish() แทน

    [Range(1, 5)]
    public int shufflePasses = 2;

    // ──────────────────────────────────────────────────────────────────────
    // Internal State
    // ──────────────────────────────────────────────────────────────────────

    // shuffledUVOrder[i] = slot UV ที่ piece[i] ได้รับ (ใช้ตรวจว่าถูกที่หรือยัง)
    private int[] _shuffledUVOrder;

    // เก็บ Sprite ที่สร้างขึ้น Runtime เพื่อ Destroy ตอนปิด (ป้องกัน memory leak)
    private readonly List<Sprite> _runtimeSprites = new List<Sprite>();

    // ──────────────────────────────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>สุ่มรูปปลาแล้วแสดงบน piece ทั้ง 9 — เรียกจาก FishMinigameInteractable</summary>
    public void ShowRandomFish()
    {
        if (fishTextures == null || fishTextures.Length == 0)
        {
            Debug.LogWarning("[FishJigsawVisual] ไม่มี fishTextures! ใส่รูปปลาให้ครบก่อน");
            return;
        }
        int randomIndex = Random.Range(0, fishTextures.Length);
        Debug.Log($"[FishJigsawVisual] สุ่มได้ปลา index={randomIndex} ('{fishTextures[randomIndex]?.name}')");
        ShowFish(randomIndex);
    }

    /// <summary>แสดงรูปปลา fishIndex บน piece ทั้ง 9 อัน (พร้อม shuffle ถ้าเปิดไว้)</summary>
    public void ShowFish(int fishIndex)
    {
        if (fishTextures == null || fishIndex < 0 || fishIndex >= fishTextures.Length)
        {
            Debug.LogWarning($"[FishJigsawVisual] fishIndex={fishIndex} ไม่มีใน fishTextures!");
            return;
        }

        Texture2D tex = fishTextures[fishIndex];
        if (tex == null)
        {
            Debug.LogWarning($"[FishJigsawVisual] fishTextures[{fishIndex}] เป็น null!");
            return;
        }

        // Cleanup Sprite เก่าก่อน
        CleanupSprites();

        // สร้าง UV order (สับหรือเรียงตามปกติ)
        _shuffledUVOrder = BuildUVOrder(pieces.Length);

        // Apply ทุก piece
        for (int i = 0; i < pieces.Length; i++)
            ApplyPiece(i, tex);

        Debug.Log($"[FishJigsawVisual] แสดงปลา '{tex.name}' บน {pieces.Length} ชิ้น" +
                  (shufflePieces ? " [SHUFFLED]" : "") + " ✓");
    }

    /// <summary>ซ่อนรูปปลาทั้งหมด + คืน text number — เรียกจาก CloseMinigame()</summary>
    public void HideFishImages()
    {
        foreach (var piece in pieces)
        {
            if (piece == null) continue;

            Transform fishTf = piece.Find("FishImage");
            if (fishTf != null)
            {
                Image img = fishTf.GetComponent<Image>();
                if (img != null) img.enabled = false;
            }

            SetNumberTextVisible(piece, true);
        }

        CleanupSprites();
    }

    // ──────────────────────────────────────────────────────────────────────
    // Puzzle Solve Check (สำหรับระบบ drag-and-drop ภายนอก)
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>คืน true เมื่อ piece[i] อยู่ถูกที่</summary>
    public bool IsPieceCorrect(int pieceIndex)
    {
        if (_shuffledUVOrder == null || pieceIndex < 0 || pieceIndex >= _shuffledUVOrder.Length)
            return false;
        return _shuffledUVOrder[pieceIndex] == pieceIndex;
    }

    /// <summary>คืน true เมื่อทุกชิ้นอยู่ถูกที่ (ปริศนาสำเร็จ!)</summary>
    public bool IsSolved()
    {
        if (_shuffledUVOrder == null) return false;
        for (int i = 0; i < _shuffledUVOrder.Length; i++)
            if (_shuffledUVOrder[i] != i) return false;
        return true;
    }

    /// <summary>Swap UV ระหว่าง piece[a] กับ piece[b] แล้ว refresh รูป</summary>
    public void SwapPieces(int a, int b, Texture2D tex)
    {
        if (_shuffledUVOrder == null) return;
        if (a < 0 || a >= pieces.Length || b < 0 || b >= pieces.Length) return;
        (_shuffledUVOrder[a], _shuffledUVOrder[b]) = (_shuffledUVOrder[b], _shuffledUVOrder[a]);
        ApplyPiece(a, tex);
        ApplyPiece(b, tex);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Shuffle Logic
    // ──────────────────────────────────────────────────────────────────────

    private int[] BuildUVOrder(int count)
    {
        int[] order = new int[count];
        for (int i = 0; i < count; i++) order[i] = i;

        if (!shufflePieces) return order;

        for (int pass = 0; pass < shufflePasses; pass++)
            for (int i = count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

        // Safety: ถ้าสับได้เหมือนเดิมทุกตัว ให้สลับ 2 ชิ้นแรก
        bool allSame = true;
        for (int i = 0; i < count; i++)
            if (order[i] != i) { allSame = false; break; }
        if (allSame && count >= 2)
            (order[0], order[1]) = (order[1], order[0]);

        return order;
    }

    // ──────────────────────────────────────────────────────────────────────
    // Apply Piece
    // ──────────────────────────────────────────────────────────────────────

    private void ApplyPiece(int index, Texture2D tex)
    {
        if (index < 0 || index >= pieces.Length || pieces[index] == null) return;

        int uvSlot = _shuffledUVOrder[index];
        int row    = uvSlot / cols;
        int col    = uvSlot % cols;

        // คำนวณ Rect เป็น pixel สำหรับ Sprite.Create
        // Unity texture Y=0 อยู่ล่าง → ต้อง flip row
        float pieceW  = tex.width  / (float)cols;
        float pieceH  = tex.height / (float)rows;
        float pixelX  = col * pieceW;
        float pixelY  = (rows - 1 - row) * pieceH;   // flip Y

        Rect   spriteRect = new Rect(pixelX, pixelY, pieceW, pieceH);
        Sprite sprite     = Sprite.Create(tex, spriteRect, new Vector2(0.5f, 0.5f),
                                          100f, 0, SpriteMeshType.FullRect);
        _runtimeSprites.Add(sprite);

        Image img = GetOrCreateFishImage(pieces[index]);
        img.sprite          = sprite;
        img.color           = Color.white;
        img.enabled         = true;
        img.type            = Image.Type.Simple;
        img.preserveAspect  = false;

        SetNumberTextVisible(pieces[index], !hideNumberText);

        Debug.Log($"[FishJigsawVisual] Piece[{index}] ← slot {uvSlot} " +
                  $"rect({pixelX:F0},{pixelY:F0},{pieceW:F0},{pieceH:F0}) tex={tex.name}");
    }

    // ──────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>หาหรือสร้าง child Image ชื่อ "FishImage" บน piece</summary>
    private Image GetOrCreateFishImage(RectTransform piece)
    {
        Transform existing = piece.Find("FishImage");
        if (existing != null)
        {
            Image existingImg = existing.GetComponent<Image>();
            if (existingImg != null) return existingImg;
        }

        GameObject go = new GameObject("FishImage");
        go.transform.SetParent(piece, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // ไม่ set sibling index → FishImage อยู่บนสุด ทับ background ของ Piece

        Image img = go.AddComponent<Image>();
        img.raycastTarget = false; // ไม่รับ click เพื่อไม่รบกวน Piece เดิม
        return img;
    }

    private void SetNumberTextVisible(RectTransform piece, bool visible)
    {
        var tmp = piece.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        if (tmp != null) tmp.gameObject.SetActive(visible);
    }

    private void CleanupSprites()
    {
        foreach (var s in _runtimeSprites)
            if (s != null) Destroy(s);
        _runtimeSprites.Clear();
    }
}
