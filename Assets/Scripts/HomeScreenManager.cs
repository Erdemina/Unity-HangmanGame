using UnityEngine;
using System.Collections;

public class HomeScreenManager : MonoBehaviour
{
    void Start()
    {
        // Sahne yüklendiğinde UI'ı güncelle
        StartCoroutine(InitializeUI());
    }

    private IEnumerator InitializeUI()
    {
        // Sahnenin tamamen yüklenmesi için kısa bir bekleme
        yield return new WaitForSeconds(0.5f);

        if (AuthManager.Instance != null && !string.IsNullOrEmpty(AuthManager.Instance.UserID))
        {
            Debug.Log("[HomeScreenManager] User is logged in, refreshing UI...");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.RefreshUI();
            }
            else
            {
                Debug.LogWarning("[HomeScreenManager] UIManager instance not found!");
            }
        }
        else
        {
            Debug.LogWarning("[HomeScreenManager] User is not logged in!");
        }
    }
} 