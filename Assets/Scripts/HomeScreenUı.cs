using UnityEngine;
using System.Collections;

public class HomeScreenUI : MonoBehaviour
{
    public GameObject leaderboardPanel;
    public GameObject friendsPanel;
    public GameObject settingsPanel;
    private FriendsPanelManager friendsPanelManager;

    void Start()
    {
        // Sahne yüklendiğinde UI'ı güncelle
        StartCoroutine(InitializeUI());
        
        // Get the FriendsPanelManager component
        friendsPanelManager = friendsPanel.GetComponent<FriendsPanelManager>();
        if (friendsPanelManager == null)
        {
            friendsPanelManager = friendsPanel.AddComponent<FriendsPanelManager>();
        }
    }

    private IEnumerator InitializeUI()
    {
        // Sahnenin tamamen yüklenmesi için kısa bir bekleme
        yield return new WaitForSeconds(0.5f);

        if (AuthManager.Instance != null && !string.IsNullOrEmpty(AuthManager.Instance.UserID))
        {
            Debug.Log("[HomeScreenUI] User is logged in, refreshing UI...");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.RefreshUI();
            }
            else
            {
                Debug.LogWarning("[HomeScreenUI] UIManager instance not found!");
            }
        }
        else
        {
            Debug.LogWarning("[HomeScreenUI] User is not logged in!");
        }
    }

    public void OpenLeaderboard()
    {
        CloseAll();
        leaderboardPanel.SetActive(true);
    }

    public void OpenFriends()
    {
        CloseAll();
        friendsPanel.SetActive(true);
        
        // Get match history when friends panel is opened
        if (friendsPanelManager != null)
        {
            friendsPanelManager.enabled = true;
        }
    }

    public void OpenSettings()
    {
        CloseAll();
        settingsPanel.SetActive(true);
    }

    public void CloseAll()
    {
        leaderboardPanel.SetActive(false);
        friendsPanel.SetActive(false);
        settingsPanel.SetActive(false);
    }

    public void LogoutButtonAction()
    {
        if (AuthManager.Instance != null)
            AuthManager.Instance.Logout();
    }
}
