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

    [Header("Fish Data (New System)")]
    public FishDataSO fishData;

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
    private bool isPanelOpen = false;
    
    // เก็บสถานะว่าปลาตัวนี้ถูกเก็บไปแล้วก่อนที่จะเริ่มการโต้ตอบครั้งนี้หรือไม่
    private bool wasCollectedBeforeInteraction = false;

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

        // ถ้า minigame (jigsaw/quiz/victory) กำลังแสดงอยู่ ให้ซ่อน prompt และไม่ทำอะไรเลย
        if (nearbyManager.MinigameManager != null && nearbyManager.MinigameManager.IsMinigamePlaying)
        {
            if (worldPressEPrompt != null && worldPressEPrompt.activeSelf)
            {
                worldPressEPrompt.SetActive(false);
            }
            nearbyManager.ShowPressEHint(false);

            // ถ้า panel ข้อมูลปลาเปิดค้างอยู่ ให้ปิดด้วย
            if (isPanelOpen)
            {
                nearbyManager.MinigameManager.HideFishInfo();
                isPanelOpen = false;
            }
            return;
        }

        if (!Input.GetKeyDown(KeyCode.E)) return;

        FishMinigameManager minigameMgr = nearbyManager.GetComponent<FishMinigameManager>();
        if (minigameMgr == null) return;

        // ตรวจสอบสถานะการเก็บปลา ณ ตอนที่กด E ครั้งแรก
        // (ถ้ากด E ครั้งแรกเพื่อเปิด panel, wasCollectedBeforeInteraction จะถูกตั้งค่าใน OnTriggerEnter)

        if (!isPanelOpen)
        {
            // เปิด Panel ข้อมูลปลา
            if (fishData != null)
            {
                minigameMgr.ShowFishInfo(fishData.fishHeader, fishData.fishName, fishData.fishInfo);
            }
            else
            {
                minigameMgr.ShowFishInfo("No Header", "Unknown", "No Data Available");
            }
            isPanelOpen = true;

            // เก็บปลาทันทีที่เปิด Panel (ถ้ายังไม่เคยเก็บ)
            nearbyManager.CollectFish(fishIndex);
            // แสดง effect การเก็บทันที (ถ้ายังไม่เคยแสดง)
            if (!wasCollectedBeforeInteraction && collectedEffect != null)
                collectedEffect.SetActive(true);

            // หยุดปลาไม่ให้ว่ายหนี
            NetworkFish netFish = GetComponent<NetworkFish>();
            if (netFish != null) netFish.isPaused = true;

            // ซ่อน "Press E" ไปก่อน
            if (worldPressEPrompt != null) worldPressEPrompt.SetActive(false);
            nearbyManager.ShowPressEHint(false);

            Debug.Log($"[FishMinigameInteractable] Opened info for fish #{fishIndex} and collected it (if not already).");
        }
        else
        {
            // ปิด Panel ข้อมูลปลา
            minigameMgr.HideFishInfo();
            isPanelOpen = false;

            // ให้ปลากลับมาว่ายต่อ
            NetworkFish netFish = GetComponent<NetworkFish>();
            if (netFish != null) netFish.isPaused = false;

            // ตรวจสอบว่าปลาครบ 3 ตัวหรือยัง *หลังจาก* ปิด panel
            // และปลาตัวนี้คือตัวที่เพิ่งเก็บไป (ไม่ใช่การอ่านซ้ำ)
            if (!wasCollectedBeforeInteraction && nearbyManager.AreAllFishCollected())
            {
                // ถ้าใช่, ให้เริ่มมินิเกมสุดท้าย
                Debug.Log($"[FishMinigameInteractable] Closed info for fish #{fishIndex}. All fish collected. Starting final minigame.");
                nearbyManager.StartFinalMinigame();
                // ไม่ต้องทำอะไรต่อ เพราะ minigame manager จะ take over
            }
            else
            {
                // ถ้ายังไม่ครบ 3 ตัว หรือเป็นการอ่านข้อมูลซ้ำ
                if (!wasCollectedBeforeInteraction)
                {
                    // ปลาตัวนี้เพิ่งถูกเก็บ (แต่ยังไม่ครบ 3) -> หยุดการโต้ตอบ
                    nearbyManager = null;
                }
                else
                {
                    // อ่านข้อมูลซ้ำ -> แสดง "Press E" คืนมา
                    if (worldPressEPrompt != null) worldPressEPrompt.SetActive(true);
                    nearbyManager.ShowPressEHint(true);
                }
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    // Trigger Detection (3D Collider ต้องเป็น Trigger)
    // ──────────────────────────────────────────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        FishCollectionManager mgr = GetOwnerManager(other);
        if (mgr == null) return;

        // ถ้า minigame (jigsaw/quiz/victory) กำลังแสดงอยู่ ไม่ต้องแสดง prompt
        if (mgr.MinigameManager != null && mgr.MinigameManager.IsMinigamePlaying)
        {
            // nearbyManager จะยังคงเป็น null และ Update() จะไม่ทำงาน
            return;
        }

        // เก็บสถานะการเก็บปลาไว้ก่อนเริ่มการโต้ตอบ
        wasCollectedBeforeInteraction = mgr.IsCollected(fishIndex);

        nearbyManager = mgr;

        // แสดง "Press E" บน world และ HUD เสมอ เพื่อให้ผู้เล่นสามารถอ่านข้อมูลซ้ำได้
        if (worldPressEPrompt != null) worldPressEPrompt.SetActive(true);
        mgr.ShowPressEHint(true);

        if (wasCollectedBeforeInteraction)
        {
            Debug.Log($"[FishMinigameInteractable] Player เข้าใกล้ปลา #{fishIndex} (เก็บไปแล้ว) — กด E เพื่ออ่านข้อมูลซ้ำ");
        }
        else
        {
            Debug.Log($"[FishMinigameInteractable] Player เข้าใกล้ปลา #{fishIndex} — กด E เพื่ออ่านข้อมูล");
        }
    }

    void OnTriggerExit(Collider other)
    {
        FishCollectionManager mgr = GetOwnerManager(other);
        if (mgr == null || mgr != nearbyManager) return;

        // ถ้าเดินออกไปให้ปิด Panel ทันที (เผื่อหลุดออกไปได้)
        if (isPanelOpen)
        {
            FishMinigameManager minigameMgr = nearbyManager.GetComponent<FishMinigameManager>();
            if (minigameMgr != null) minigameMgr.HideFishInfo();
            isPanelOpen = false;

            // ให้ปลากลับมาว่ายต่อ
            NetworkFish netFish = GetComponent<NetworkFish>();
            if (netFish != null) netFish.isPaused = false;
        }

        nearbyManager = null;

        if (worldPressEPrompt != null) worldPressEPrompt.SetActive(false);
        mgr.ShowPressEHint(false);
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
