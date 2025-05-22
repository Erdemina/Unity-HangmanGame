using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using System.Text;

public class ClickManager : MonoBehaviour
{
	public static ClickManager Instance;

	[Header("UI Elements")]
	public Button BTN_givepoint;
	public TextMeshProUGUI TEX_history;
	public TextMeshProUGUI TEX_totalpoint;
	public TextMeshProUGUI TEX_totalclicks;
	public TextMeshProUGUI TEX_username;

	private string apiUrl = "https://5wcorlr6ic.execute-api.eu-north-1.amazonaws.com/v2";

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
		}
	}

	private void Start()
	{
		BTN_givepoint.onClick.AddListener(() => StartCoroutine(GiveTrophy()));
		UpdateUI();
	}

	private IEnumerator GiveTrophy()
	{
		string userId = AuthManager.Instance.UserID;

		// JSON verisi (Yeni yapı)
		string jsonData = $"{{\"userId\": \"{userId}\", \"trophies\": 10}}";
		Debug.Log($"Sending JSON to API: {jsonData}");

		byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

		using (UnityWebRequest request = new UnityWebRequest(apiUrl + "/addTrophy", "POST"))
		{
			request.uploadHandler = new UploadHandlerRaw(bodyRaw);
			request.downloadHandler = new DownloadHandlerBuffer();
			request.SetRequestHeader("Content-Type", "application/json");
			request.SetRequestHeader("Accept", "application/json");

			yield return request.SendWebRequest();

			Debug.Log($"Response Code: {request.responseCode}");
			Debug.Log($"Raw Response: {request.downloadHandler.text}");

			if (request.result != UnityWebRequest.Result.Success)
			{
				Debug.LogError($"Trophy update failed: {request.error}\nServer Response: {request.downloadHandler.text}");
			}
			else
			{
				Debug.Log($"Trophy update successful: {request.downloadHandler.text}");
				try
				{
					TrophyResponse response = JsonUtility.FromJson<TrophyResponse>(request.downloadHandler.text);
					if (response != null)
					{
						TEX_history.text += $"\n+10 trophies on {response.timestamp}";
						TEX_totalpoint.text = $"Total Trophies: {response.totalTrophies}";
					}
				}
				catch (System.Exception e)
				{
					Debug.LogError($"JSON Parsing Error: {e.Message}\nResponse: {request.downloadHandler.text}");
				}
			}
		}
	}

	private void UpdateUI()
	{
		TEX_username.text = $"Username: {AuthManager.Instance.Username}";
		TEX_totalpoint.text = "Total Trophies: 0";
		TEX_totalclicks.text = ""; 
	}
}

[System.Serializable]
public class TrophyResponse
{
	public string userId;
	public int totalTrophies;
	public string timestamp;
}
