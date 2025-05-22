using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class WinScreen : MonoBehaviour
{
    public GameObject winPanel;

    public GameObject P_1;  // 1. karakterin görseli
    public GameObject P_2;  // 2. karakterin görseli

    public ParticleSystem confettiEffect;

    public Button mainMenuButton;

    void Start()
    {
        mainMenuButton.onClick.AddListener(ReturnToMainMenu);
    }

    private void ReturnToMainMenu()
    {
        if (AuthManager.Instance != null && !string.IsNullOrEmpty(AuthManager.Instance.UserID))
        {
            Debug.Log("Fetching user data before returning to main menu...");
            UIManager.Instance.RefreshUI();
            StartCoroutine(WaitAndLoadMainMenu());
        }
        else
        {
            Debug.LogWarning("AuthManager or UserID not available, loading main menu directly");
            SceneManager.LoadScene("HomeScreen");
        }
    }

    private IEnumerator WaitAndLoadMainMenu()
    {
        Debug.Log("[WinScreen] Waiting for data fetch...");
        yield return new WaitForSeconds(3f);
        Debug.Log("[WinScreen] Loading main menu after data fetch...");
        
        if (UIManager.Instance != null)
        {
            Debug.Log("[WinScreen] Updating UI before scene transition");
            UIManager.Instance.UpdateUserInfoUI();
        }
        else
        {
            Debug.LogWarning("[WinScreen] UIManager instance not found");
        }
        
        SceneManager.LoadScene("HomeScreen");
    }

    public void ShowWinScreen(int winnerID)
    {
        winPanel.SetActive(true);

        // Her iki karakteri önce kapat
        P_1.SetActive(false);
        P_2.SetActive(false);

        // Kazanana göre karakteri göster
        if (winnerID == 1)
            P_1.SetActive(true);
        else if (winnerID == 2)
            P_2.SetActive(true);
    }
}