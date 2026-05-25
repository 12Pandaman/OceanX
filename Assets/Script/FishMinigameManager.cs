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

    private string currentCorrectAnswer;

    private void Awake()
    {
        if (quizPanel != null) quizPanel.SetActive(false);
        if (jigsawPanel != null) jigsawPanel.SetActive(false);
    }

    public void StartMinigame(string question, string[] choices, int correctIndex)
    {
        IsMinigamePlaying = true;
        
        if (quizPanel != null) quizPanel.SetActive(true);
        else Debug.LogError("⚠️ [FishMinigame] หา 'Quiz Panel' ไม่เจอ! อย่าลืมลาก UI มาใส่ในสคริปต์บน Player Prefab นะครับ");

        if (jigsawPanel != null) jigsawPanel.SetActive(false);
        else Debug.LogError("⚠️ [FishMinigame] หา 'Jigsaw Panel' ไม่เจอ! อย่าลืมลาก UI มาใส่ในสคริปต์บน Player Prefab นะครับ");
        
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

        // Create buttons based on provided choices
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (i < choices.Length)
            {
                choiceButtons[i].gameObject.SetActive(true);
                if (choiceTexts != null && i < choiceTexts.Length && choiceTexts[i] != null)
                {
                    // สร้างตัวอักษร A, B, C นำหน้าคำตอบอัตโนมัติ
                    char prefix = (char)('A' + i);
                    choiceTexts[i].text = $"{prefix}. {choices[i]}";
                }
                else
                {
                    Debug.LogWarning($"⚠️ [FishMinigame] หา Choice Text ของปุ่มที่ {i + 1} ไม่เจอ! ลาก UI Text มาใส่ให้ครบด้วยนะครับ");
                }
                
                int index = i; // Store index for use inside the Event
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(choices[index]));
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }
    }

    public void OnChoiceSelected(string selectedChoice)
    {
        if (selectedChoice == currentCorrectAnswer)
        {
            if (feedbackText != null) feedbackText.text = "<color=green>Correct!</color>";
            Invoke(nameof(StartJigsawPhase), 1f); // Wait 1 sec then go to Jigsaw phase
        }
        else
        {
            if (feedbackText != null) feedbackText.text = "<color=red>Wrong, try again!</color>";
        }
    }

    private void StartJigsawPhase()
    {
        quizPanel.SetActive(false);
        jigsawPanel.SetActive(true); // Open Jigsaw panel
    }

    public void CloseMinigame()
    {
        IsMinigamePlaying = false;
        if (quizPanel != null) quizPanel.SetActive(false);
        if (jigsawPanel != null) jigsawPanel.SetActive(false);
    }
}