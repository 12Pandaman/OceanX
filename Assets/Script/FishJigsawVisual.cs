using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ใส่รูปปลาลงบน Piece1-9 ที่มีอยู่แล้วใน JigsawPanel ของ Player Prefab
///
/// วิธีใช้:
///   1. Attach script นี้บน JigsawPanel (หรือ Object เดียวกับ FishMinigameManager)
///   2. ลาก Piece1-9 ใส่ช่อง pieces[] ใน Inspector
///   3. ลากรูปปลา 3 รูป ใส่ช่อง fishTextures[] ใน Inspector
///   4. FishMinigameInteractable จะเรียก ShowFish(fishIndex) อัตโนมัติ
/// </summary>
public class FishJigsawVisual : MonoBehaviour
{
    [Header("Piece ทั้ง 9 — ลาก Piece1 ถึง Piece9 ตามลำดับ")]
    [Tooltip("ลาก Piece1, Piece2, ... Piece9 จาก JigsawPanel มาใส่ตามลำดับ")]
    public RectTransform[] pieces; // ขนาด 9

    [Header("รูปปลา 3 ตัว — index ต้องตรงกับ FishMinigameInteractable")]
    [Tooltip("[0]=ปลาตัวที่ 1, [1]=ปลาตัวที่ 2, [2]=ปลาตัวที่ 3")]
    public Texture2D[] fishTextures; // ขนาด 3

    [Header("Grid (ต้องตรงกับ Slot layout)")]
    public int rows = 3;
    public int cols = 3;

    [Header("Options")]
    [Tooltip("ซ่อนตัวเลข Text(TMP) บน piece เมื่อแสดงรูปปลา")]
    public bool hideNumberText = true;

    // ──────────────────────────────────────────────────────────────────────
    // Public API — เรียกจาก FishMinigameInteractable
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// แสดงรูปปลาบน piece ทั้ง 9 อัน
    /// fishIndex = 0/1/2 ตาม fishTextures[]
    /// </summary>
    public void ShowFish(int fishIndex)
    {
        if (fishTextures == null || fishIndex < 0 || fishIndex >= fishTextures.Length)
        {
            Debug.LogWarning($"[FishJigsawVisual] fishIndex={fishIndex} ไม่มีใน fishTextures! ใส่รูปปลาให้ครบก่อน");
            return;
        }

        Texture2D tex = fishTextures[fishIndex];
        if (tex == null)
        {
            Debug.LogWarning($"[FishJigsawVisual] fishTextures[{fishIndex}] เป็น null!");
            return;
        }

        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i] == null) continue;

            int r = i / cols;
            int c = i % cols;

            // หา child ชื่อ "FishImage" หรือสร้างใหม่
            RawImage rawImg = GetOrCreateFishImage(pieces[i]);

            // ใส่ texture และ UV slice ที่ถูกต้องสำหรับชิ้นนี้
            rawImg.texture  = tex;
            rawImg.enabled  = true;
            rawImg.uvRect   = new Rect(
                (float)c / cols,
                (float)(rows - 1 - r) / rows,   // UV Y = flip เพราะ origin อยู่ล่าง
                1f / cols,
                1f / rows
            );

            // ซ่อน/แสดง text number
            SetNumberTextVisible(pieces[i], !hideNumberText);
        }

        Debug.Log($"[FishJigsawVisual] แสดงปลา index={fishIndex} ('{tex.name}') บน {pieces.Length} ชิ้น ✓");
    }

    /// <summary>
    /// ซ่อนรูปปลาทั้งหมด + คืน text number
    /// เรียกจาก FishMinigameManager.CloseMinigame()
    /// </summary>
    public void HideFishImages()
    {
        foreach (var piece in pieces)
        {
            if (piece == null) continue;

            // ซ่อน FishImage child
            Transform fishImgTf = piece.Find("FishImage");
            if (fishImgTf != null)
            {
                RawImage ri = fishImgTf.GetComponent<RawImage>();
                if (ri != null) ri.enabled = false;
            }

            // คืน text number
            SetNumberTextVisible(piece, true);
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    // Private Helpers
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>หาหรือสร้าง child RawImage ชื่อ "FishImage" บน piece</summary>
    private RawImage GetOrCreateFishImage(RectTransform piece)
    {
        Transform existing = piece.Find("FishImage");
        if (existing != null)
            return existing.GetComponent<RawImage>();

        // สร้างใหม่
        GameObject go = new GameObject("FishImage");
        go.transform.SetParent(piece, false);

        // ยืด full piece
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;

        // วางไว้ที่ sibling index 0 (ด้านหลัง Text)
        go.transform.SetSiblingIndex(0);

        return go.AddComponent<RawImage>();
    }

    /// <summary>ซ่อน/แสดง TextMeshPro ที่อยู่บน piece</summary>
    private void SetNumberTextVisible(RectTransform piece, bool visible)
    {
        var tmp = piece.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        if (tmp != null) tmp.gameObject.SetActive(visible);
    }
}
