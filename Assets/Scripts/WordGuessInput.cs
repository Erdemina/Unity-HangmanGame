using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;

public class WordGuessInput : MonoBehaviour
{
    [SerializeField] private TMP_InputField wordInputField;
    [SerializeField] private Button guessButton;

    private void Awake()
    {
        if (guessButton != null)
        {
            guessButton.onClick.AddListener(OnGuessButtonClicked);
        }
    }

    private void OnEnable()
    {
        if (HangmanGameManager.Instance != null)
        {
            HangmanGameManager.Instance.currentTurn.OnValueChanged += HandleTurnChanged;
            // Set initial state
            HandleTurnChanged(HangmanGameManager.Instance.currentTurn.Value, HangmanGameManager.Instance.currentTurn.Value);
        }
    }

    private void OnDisable()
    {
        if (HangmanGameManager.Instance != null)
        {
            HangmanGameManager.Instance.currentTurn.OnValueChanged -= HandleTurnChanged;
        }
    }

    private void HandleTurnChanged(ulong oldTurn, ulong newTurn)
    {
        // Only allow the button if it's our turn.
        bool isMyTurn = newTurn == NetworkManager.Singleton.LocalClientId;
        guessButton.interactable = isMyTurn;
    }

    private void OnGuessButtonClicked()
    {
        string guess = wordInputField.text.Trim();
        if (!string.IsNullOrEmpty(guess))
        {
            HangmanGameManager.Instance.SubmitGuessServerRpc(guess, GuessType.Word);
            wordInputField.text = "";
        }
    }
}
