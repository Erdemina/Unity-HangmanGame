using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Collections;

public class LetterButton : MonoBehaviour
{
    [Tooltip("The character this button represents")]
    public char letter;


    [Header("Colors")]
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color wrongColor = Color.red;

    private Button button;
    private Image image;
    private Color defaultColor;

    private void Awake()
    {
        button = GetComponent<Button>();
        image = GetComponent<Image>();
        if (image != null) defaultColor = image.color;

        // Ensure uppercase consistency
        letter = char.ToUpper(letter);

        button.onClick.AddListener(OnButtonClicked);
    }

    private void OnEnable()
    {
        if (HangmanGameManager.Instance == null)
        {
            Debug.LogWarning("HangmanGameManager not found in scene.");
            return;
        }
        // Listen for letter guesses
        HangmanGameManager.Instance.guessedLetters.OnListChanged += OnGuessedLettersChanged;
        // Also listen for word updates (so we know when a correct guess appears)
        HangmanGameManager.Instance.revealedWord.OnValueChanged += OnRevealedWordChanged;

        // Initialize state
        UpdateState();
    }

    private void OnDisable()
    {
        if (HangmanGameManager.Instance != null)
        {
            HangmanGameManager.Instance.guessedLetters.OnListChanged -= OnGuessedLettersChanged;
            HangmanGameManager.Instance.revealedWord.OnValueChanged -= OnRevealedWordChanged;
        }
    }

    private void OnGuessedLettersChanged(NetworkListEvent<char> evt)
    {
        UpdateState();
    }

    private void OnRevealedWordChanged(FixedString64Bytes oldVal, FixedString64Bytes newVal)
    {
        UpdateState();
    }

    private void UpdateState()
    {
        var gm = HangmanGameManager.Instance;
        if (gm == null) return;

        // Has this letter been guessed?
        bool guessed = gm.guessedLetters.Contains(letter);
        button.interactable = !guessed;

        if (!guessed)
        {
            // Reset color for unguessed
            if (image != null) image.color = defaultColor;
            return;
        }

        // Letter was guessed: determine correct vs wrong by checking revealedWord
        string revealed = gm.revealedWord.Value.ToString();
        bool correct = revealed.Contains(letter.ToString());

        if (image != null)
            image.color = correct ? correctColor : wrongColor;
    }

    private void OnButtonClicked()
    {
        // Submit the guess
        HangmanGameManager.Instance.SubmitGuessServerRpc(letter.ToString(), GuessType.Letter);
    }
}
