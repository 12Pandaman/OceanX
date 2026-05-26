using UnityEngine;
using UnityEngine.UI;

public class FishCollectionManager : MonoBehaviour
{
    [Header("Fish Icon UI (3 อัน — ลาก Image ใส่ตามลำดับ)")]
    public Image[] fishIcons;

    [Header("สี Icon")]
    public Color lockedColor   = new Color(0.25f, 0.50f, 0.85f, 1f);
    public Color unlockedColor = Color.white;

    [Header("Press E Hint (optional)")]
    public GameObject pressEHint;

    [Header("Quiz ท้ายสุด (หลังครบ 3 ตัว)")]
    [Tooltip("สุ่มรูปปลาตอนเริ่มจิ๊กซอว์ (ถ้าเปิด จะใช้คำตอบที่ถูกต้องตาม index ที่สุ่มได้)")]
    public bool     randomizeFinalFish   = true;
    public string   finalQuestion        = "Which fish did you find?";
    public string[] finalChoices         = { "Tuna", "Blue Tang", "Cichlid" };
    public int      finalCorrectIndex    = 0;
    public int      finalJigsawFishIndex = 0;

    // ── private ───────────────────────────────────────────────────────────
    private FishMinigameManager _manager;
    private FishJigsawVisual    _visual;
    public FishMinigameManager MinigameManager => _manager;

    private bool[] collected     = new bool[3]; // ติดตามว่าปลา index ไหนถูกเก็บแล้ว
    private int    collectedCount = 0;           // นับลำดับ → ใช้ตั้งสี icon
    private bool   allCollected   = false;

    // ──────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _manager = GetComponentInChildren<FishMinigameManager>(true);
        _visual  = GetComponentInChildren<FishJigsawVisual>(true);

        // Ensure fishIcons array is not null, even if empty, to prevent NREs later
        if (fishIcons == null)
        {
            fishIcons = new Image[0]; // Initialize as an empty array if not assigned
            Debug.LogWarning("[FishCollectionManager] 'fishIcons' array was null and has been initialized as empty. Please assign Image elements in the Inspector.");
        }
        // ตั้ง icon ทุกอันเป็นสีน้ำเงินก่อน
        SetAllIconsLocked();
        if (pressEHint != null) pressEHint.SetActive(false);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Public API  (เรียกจาก FishMinigameInteractable)
    // ──────────────────────────────────────────────────────────────────────

    public void CollectFish(int fishIndex)
    {
        if (fishIndex < 0 || fishIndex >= 3) return;
        if (collected[fishIndex]) return;   // เก็บไปแล้ว

        // mark ปลาตัวนี้ว่าเก็บแล้ว
        collected[fishIndex] = true;

        // จุดสำคัญ: ไล่ icon จากซ้าย (0→1→2) ตามลำดับที่เก็บ ไม่ใช่ fish index
        if (fishIcons != null && collectedCount < fishIcons.Length)
        {
            if (fishIcons[collectedCount] != null)
            {
                fishIcons[collectedCount].color = unlockedColor;
                Debug.Log($"[FishCollection] Icon for collected fish #{fishIndex} (slot {collectedCount}) updated to unlocked color.");
            }
            else
            {
                Debug.LogWarning($"[FishCollection] fishIcons[{collectedCount}] is null! Cannot update icon for fish #{fishIndex}. Please check Inspector assignments.");
            }
        }
        else if (fishIcons == null)
        {
            Debug.LogWarning($"[FishCollection] fishIcons array is null! Cannot update icon for fish #{fishIndex}. Please check Inspector assignments.");
        }
        collectedCount++;
        Debug.Log($"[FishCollection] เก็บปลา #{fishIndex} ✓  รวม {collectedCount}/3");

        if (collectedCount >= 3)
        {
            allCollected = true;
            Debug.Log("[FishCollection] ปลาครบ 3 ตัวแล้ว! รอการยืนยันเพื่อเริ่มมินิเกมสุดท้าย...");
        }
    }

    public bool IsCollected(int fishIndex)
    {
        if (fishIndex < 0 || fishIndex >= 3) return false;
        return collected[fishIndex];
    }

    public bool AreAllFishCollected()
    {
        return allCollected;
    }

    public void ShowPressEHint(bool show)
    {
        if (pressEHint != null) pressEHint.SetActive(show);
    }

    public void ResetCollection()
    {
        collected      = new bool[3];
        collectedCount = 0;
        allCollected   = false;
        SetAllIconsLocked();
        if (pressEHint != null) pressEHint.SetActive(false);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Private
    // ──────────────────────────────────────────────────────────────────────

    public void StartFinalMinigame()
    {
        Debug.Log("[FishCollection] 🐟🐟🐟 ครบ 3 ตัว! เปิด Jigsaw + Quiz...");

        if (_manager == null) { Debug.LogError("[FishCollection] ไม่เจอ FishMinigameManager!"); return; }

        if (_manager.minigameCanvas != null)
            _manager.minigameCanvas.SetActive(true);

        int fishIndexToUse = finalJigsawFishIndex;
        int correctIndexToUse = finalCorrectIndex;

        // ถ้าเปิดตั้งค่าสุ่ม ให้สุ่มเลือกรูปปลาและอัปเดตคำตอบที่ถูกต้อง
        if (randomizeFinalFish && finalChoices.Length > 0)
        {
            UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);
            fishIndexToUse = UnityEngine.Random.Range(0, finalChoices.Length);
            correctIndexToUse = fishIndexToUse; // ให้คำตอบที่ถูกตรงกับ index ของปลา
            Debug.Log($"[FishCollection] เปิดระบบสุ่ม! สุ่มได้ปลา index ที่: {fishIndexToUse}");
        }
        else
        {
            Debug.Log($"[FishCollection] ระบบสุ่มปิดอยู่ (หรือ finalChoices ว่าง) ใช้รูป index: {fishIndexToUse}");
        }

        if (_visual != null)
            _visual.ShowFish(fishIndexToUse);

        _manager.StartMinigame(finalQuestion, finalChoices, correctIndexToUse, fishIndexToUse);
    }

    void SetAllIconsLocked()
    {
        if (fishIcons == null) return;
        foreach (var icon in fishIcons)
            if (icon != null) icon.color = lockedColor;
    }
}
