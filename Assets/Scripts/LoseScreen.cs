using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class LoseScreen : MonoBehaviour
{
    public GameObject losePanel;

    public GameObject P_1;  // 1. karakterin görseli
    public GameObject P_2;  // 2. karakterin görseli

    public ParticleSystem sadEffect; // östeğe bağlı özgün efekt

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
        Debug.Log("[LoseScreen] Waiting for data fetch...");
        yield return new WaitForSeconds(3f);
        Debug.Log("[LoseScreen] Loading main menu after data fetch...");
        
        if (UIManager.Instance != null)
        {
            Debug.Log("[LoseScreen] Updating UI before scene transition");
            UIManager.Instance.UpdateUserInfoUI();
        }
        else
        {
            Debug.LogWarning("[LoseScreen] UIManager instance not found");
        }
        
        SceneManager.LoadScene("HomeScreen");
    }

    public void ShowLoseScreen(int loserID)
    {
        losePanel.SetActive(true);

        // Her iki karakteri gizle
        P_1.SetActive(false);
        P_2.SetActive(false);

        // Kaybedeni göster
        if (loserID == 1)
            P_1.SetActive(true);
        else if (loserID == 2)
            P_2.SetActive(true);

        // özgün efekt varsa oynat
        if (sadEffect != null)
            sadEffect.Play();
    }
}
