using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FishMinigameManager : MonoBehaviour
{
    public bool IsMinigamePlaying = false;

    [Header("UI Panels")]
    public GameObject minigameCanvas; // Main Canvas
    public GameObject quizPanel;      // Quiz Panel
    public GameObject jigsawPanel;    // Jigsaw Panel

    [Header("Quiz Elements")]
    public TMP_Text questionText;     // Question UI Text
    public Button[] choiceButtons;    // Choice Buttons (should be 3)
    public TMP_Text[] choiceTexts;    // Button Texts
    public TMP_Text feedbackText;     // Feedback Text (Correct/Wrong)

    [Header("Jigsaw Elements")]
    public JigsawPiece[] jigsawPieces; // ใส่ชิ้นส่วนจิกซอว์ที่ลากได้ทั้งหมดลงในช่องนี้
    public RectTransform[] allJigsawSlots; // ใส่ช่องเงา (Slots) ทั้งหมดลงในนี้ เพื่อให้ชิ้นส่วนดูดติดได้ทุกช่อง
    public Button nextButton; // ปุ่ม Next สำหรับไปหน้า Quiz
    public float jigsawScatterRadius = 200f; // รัศมีการสุ่มกระจายชิ้นส่วน

    private string currentCorrectAnswer;
    private bool isTransitioningToQuiz = false;
    private int wrongAnswerCount = 0;

    private void Awake()
    {
        if (quizPanel != null) quizPanel.SetActive(false);
        if (jigsawPanel != null) jigsawPanel.SetActive(false);
        
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
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

        bool allCorrect = true;

        foreach (var piece in jigsawPieces)
        {
            if (piece == null || piece.targetSlot == null) continue;
            
            RectTransform pieceRect = piece.GetComponent<RectTransform>();
            if (pieceRect == null) continue;
            
            // เช็คว่าชิ้นส่วนถูกวางตรงกับช่องเป้าหมายของมันหรือไม่
            float distance = Vector2.Distance(pieceRect.anchoredPosition, piece.targetSlot.anchoredPosition);
            if (distance > 5f) // อนุโลมความคลาดเคลื่อน 5 หน่วย
            {
                allCorrect = false;
                break;
            }
        }
        
        if (nextButton != null) nextButton.interactable = allCorrect; // เปิดปุ่ม Next เมื่อถูกหมด
        
        if (allCorrect)
        {
            if (feedbackText != null) feedbackText.text = "<color=green>Jigsaw Complete! Press Next.</color>";
        }
        else
        {
            if (feedbackText != null) feedbackText.text = ""; // ล้างข้อความถ้าดึงชิ้นส่วนออก
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