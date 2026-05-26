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

    [Header("Quiz Elements")]
    public TMP_Text questionText;
    public Button[] choiceButtons;
    public TMP_Text[] choiceTexts;
    public TMP_Text feedbackText;

    [Header("Jigsaw Elements")]
    [Tooltip("ถ้าว่างจะ auto-detect จาก JigsawPanel")]
    public JigsawPiece[] jigsawPieces;
    [Tooltip("ถ้าว่างจะ auto-detect Slot* จาก JigsawPanel")]
    public RectTransform[] allJigsawSlots;
    [Tooltip("ถ้าว่างจะ auto-detect ปุ่มชื่อ 'submit' หรือ 'next'")]
    public Button nextButton;
    public float jigsawScatterRadius = 200f;

    private string currentCorrectAnswer;
    private bool isTransitioningToQuiz = false;
    private int wrongAnswerCount = 0;

    private void Awake()
    {
        if (quizPanel != null)   quizPanel.SetActive(false);
        if (jigsawPanel != null) jigsawPanel.SetActive(false);

        AutoDetectJigsawComponents();

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
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

        // ── Auto-detect nextButton (ชื่อ submit / next) ───────────────────
        if (nextButton == null)
        {
            // หาจาก parent ทั้งหมด (submit อาจอยู่นอก jigsawPanel)
            Button[] allButtons = GetComponentsInChildren<Button>(true);
            foreach (var btn in allButtons)
            {
                string n = btn.name.ToLower();
                if (n.Contains("submit") || n.Contains("next"))
                {
                    nextButton = btn;
                    Debug.Log($"[FishMinigameManager] Auto-detect nextButton: '{btn.name}'");
                    break;
                }
            }
            if (nextButton == null)
                Debug.LogWarning("[FishMinigameManager] ไม่พบปุ่ม submit/next — ตั้งชื่อปุ่มให้มีคำว่า 'submit' หรือ 'next'");
        }
    }

    public void StartMinigame(string question, string[] choices, int correctIndex)
    {
        IsMinigamePlaying = true;
        isTransitioningToQuiz = false; // Reset transition flag
        wrongAnswerCount = 0; // Reset wrong answer count
        
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

        // เฟส 1: เริ่มต้นมินิเกมด้วยการเปิดหน้าจิกซอว์ขึ้นมาก่อน
        StartJigsawPhase();
    }

    private void StartJigsawPhase()
    {
        isTransitioningToQuiz = false;
        if (quizPanel != null) quizPanel.SetActive(false);
        
        if (jigsawPanel != null) jigsawPanel.SetActive(true);
        else Debug.LogWarning("⚠️ [FishMinigame] หา 'Jigsaw Panel' ไม่เจอ! อย่าลืมลาก UI มาใส่ในสคริปต์บน Player Prefab นะครับ");
        
        if (nextButton != null) nextButton.interactable = false;
        if (feedbackText != null) feedbackText.text = "";
    }

    public void CheckJigsawCompletion()
    {
        if (isTransitioningToQuiz) return; // Prevent multiple calls while transitioning
        if (jigsawPieces == null || jigsawPieces.Length == 0) return;

        int validPieceCount = 0;
        int correctCount = 0;

        foreach (var piece in jigsawPieces)
        {
            if (piece == null) continue;

            // ถ้าชิ้นไหนไม่มี targetSlot ให้นับว่าผิด (ไม่ข้ามไป)
            if (piece.targetSlot == null)
            {
                validPieceCount++;
                // ไม่ increment correctCount → ชิ้นนี้จะทำให้ allCorrect = false
                continue;
            }

            RectTransform pieceRect = piece.GetComponent<RectTransform>();
            if (pieceRect == null) continue;

            validPieceCount++;

            // เช็คว่าชิ้นส่วนถูกวางตรงกับช่องเป้าหมายของมันหรือไม่
            float distance = Vector2.Distance(pieceRect.anchoredPosition, piece.targetSlot.anchoredPosition);
            if (distance <= 5f) // อนุโลมความคลาดเคลื่อน 5 หน่วย
                correctCount++;
        }

        // ต้องมีชิ้นที่ valid อย่างน้อย 1 ชิ้น และถูกทุกชิ้น
        bool allCorrect = validPieceCount > 0 && correctCount == validPieceCount;

        if (nextButton != null) nextButton.interactable = allCorrect; // เปิดปุ่ม Next เมื่อถูกหมด

        if (allCorrect)
        {
            if (feedbackText != null) feedbackText.text = "<color=green>Jigsaw Complete! Press Next.</color>";
        }
        else
        {
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
        
        if (quizPanel != null) quizPanel.SetActive(true);
        else Debug.LogWarning("⚠️ [FishMinigame] หา 'Quiz Panel' ไม่เจอ! อย่าลืมลาก UI มาใส่ในสคริปต์บน Player Prefab นะครับ");
        
        if (feedbackText != null) feedbackText.text = ""; // ล้างข้อความแจ้งเตือนที่ค้างอยู่

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
            Invoke(nameof(CloseMinigame), 1.5f); // เฟส 3: ตอบถูกแล้ว ปิดมินิเกมได้เลย
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
        wrongAnswerCount = 0;
        if (jigsawPieces != null)
        {
            foreach (var piece in jigsawPieces)
            {
                if (piece != null)
                {
                    piece.ResetPiece();
                    piece.Scatter(jigsawScatterRadius);
                }
            }
        }
        StartJigsawPhase();
    }

    public void CloseMinigame()
    {
        IsMinigamePlaying = false;
        if (quizPanel != null) quizPanel.SetActive(false);
        if (jigsawPanel != null) jigsawPanel.SetActive(false);
    }
}