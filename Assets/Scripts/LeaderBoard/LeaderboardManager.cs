using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Newtonsoft.Json;

public class LeaderboardManager : MonoBehaviour
{
    [Header("API Settings")]
    [SerializeField] private string apiUrl = "https://4knen6mrs1.execute-api.eu-north-1.amazonaws.com/leaderboard";

    [Header("UI References")]
    [SerializeField] private GameObject leaderboardItemPrefab;
    [SerializeField] private Transform contentParent;

    private void OnEnable()
    {
        StartCoroutine(FetchLeaderboardData());
    }

    IEnumerator FetchLeaderboardData()
    {
        UnityWebRequest request = UnityWebRequest.Get(apiUrl);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ API Hatası: " + request.error);
            yield break;
        }

        string json = request.downloadHandler.text;

        List<LeaderboardEntry> entries = JsonConvert.DeserializeObject<List<LeaderboardEntry>>(json);
        PopulateLeaderboard(entries);
    }

    void PopulateLeaderboard(List<LeaderboardEntry> entries)
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        entries.Sort((a, b) => b.trophies.CompareTo(a.trophies));

        foreach (var entry in entries)
        {
            GameObject item = Instantiate(leaderboardItemPrefab, contentParent);

            // 👇👇 ÖNEMLİ: Mutlaka aktif hale getir
            item.SetActive(true);

            // 👇 Ek olarak çocuk objelerin hepsi için de aktiflik garantisi:
            foreach (Transform child in item.transform)
            {
                child.gameObject.SetActive(true);
            }

            LeaderboardItem li = item.GetComponent<LeaderboardItem>();
            li.SetData(entry.username, entry.trophies);
        }
    }

}
