using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class TrophyManager : MonoBehaviour
{
    public static IEnumerator UpdateTrophy(string userId, int trophyChange, System.Action<bool, int> callback = null)
    {
        string url = "https://uxjrhphe8e.execute-api.eu-north-1.amazonaws.com/addTrophy";
        var body = new TrophyRequest { userId = userId, trophies = trophyChange };
        string jsonBody = JsonUtility.ToJson(body);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonUtility.FromJson<TrophyResponse>(request.downloadHandler.text);
            Debug.Log($"Trophy updated! New total: {response.totalTrophies}");
            callback?.Invoke(true, response.totalTrophies);
        }
        else
        {
            Debug.LogError("Trophy update failed: " + request.error);
            callback?.Invoke(false, 0);
        }
    }

    [System.Serializable]
    public class TrophyRequest
    {
        public string userId;
        public int trophies;
    }

    [System.Serializable]
    public class TrophyResponse
    {
        public string userId;
        public int totalTrophies;
        public int change;
    }
} 