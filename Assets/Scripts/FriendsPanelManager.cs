using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class FriendsPanelManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject matchHistoryItemPrefab; // Assign in Inspector
    public Transform matchHistoryContentParent; // Assign in Inspector (Content of ScrollView or VerticalLayoutGroup)

    private string apiUrl = "https://6mfqpxj1i0.execute-api.eu-north-1.amazonaws.com/gethistory";

    void Start()
    {
        // When the friends panel is opened, get the match history
        if (AuthManager.Instance != null && !string.IsNullOrEmpty(AuthManager.Instance.UserID))
        {
            StartCoroutine(GetMatchHistory(AuthManager.Instance.UserID));
        }
    }

    private IEnumerator GetMatchHistory(string userId)
    {
        // Create request body
        var requestData = new MatchHistoryRequest { userId = userId };
        string jsonBody = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 10; // Set timeout to 10 seconds

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                MatchHistoryResponse matchHistory = JsonUtility.FromJson<MatchHistoryResponse>(response);
                ShowMatchHistory(matchHistory.matches);
            }
            else
            {
                Debug.LogError($"Error getting match history: {request.error}");
                Debug.LogError($"Response Code: {request.responseCode}");
                Debug.LogError($"Response Headers: {request.GetResponseHeaders()}");
                Debug.LogError($"Response Body: {request.downloadHandler?.text}");
            }
        }
    }

    private void ShowMatchHistory(List<MatchData> matches)
    {
        // Clear previous items
        foreach (Transform child in matchHistoryContentParent)
            Destroy(child.gameObject);

        if (matches == null) return;

        foreach (var match in matches)
        {
            GameObject item = Instantiate(matchHistoryItemPrefab, matchHistoryContentParent);
            // These names must match your prefab's child objects
            var opponentText = item.transform.Find("OpponentText")?.GetComponent<TMP_Text>();
            var resultText = item.transform.Find("ResultText")?.GetComponent<TMP_Text>();
            var dateText = item.transform.Find("DateText")?.GetComponent<TMP_Text>();
            if (opponentText) opponentText.text = match.opponentName;
            if (resultText) resultText.text = $"{match.matchResult} | {match.trophyCount}";
            if (dateText) dateText.text = match.playedAt;
        }
    }
}

[System.Serializable]
public class MatchHistoryRequest
{
    public string userId;
}

[System.Serializable]
public class MatchHistoryResponse
{
    public List<MatchData> matches;
}

[System.Serializable]
public class MatchData
{
    public string matchResult;
    public int trophyCount;
    public string opponentName;
    public string playedAt;
} 