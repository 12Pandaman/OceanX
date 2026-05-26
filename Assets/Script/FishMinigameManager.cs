using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FishMinigameManager : MonoBehaviour
{
    public bool IsMinigamePlaying = false;

    [Header("UI Panels")]
    public GameObject minigameCanvas;
    public GameObject quizPanel;
    public GameObject jigsawPanel;
    public GameObject victoryPanel;

    [Header("Victory Elements")]
    public TMP_Text victoryTimeText;
    public Button victoryRestartButton;

    [Header("Minigame Timer")]
    public TMP_Text currentTimerText;

    [Header("Quiz Elements")]
    public TMP_Text questionText;
    public Button[] choiceButtons;
    public TMP_Text[] choiceTexts;
    public TMP_Text feedbackText;
    public RawImage quizJigsawImage;

    [Header("Jigsaw Elements")]
    [Tooltip("ถ้าว่างจะ auto-detect จาก JigsawPanel")]
    public JigsawPiece[] jigsawPieces;
    [Tooltip("ถ้าว่างจะ auto-detect Slot* จาก JigsawPanel")]
    public RectTransform[] allJigsawSlots;
    [Tooltip("ถ้าว่างจะ auto-detect ปุ่มชื่อ 'submit' หรือ 'next'")]
    public Button nextButton;
    [Tooltip("ถ้าว่างจะ auto-detect ปุ่มชื่อ 'leave', 'close', หรือ 'exit'")]
    public Button leaveButton;
    public float jigsawScatterRadius = 200f;

    [Header("Fish Info Panel")]
    public GameObject fishInfoPanel;
    public TMP_Text fishInfoHeaderText;
    public TMP_Text fishInfoNameText;
    public TMP_Text fishInfoDetailText;

    private string currentCorrectAnswer;
    private bool isTransitioningToQuiz = false;
    private int wrongAnswerCount = 0;
    private Dictionary<RectTransform, JigsawPiece> _occupiedSlots = new Dictionary<RectTransform, JigsawPiece>(); // เพิ่มเพื่อติดตาม slot ที่ถูกครอบครอง
    private FishJigsawVisual jigsawVisual;
    private int currentFishIndex;
    
    private float minigameTimer = 0f;
    private bool isTimerStopped = false; // เริ่มนับทันทีที่ตัวละครเกิด

    private void Awake()
    {
        if (quizPanel != null)   quizPanel.SetActive(false);
        if (jigsawPanel != null) jigsawPanel.SetActive(false);
        if (quizJigsawImage != null) quizJigsawImage.gameObject.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);

        AutoDetectJigsawComponents();

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }

        if (victoryRestartButton != null)
        {
            victoryRestartButton.onClick.RemoveAllListeners();
            victoryRestartButton.onClick.AddListener(RestartGame);
        }
    }

    private void Update()
    {
        if (!isTimerStopped)
        {
            minigameTimer += Time.deltaTime;
            
            if (currentTimerText != null)
            {
                int minutes = Mathf.FloorToInt(minigameTimer / 60F);
                int seconds = Mathf.FloorToInt(minigameTimer - minutes * 60);
                currentTimerText.text = $"{minutes:00}:{seconds:00}";
                // Log ถูกคอมเมนต์ออกเพื่อลดข้อความใน Console และปรับปรุงประสิทธิภาพ
                // Debug.Log($"[FishMinigame] Timer: {currentTimerText.text} (Raw: {minigameTimer:F2})");
            }
            else
            {
                // แจ้งเตือนเพื่อให้รู้ว่าช่อง Current Timer Text ว่างอยู่
                if (Time.frameCount % 60 == 0)
                {
                    Debug.LogWarning("⚠️ [FishMinigame] เวลากำลังเดิน แต่หาช่อง 'Current Timer Text' ไม่เจอ! โปรดลาก UI Text มาใส่ใน Inspector ครับ");
                }
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    // Jigsaw Slot Management (Called by JigsawPiece)
    // ──────────────────────────────────────────────────────────────────────

    public bool TryOccupySlot(RectTransform slot, JigsawPiece piece)
    {
        if (slot == null || piece == null) return false;
        // If slot is occupied by another piece, fail. If it's the same piece, succeed.
        if (_occupiedSlots.TryGetValue(slot, out JigsawPiece existingPiece))
        {
            return existingPiece == piece;
        }
        _occupiedSlots[slot] = piece;
        return true;
    }

    public void FreeSlot(RectTransform slot, JigsawPiece piece)
    {
        // Only remove if the slot is occupied by the piece that is being freed.
        if (slot != null && _occupiedSlots.ContainsKey(slot) && _occupiedSlots[slot] == piece)
        {
            _occupiedSlots.Remove(slot);
        }
    }

    /// <summary>
    /// Auto-detect jigsawPieces, allJigsawSlots, nextButton
    /// จาก Hierarchy ถ้ายังไม่ได้ Assign ใน Inspector
    /// </summary>
    private void AutoDetectJigsawComponents()
    {
        Transform panel = jigsawPanel != null ? jigsawPanel.transform : transform;

        // ── Auto-detect JigsawPieces ──────────────────────────────────────
        if (jigsawPieces == null || jigsawPieces.Length == 0)
        {
            jigsawPieces = panel.GetComponentsInChildren<JigsawPiece>(true);
            if (jigsawPieces.Length > 0)
                Debug.Log($"[FishMinigameManager] Auto-detect jigsawPieces: {jigsawPieces.Length} ชิ้น");
            else
                Debug.LogWarning("[FishMinigameManager] ไม่พบ JigsawPiece ใต้ JigsawPanel — ลาก Assign ใน Inspector ด้วย");
        }

        // ── Auto-detect Slots (RectTransform ชื่อขึ้นต้นด้วย Slot) ─────────
        if (allJigsawSlots == null || allJigsawSlots.Length == 0)
        {
            var slots = new List<RectTransform>();
            foreach (Transform child in panel)
            {
                if (child.name.ToLower().StartsWith("slot"))
                {
                    RectTransform rt = child.GetComponent<RectTransform>();
                    if (rt != null) slots.Add(rt);
                }
            }
            allJigsawSlots = slots.ToArray();
            if (allJigsawSlots.Length > 0)
                Debug.Log($"[FishMinigameManager] Auto-detect Slots: {allJigsawSlots.Length} ช่อง");
            else
                Debug.LogWarning("[FishMinigameManager] ไม่พบ Slot ใต้ JigsawPanel — ตั้งชื่อ child ให้ขึ้นต้นด้วย 'Slot'");
        }

        jigsawVisual = GetComponentInChildren<FishJigsawVisual>(true);
        if (jigsawVisual == null)
            Debug.LogWarning("[FishMinigameManager] ไม่พบ FishJigsawVisual — รูปจิ๊กซอว์ในหน้า Quiz จะไม่แสดง");

        // ── Auto-detect nextButton (ชื่อ submit / next) ───────────────────
        Button[] allButtons = minigameCanvas != null 
            ? minigameCanvas.GetComponentsInChildren<Button>(true) 
            : GetComponentsInChildren<Button>(true);
            
        foreach (var btn in allButtons)
        {
            string n = btn.name.ToLower();
            if (nextButton == null && (n.Contains("submit") || n.Contains("next")))
            {
                nextButton = btn;
                Debug.Log($"[FishMinigameManager] Auto-detect nextButton: '{btn.name}'");
            }
            if (leaveButton == null && (n.Contains("leave") || n.Contains("close") || n.Contains("exit") || n.Contains("quit")))
            {
                leaveButton = btn;
                Debug.Log($"[FishMinigameManager] Auto-detect leaveButton: '{btn.name}'");
            }
        }

        if (nextButton == null)
            Debug.LogWarning("[FishMinigameManager] ไม่พบปุ่ม submit/next — ตั้งชื่อปุ่มให้มีคำว่า 'submit' หรือ 'next'");

        // ── Auto-detect victoryRestartButton ───────────────────
        if (victoryRestartButton == null && victoryPanel != null)
        {
            Button[] vicButtons = victoryPanel.GetComponentsInChildren<Button>(true);
            foreach (var btn in vicButtons)
            {
                string n = btn.name.ToLower();
                if (n.Contains("restart") || n.Contains("play") || n.Contains("leave") || n.Contains("exit") || n.Contains("home"))
                {
                    victoryRestartButton = btn;
                    Debug.Log($"[FishMinigameManager] Auto-detect victoryRestartButton: '{btn.name}'");
                    break;
                }
            }
        }

        // ── Auto-detect victoryTimeText ───────────────────
        if (victoryTimeText == null && victoryPanel != null)
        {
            TMP_Text[] vicTexts = victoryPanel.GetComponentsInChildren<TMP_Text>(true);
            TMP_Text fallback = null;
            foreach (var txt in vicTexts)
            {
                string n = txt.name.ToLower();
                // หาตัวที่ชื่อมี "time" หรือ "score" ก่อน
                if (n.Contains("time") || n.Contains("score"))
                {
                    victoryTimeText = txt;
                    Debug.Log($"[FishMinigameManager] Auto-detect victoryTimeText: '{txt.name}'");
                    break;
                }
                // เก็บ fallback = text ตัวแรกที่ไม่ใช่ชื่อ title/ปุ่ม
                if (fallback == null && !n.Contains("victory") && !n.Contains("play") && !n.Contains("again") && !n.Contains("title"))
                    fallback = txt;
            }
            // ถ้าหาชื่อตรงไม่เจอ → ใช้ fallback ตัวแรกที่เหลือ
            if (victoryTimeText == null && fallback != null)
            {
                victoryTimeText = fallback;
                Debug.Log($"[FishMinigameManager] Auto-detect victoryTimeText (fallback): '{fallback.name}'");
            }
            if (victoryTimeText == null)
                Debug.LogWarning("[FishMinigameManager] ไม่พบ Text สำหรับแสดงเวลาใน Victory Panel — ลาก Assign 'Victory Time Text' ใน Inspector ด้วยครับ");
        }

        // ── Auto-detect currentTimerText ───────────────────
        if (currentTimerText == null && minigameCanvas != null)
        {
            TMP_Text[] allTexts = minigameCanvas.GetComponentsInChildren<TMP_Text>(true);
            foreach (var txt in allTexts)
            {
                string n = txt.name.ToLower();
                if ((n.Contains("timer") || n.Contains("time")) && (victoryTimeText == null || txt != victoryTimeText) && (victoryPanel == null || !txt.transform.IsChildOf(victoryPanel.transform)))
                {
                    currentTimerText = txt;
                    Debug.Log($"[FishMinigameManager] Auto-detect currentTimerText: '{txt.name}'");
                    break;
                }
            }
        }
        if (currentTimerText == null)
            Debug.LogWarning("[FishMinigameManager] ไม่พบ Text สำหรับจับเวลา — ลาก Assign 'Current Timer Text' ใน Inspector ด้วยครับ");

        if (leaveButton != null)
        {
            leaveButton.onClick.RemoveAllListeners();
            leaveButton.onClick.AddListener(CloseMinigame);
        }
    }

    public void StartMinigame(string question, string[] choices, int correctIndex, int fishIndex)
    {
        IsMinigamePlaying = true;
        // ไม่ reset timer — นับต่อเนื่องจากตอนที่ตัวละครเกิด

        isTransitioningToQuiz = false; // Reset transition flag
        wrongAnswerCount = 0; // Reset wrong answer count
        this.currentFishIndex = fishIndex;
        
        if (nextButton != null) nextButton.interactable = false;
        if (feedbackText != null) feedbackText.text = "";

        if (questionText != null)
        {
            questionText.text = question;
        }
        else
        {
            Debug.LogWarning("⚠️ [FishMinigame] หา Question Text ไม่เจอ! อย่าลืมลาก UI Text มาใส่ในช่อง Question Text ที่หน้า Inspector นะครับ");
        }
        currentCorrectAnswer = choices[correctIndex];

        // --- สุ่มลำดับตัวเลือก (Shuffle) ---
        string[] shuffledChoices = (string[])choices.Clone();
        for (int i = 0; i < shuffledChoices.Length; i++)
        {
            int rnd = Random.Range(0, shuffledChoices.Length);
            string temp = shuffledChoices[i];
            shuffledChoices[i] = shuffledChoices[rnd];
            shuffledChoices[rnd] = temp;
        }

        // สร้างปุ่มตามตัวเลือกที่ถูกสุ่มแล้ว
        if (choiceButtons != null)
        {
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (choiceButtons[i] == null) continue;
                
                if (i < shuffledChoices.Length)
                {
                    choiceButtons[i].gameObject.SetActive(true);
                    if (choiceTexts != null && i < choiceTexts.Length && choiceTexts[i] != null)
                    {
                        // สร้างตัวอักษร A, B, C นำหน้าคำตอบอัตโนมัติ
                        char prefix = (char)('A' + i);
                        choiceTexts[i].text = $"{prefix}. {shuffledChoices[i]}";
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ [FishMinigame] หา Choice Text ของปุ่มที่ {i + 1} ไม่เจอ! ลาก UI Text มาใส่ให้ครบด้วยนะครับ");
                    }
                    
                    int index = i; // Store index for use inside the Event
                    choiceButtons[i].onClick.RemoveAllListeners();
                    choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(shuffledChoices[index]));
                }
                else
                {
                    choiceButtons[i].gameObject.SetActive(false);
                }
            }
        }

        // เฟส 1: เริ่มต้นมินิเกมด้วยการเปิดหน้าจิกซอว์ขึ้นมาก่อน (เพื่อให้ชิ้นส่วนพร้อมจดจำตำแหน่งดั้งเดิมก่อนถูกสุ่ม)
        StartJigsawPhase();

        // รีเซ็ตชิ้นส่วนจิกซอว์และสุ่มตำแหน่งให้กระจัดกระจาย
        if (jigsawPieces != null)
        {
            foreach (var piece in jigsawPieces)
            {
                if (piece != null)
                {
                    piece.ResetPiece();
                    piece.Scatter(jigsawScatterRadius); // สุ่มตำแหน่งกระจาย
                }
            }
        }

    }

    private void StartJigsawPhase()
    {
        isTransitioningToQuiz = false;
        if (quizPanel != null) quizPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (quizJigsawImage != null) quizJigsawImage.gameObject.SetActive(false);
        if (fishInfoPanel != null) fishInfoPanel.SetActive(false); // ปิด DataName ตอนเปิด Jigsaw
        
        if (jigsawPanel != null) jigsawPanel.SetActive(true);
        else Debug.LogWarning("⚠️ [FishMinigame] หา 'Jigsaw Panel' ไม่เจอ! อย่าลืมลาก UI มาใส่ในสคริปต์บน Player Prefab นะครับ");
        
        if (nextButton != null) 
        {
            nextButton.interactable = false;
            nextButton.gameObject.SetActive(false); // ซ่อนปุ่มไว้จนกว่าจะต่อเสร็จ
        }
        if (leaveButton != null)
        {
            leaveButton.gameObject.SetActive(true); // บังคับให้ปุ่มปรากฏ
            leaveButton.interactable = true;        // บังคับให้ปุ่มกดได้
        }
        if (feedbackText != null) feedbackText.text = "";

        // เคลียร์สถานะ slot ที่ถูกครอบครองทั้งหมดเมื่อเริ่ม Jigsaw Phase
        _occupiedSlots.Clear();
        if (jigsawPieces != null)
        {
            foreach (var piece in jigsawPieces)
            {
                if (piece != null) piece.ResetPiece(); // ResetPiece จะเคลียร์ CurrentSlot ด้วย
            }
        }
    }

    public void CheckJigsawCompletion()
    {
        if (isTransitioningToQuiz) return; // Prevent multiple calls while transitioning
        if (jigsawPieces == null || jigsawPieces.Length == 0)
        {
            // No pieces to check, so nothing is complete.
            if (nextButton != null)
            {
                nextButton.interactable = false;
                nextButton.gameObject.SetActive(false);
            }
            return;
        }

        int correctCount = 0;
        int totalPieces = jigsawPieces.Length;

        foreach (var piece in jigsawPieces)
        {
            if (piece == null || piece.targetSlot == null) continue;

            RectTransform pieceRect = piece.GetComponent<RectTransform>();
            if (pieceRect == null) continue;

            // เช็คว่าชิ้นส่วนถูกวางตรงกับช่องเป้าหมายของมันหรือไม่
            float distance = Vector2.Distance(pieceRect.anchoredPosition, piece.targetSlot.anchoredPosition);
            if (distance <= 5f) // อนุโลมความคลาดเคลื่อน 5 หน่วย (pixels)
                correctCount++;
        }

        // All pieces must be in their correct places.
        bool allCorrect = totalPieces > 0 && correctCount == totalPieces;

        if (allCorrect)
        {
            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(true); // เปิดปุ่มเมื่อต่อเสร็จ
                nextButton.interactable = true;
            }
            if (feedbackText != null) feedbackText.text = "<color=green>Jigsaw Complete! Press Next.</color>";
        }
        else
        {
            if (nextButton != null)
            {
                nextButton.interactable = false;
                nextButton.gameObject.SetActive(false); // ปิดปุ่มไว้ถ้าดึงชิ้นส่วนออก
            }
            if (feedbackText != null) feedbackText.text = $""; // ล้างข้อความถ้าดึงชิ้นส่วนออก
        }
    }

    public void OnNextButtonClicked()
    {
        if (nextButton == null || !nextButton.interactable) return;
        
        isTransitioningToQuiz = true;
        StartQuizPhase();
    }

    private void StartQuizPhase()
    {
        if (jigsawPanel != null) jigsawPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);

        // ไม่หยุดเวลา — นับต่อไปจนกว่าจะตอบ Quiz เสร็จ

        if (quizPanel != null) quizPanel.SetActive(true);
        else Debug.LogWarning("⚠️ [FishMinigame] หา 'Quiz Panel' ไม่เจอ! อย่าลืมลาก UI มาใส่ในสคริปต์บน Player Prefab นะครับ");
        
        if (feedbackText != null) feedbackText.text = ""; // ล้างข้อความแจ้งเตือนที่ค้างอยู่

        if (quizJigsawImage != null && jigsawVisual != null)
        {
            if (currentFishIndex >= 0 && currentFishIndex < jigsawVisual.fishTextures.Length)
            {
                quizJigsawImage.texture = jigsawVisual.fishTextures[currentFishIndex];
                quizJigsawImage.gameObject.SetActive(true);
            }
            else
            {
                quizJigsawImage.gameObject.SetActive(false);
            }
        }

        // Re-enable buttons for the quiz
        if (choiceButtons != null)
        {
            foreach (var button in choiceButtons)
            {
                if (button != null) button.interactable = true;
            }
        }
    }

    public void OnChoiceSelected(string selectedChoice)
    {
        if (choiceButtons != null)
        {
            foreach (var button in choiceButtons) { if (button != null) button.interactable = false; } // Disable buttons after answer
        }
        
        if (selectedChoice == currentCorrectAnswer)
        {
            if (feedbackText != null) feedbackText.text = "<color=green>Correct!</color>";
            Invoke(nameof(ShowVictoryPanel), 1.5f); // เฟส 3: ตอบถูกแล้ว เปิดหน้า Victory
        }
        else
        {
            wrongAnswerCount++;
            if (wrongAnswerCount >= 3) // ตอบผิดได้ 2 ครั้ง ถ้าครั้งที่ 3 ให้เริ่มใหม่
            {
                if (feedbackText != null) feedbackText.text = "<color=red>Too many wrong attempts! Restarting Jigsaw...</color>";
                Invoke(nameof(ResetToJigsawPhase), 1.5f);
            }
            else
            {
                if (feedbackText != null) feedbackText.text = $"<color=red>Wrong, try again! ({3 - wrongAnswerCount} attempts left)</color>";
                Invoke(nameof(ReenableQuizButtons), 1.5f);
            }
        }
    }

    private void ShowVictoryPanel()
    {
        if (quizPanel != null) quizPanel.SetActive(false);
        if (jigsawPanel != null) jigsawPanel.SetActive(false);
        if (quizJigsawImage != null) quizJigsawImage.gameObject.SetActive(false);

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);

            isTimerStopped = true; // หยุดเวลาให้ชัวร์อีกครั้ง
            if (victoryTimeText != null)
            {
                int minutes = Mathf.FloorToInt(minigameTimer / 60F);
                int seconds = Mathf.FloorToInt(minigameTimer - minutes * 60);
                victoryTimeText.text = $"{minutes:00}:{seconds:00}";
            }
        }
        else
        {
            CloseMinigame(); // ถ้าไม่ได้ใส่ Victory Panel ไว้ ให้ปิดมินิเกมตามปกติ
        }
    }

    private void ReenableQuizButtons()
    {
        if (feedbackText != null) feedbackText.text = "";
        if (choiceButtons != null)
        {
            foreach (var button in choiceButtons) { if (button != null) button.interactable = true; }
        }
    }

    private void ResetToJigsawPhase()
    {
        isTimerStopped = false; // ถ้าตอบผิดแล้วกลับมาหน้าจิ๊กซอว์ ให้เดินเวลาต่อ
        _occupiedSlots.Clear(); // เคลียร์สถานะ slot ที่ถูกครอบครอง
        wrongAnswerCount = 0;
        if (jigsawPieces != null)
        {
            foreach (var piece in jigsawPieces)
            {
                if (piece != null)
                {
                    piece.ResetPiece(); // ResetPiece จะเคลียร์ CurrentSlot ด้วย
                    piece.Scatter(jigsawScatterRadius);
                }
            }
        }
        StartJigsawPhase();
    }

    public void CloseMinigame()
    {
        IsMinigamePlaying = false;
        // ไม่หยุดนับเวลา — timer ยังเดินต่อเพื่อวัด session รวม
        if (quizPanel != null) quizPanel.SetActive(false);
        if (jigsawPanel != null) jigsawPanel.SetActive(false);
        if (minigameCanvas != null) minigameCanvas.SetActive(false); // ปิด Canvas หลักทิ้ง เพื่อไม่ให้พื้นหลังค้าง
        if (quizJigsawImage != null) quizJigsawImage.gameObject.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
    }

    public void ShowFishInfo(string header, string fishName, string info)
    {
        if (fishInfoPanel != null) fishInfoPanel.SetActive(true);
        if (fishInfoHeaderText != null) fishInfoHeaderText.text = header;
        if (fishInfoNameText != null) fishInfoNameText.text = fishName;
        if (fishInfoDetailText != null) fishInfoDetailText.text = info;
        
        // ปลดล็อคเมาส์เผื่อว่าในหน้าต่างนี้มีปุ่มให้คลิก
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void HideFishInfo()
    {
        if (fishInfoPanel != null) fishInfoPanel.SetActive(false);
        // เมื่อหน้าต่างปิด เมาส์จะถูกล็อคกลับไปโดย MainPlayerScript (LateUpdate) โดยอัตโนมัติ
    }

    public void RestartGame()
    {
        // ปลดเมาส์ก่อนเพื่อป้องกันเมาส์หาย
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // ตัดการเชื่อมต่อและรีโหลดฉาก (เหมือนกด Leave Game กลับไปหน้าแรก)
        if (GameMenuManager.Instance != null)
        {
            GameMenuManager.Instance.LeaveGame(); 
        }
        else
        {
            if (Unity.Netcode.NetworkManager.Singleton != null) Unity.Netcode.NetworkManager.Singleton.Shutdown();
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }
}