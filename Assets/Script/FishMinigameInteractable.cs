using UnityEngine;

public class FishMinigameInteractable : MonoBehaviour
{
    [Header("Quiz Settings")]
    public string fishNameQuestion = "What is the name of this fish?";
    public string[] choices = new string[3] { "Shark", "Clownfish", "Dolphin" };
    public int correctChoiceIndex = 1; // Correct choice index (starts from 0)

    public void StartMinigame(FishMinigameManager playerMinigameManager)
    {
        if (playerMinigameManager != null)
        {
            playerMinigameManager.StartMinigame(fishNameQuestion, choices, correctChoiceIndex);
        }
        else
        {
            Debug.LogWarning("FishMinigameManager is missing from the player!");
        }
    }
}