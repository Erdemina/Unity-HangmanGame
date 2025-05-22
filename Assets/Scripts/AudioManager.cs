using UnityEngine;
public enum NotificationType
{
    CorrectLetter,
    WrongLetter,
    CorrectWord,
    WrongWord,
    PlayersTurn,
    GameWon,
    GameLost
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Notification Audio Clips")]
    public AudioClip correctLetterClip;
    public AudioClip wrongLetterClip;
    public AudioClip correctWordClip;
    public AudioClip wrongWordClip;
    public AudioClip playersTurnClip;
    public AudioClip gameWonClip;
    public AudioClip gameLostClip;

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    [ContextMenu("Play Correct Letter Audio")]
    private void PlayCorrectLetterAudio()
    {
        audioSource.PlayOneShot(correctLetterClip);
    }
    [ContextMenu("Play Wrong Letter Audio")]
    private void PlayWrongLetterAudio()
    {
        audioSource.PlayOneShot(wrongLetterClip);
    }
    [ContextMenu("Play Correct Word Audio")]
    private void PlayCorrectWordAudio()
    {
        audioSource.PlayOneShot(correctWordClip);
    }
    [ContextMenu("Play Wrong Word Audio")]
    private void PlayWrongWordAudio()
    {
        audioSource.PlayOneShot(wrongWordClip);
    }
    [ContextMenu("Play Player's Turn Audio")]
    private void PlayPlayersTurnAudio()
    {
        audioSource.PlayOneShot(playersTurnClip);
    }
    [ContextMenu("Play Game Won Audio")]
    private void PlayGameWonAudio()
    {
        audioSource.PlayOneShot(gameWonClip);
    }
    [ContextMenu("Play Game Lost Audio")]
    private void PlayGameLostAudio()
    {
        audioSource.PlayOneShot(gameLostClip);
    }  

    public void PlayNotification(NotificationType type)
    {
        switch (type)
        {
            case NotificationType.CorrectLetter:
                PlayCorrectLetterAudio();
                break;
            case NotificationType.WrongLetter:
                PlayWrongLetterAudio();
                break;
            case NotificationType.CorrectWord:
                PlayCorrectWordAudio();
                break;
            case NotificationType.WrongWord:
                PlayWrongWordAudio();
                break;
            case NotificationType.PlayersTurn:
                PlayPlayersTurnAudio();
                break;
            case NotificationType.GameWon:
                PlayGameWonAudio();
                break;
            case NotificationType.GameLost:
                PlayGameLostAudio();
                break;
        }
    }
}
