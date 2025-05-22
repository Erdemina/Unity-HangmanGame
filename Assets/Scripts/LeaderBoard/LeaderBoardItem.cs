using UnityEngine;
using TMPro;

public class LeaderboardItem : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text usernameText;
    [SerializeField] private TMP_Text scoreText;

    /// <summary>
    /// Skor panosu için kullanıcı verilerini ayarlar.
    /// </summary>
    /// <param name="username">Kullanıcı adı</param>
    /// <param name="score">Skor / kupa sayısı</param>
    public void SetData(string username, int score)
    {
        if (usernameText != null)
            usernameText.text = username;

        if (scoreText != null)
            scoreText.text = score.ToString();
    }
}
