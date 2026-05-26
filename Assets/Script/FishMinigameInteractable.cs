using UnityEngine;
using Unity.Netcode;

/// <summary>
/// แปะบน Fish Object แต่ละตัวในโลก (3 ตัว)
///
/// ระบบ:
///   - Player เดินเข้าใกล้ → แสดง "Press E"
///   - กด E → เรียก FishCollectionManager.CollectFish(fishIndex)
///   - เก็บได้แค่ครั้งเดียว → ปลาจะ "ติด" ไว้ใน slot ของ player นั้น
///   - Multiplayer safe: เช็ค NetworkObject.IsOwner ก่อนทำทุกอย่าง
///
/// หมายเหตุ: ถ้าไม่ใช้ Netcode ให้ลบ "using Unity.Netcode" ออก
///           และลบบรรทัด IsOwner check ออก script จะยังทำงานได้ปกติ
/// </summary>
public class FishMinigameInteractable : MonoBehaviour
{
    // ──────────────────────────────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────────────────────────────

    [Header("Fish Identity")]
    [Tooltip("0 = ปลาตัวที่ 1 / 1 = ปลาตัวที่ 2 / 2 = ปลาตัวที่ 3\n" +
             "ต้องตรงกับ index ใน FishJigsawVisual.fishTextures[]")]
    public int fishIndex = 0;

    [Header("Press E Prompt (world-space, optional)")]
    [Tooltip("GameObject ที่มีข้อความ 'Press E' ลอยอยู่เหนือปลา — ถ้าไม่มีก็ไม่ต้องใส่")]
    public GameObject worldPressEPrompt;

    [Header("Visual — เมื่อเก็บแล้ว")]
    [Tooltip("ถ้าต้องการให้รูปปลาหรือ effect เปลี่ยนตอนถูกเก็บ")]
    public GameObject collectedEffect; // เช่น particle หรือ outline glow

    // ──────────────────────────────────────────────────────────────────────
    // Private State
    // ──────────────────────────────────────────────────────────────────────

    // player ที่อยู่ในระยะ (เฉพาะ owner เท่านั้น)
    private FishCollectionManager nearbyManager = null;
    private bool alreadyCollectedByAll = false; // ถ้าต้องการ disable หลัง collect ครั้งแรก

    // ──────────────────────────────────────────────────────────────────────
    // Unity Lifecycle
    // ──────────────────────────────────────────────────────────────────────

    void Start()
    {
        // ซ่อน prompt ตั้งต้น
        if (worldPressEPrompt != null) worldPressEPrompt.SetActive(false);
        if (collectedEffect   != null) collectedEffect.SetActive(false);
    }

    void Update()
    {
        // ตรวจ E key กด — ทำงานเมื่อมี player owner อยู่ใกล้
        if (nearbyManager == null) return;
        if (!Input.GetKeyDown(KeyCode.E)) return;

        TryCollect(nearbyManager);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Trigger Detection (3D Collider ต้องเป็น Trigger)
    // ──────────────────────────────────────────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        FishCollectionManager mgr = GetOwnerManager(other);
        if (mgr == null) return;

        // ปลาตัวนี้ถูกเก็บไปแล้วโดย player นี้ → ไม่ต้อง show hint
        if (mgr.IsCollected(fishIndex)) return;

        nearbyManager = mgr;

        // แสดง "Press E" บน world
        if (worldPressEPrompt != null) worldPressEPrompt.SetActive(true);

        // แสดง "Press E" บน player HUD
        mgr.ShowPressEHint(true);

        Debug.Log($"[FishMinigameInteractable] Player เข้าใกล้ปลา #{fishIndex} — กด E เพื่อเก็บ");
    }

    void OnTriggerExit(Collider other)
    {
        FishCollectionManager mgr = GetOwnerManager(other);
        if (mgr == null || mgr != nearbyManager) return;

        nearbyManager = null;

        if (worldPressEPrompt != null) worldPressEPrompt.SetActive(false);
        mgr.ShowPressEHint(false);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Collect Logic
    // ──────────────────────────────────────────────────────────────────────

    void TryCollect(FishCollectionManager mgr)
    {
        if (mgr.IsCollected(fishIndex))
        {
            // เก็บไปแล้ว — ซ่อน hint
            if (worldPressEPrompt != null) worldPressEPrompt.SetActive(false);
            mgr.ShowPressEHint(false);
            nearbyManager = null;
            return;
        }

        // ── เก็บปลา ──────────────────────────────────────────────────────
        mgr.CollectFish(fishIndex);

        // ซ่อน hints
        if (worldPressEPrompt != null) worldPressEPrompt.SetActive(false);
        mgr.ShowPressEHint(false);

        // แสดง collected effect
        if (collectedEffect != null) collectedEffect.SetActive(true);

        nearbyManager = null; // ออกจาก state รอ
    }

    // ──────────────────────────────────────────────────────────────────────
    // Helper — ดึง FishCollectionManager จาก Collider (เฉพาะ Owner)
    // ──────────────────────────────────────────────────────────────────────

    FishCollectionManager GetOwnerManager(Collider other)
    {
        // ── Netcode: เช็คว่าเป็น local owner player ─────────────────────
        NetworkObject netObj = other.GetComponent<NetworkObject>()
                            ?? other.GetComponentInParent<NetworkObject>();

        if (netObj != null && !netObj.IsOwner)
            return null; // ไม่ใช่ player ของเราในเครื่องนี้

        // ── ดึง FishCollectionManager ────────────────────────────────────
        FishCollectionManager mgr = other.GetComponent<FishCollectionManager>()
                                 ?? other.GetComponentInParent<FishCollectionManager>();
        return mgr;
    }

    // ──────────────────────────────────────────────────────────────────────
    // Legacy (เก็บไว้ถ้ามี script อื่นเรียก — แต่ไม่ใช้แล้ว)
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>เรียกจาก player script อื่น (backward compat) — ไม่แนะนำ</summary>
    public void StartMinigame(FishMinigameManager playerMinigameManager)
    {
        Debug.LogWarning("[FishMinigameInteractable] StartMinigame() ถูกเรียก แต่ตอนนี้ใช้ระบบ CollectFish แทน\n" +
                         "Player ต้องหาปลาให้ครบ 3 ตัวก่อน jigsaw ถึงจะเริ่ม");
    }
}
