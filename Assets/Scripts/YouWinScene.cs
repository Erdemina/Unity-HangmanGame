using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class YouWinScene : MonoBehaviour
{
    public void ReturnToMainMenu()
    {
        if (AuthManager.Instance != null && !string.IsNullOrEmpty(AuthManager.Instance.UserID))
        {
            Debug.Log("Returning to main menu...");
            StartCoroutine(WaitAndLoadMainMenu());
        }
        else
        {
            Debug.LogWarning("AuthManager or UserID not available");
            SceneManager.LoadScene("HomeScreen");
        }
    }

    private IEnumerator WaitAndLoadMainMenu()
    {
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene("HomeScreen");
    }
} 