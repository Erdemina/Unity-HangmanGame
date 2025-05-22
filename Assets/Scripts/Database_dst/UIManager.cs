using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;

public class UIManager : MonoBehaviour
{
	public static UIManager Instance;

	[Header("User Info Display")]
	public TextMeshProUGUI TEX_username;
	public TextMeshProUGUI TEX_Score;


	public UserData CurrentUserData { get; private set; }

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

	public void Initialize()
	{
		// Başlangıçta yapılacak işlemler varsa buraya
	}

	public void SwitchToMainScene()
	{
		SceneManager.LoadScene("HomeScreen");
		StartCoroutine(WaitAndUpdateUI());
		// Kullanıcı adı kaydını garantiye al
		if (ChatManager.Singleton != null)
			ChatManager.Singleton.RegisterLocalPlayerName();
	}

	public void RefreshUI()
	{
		Debug.Log("[UIManager] RefreshUI called");
		if (AuthManager.Instance != null && !string.IsNullOrEmpty(AuthManager.Instance.UserID))
		{
			Debug.Log($"[UIManager] Starting UI refresh for user: {AuthManager.Instance.UserID}");
			StartCoroutine(RefreshUICoroutine());
		}
		else
		{
			Debug.LogWarning("[UIManager] Cannot refresh UI - AuthManager or UserID not available");
			UpdateUserInfoUI();
		}
	}

	private IEnumerator RefreshUICoroutine()
	{
		Debug.Log("[UIManager] Starting RefreshUICoroutine");
		yield return StartCoroutine(FetchUserDataCoroutine(AuthManager.Instance.UserID));
		Debug.Log("[UIManager] Data fetch completed, updating UI");
		UpdateUserInfoUI();
		Debug.Log("[UIManager] UI refresh completed");
	}

	private IEnumerator WaitAndUpdateUI()
	{
		yield return new WaitForSeconds(2f); // sahne yüklenmesini bekle
		RefreshUI();
	}

	private IEnumerator FetchUserDataCoroutine(string userId)
	{
		Debug.Log($"[UIManager] Fetching user data for ID: {userId}");
		if (string.IsNullOrEmpty(userId))
		{
			Debug.LogError("[UIManager] UserID is null or empty");
			yield break;
		}

		string url = $"https://5wcorlr6ic.execute-api.eu-north-1.amazonaws.com/v2/fetchUser?userId={userId}";
		Debug.Log($"[UIManager] API URL: {url}");
		
		using (UnityWebRequest www = UnityWebRequest.Get(url))
		{
			www.SetRequestHeader("Accept", "application/json");
			yield return www.SendWebRequest();

			if (www.result == UnityWebRequest.Result.ConnectionError || 
				www.result == UnityWebRequest.Result.ProtocolError)
			{
				Debug.LogError($"[UIManager] Error fetching user data: {www.error}");
				yield break;
			}

			try
			{
				string jsonResponse = www.downloadHandler.text;
				Debug.Log($"[UIManager] Received response: {jsonResponse}");
				
				if (string.IsNullOrEmpty(jsonResponse))
				{
					Debug.LogError("[UIManager] Empty response from server");
					yield break;
				}

				UserData userData = JsonUtility.FromJson<UserData>(jsonResponse);
				if (userData == null)
				{
					Debug.LogError("[UIManager] Failed to parse user data from JSON");
					yield break;
				}

				Debug.Log($"[UIManager] Successfully parsed user data: {userData.username}, Trophies: {userData.trophies}");
				CurrentUserData = userData;
			}
			catch (System.Exception e)
			{
				Debug.LogError($"[UIManager] Error processing user data: {e.Message}");
			}
		}
	}

	// Sahne yüklendikten sonra kullanıcı ismini UI'da göster
	public void UpdateUserInfoUI(UserData userData = null)
	{
		Debug.Log("[UIManager] UpdateUserInfoUI called");
		
		TEX_username = GameObject.Find("PlayerName")?.GetComponent<TextMeshProUGUI>();
		TEX_Score = GameObject.Find("TrophyScore")?.GetComponent<TextMeshProUGUI>();

		if (TEX_username == null || TEX_Score == null)
		{
			Debug.LogWarning("[UIManager] UI elements not found in scene");
			return;
		}

		if (userData != null)
		{
			Debug.Log($"[UIManager] Using provided user data: {userData.username}, Trophies: {userData.trophies}");
			TEX_username.text = userData.username;
			TEX_Score.text = userData.trophies.ToString();
			CurrentUserData = userData;
		}
		else if (CurrentUserData != null)
		{
			Debug.Log($"[UIManager] Using cached user data: {CurrentUserData.username}, Trophies: {CurrentUserData.trophies}");
			TEX_username.text = CurrentUserData.username;
			TEX_Score.text = CurrentUserData.trophies.ToString();
		}
		else
		{
			Debug.LogWarning("[UIManager] No user data available, using defaults");
			TEX_username.text = "Player1";
			TEX_Score.text = "0";
		}
	}
}
