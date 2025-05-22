using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Unity.Netcode;

public class HealthBarController : MonoBehaviour
{
    public static HealthBarController Instance;
    private void Awake() => Instance = this;

    public GameObject[] p1Bars; // 0: 100, 1: 90, ..., 10: 0
    public GameObject[] p2Bars;

    private int p1Health = 100;
    private int p2Health = 100;

    void Start()
    {
        UpdateHealthBar(p1Bars, 100);
        UpdateHealthBar(p2Bars, 100);
    }

    public void DamagePlayer(int playerNumber, int damage)
    {
        if (playerNumber == 1)
        {
            p1Health -= damage;
            p1Health = Mathf.Clamp(p1Health, 0, 100);
            UpdateHealthBar(p1Bars, p1Health);
        }
        else if (playerNumber == 2)
        {
            p2Health -= damage;
            p2Health = Mathf.Clamp(p2Health, 0, 100);
            UpdateHealthBar(p2Bars, p2Health);
        }

        CheckForDeath();
    }

    void UpdateHealthBar(GameObject[] barArray, int health)
    {
        // Tüm barları kapat
        foreach (GameObject bar in barArray)
            bar.SetActive(false);

        // Sağlığa göre doğru barı aç
        int index = (100 - health) / 10;
        barArray[index].SetActive(true);
    }

    void CheckForDeath()
    {
        int p1 = GetPlayerHealth(1);
        int p2 = GetPlayerHealth(2);
        if (p1 <= 0)
        {
            Debug.Log("P1 öldü. P2 kazandı!");
            // Notify HangmanGameManager that P2 won
            if (HangmanGameManager.Instance != null)
            {
                bool isP1 = NetworkManager.Singleton.LocalClientId == HangmanGameManager.Instance.hostClientId.Value;
                // If local player is P1, they lost. If they're P2, they won.
                bool localPlayerWon = !isP1;
                Debug.Log($"P1 died - Local player is P1: {isP1}, Local player won: {localPlayerWon}");
                HangmanGameManager.Instance.EndGame(false, p1, p2); // false means host (P1) lost
            }
        }
        else if (p2 <= 0)
        {
            Debug.Log("P2 öldü. P1 kazandı!");
            // Notify HangmanGameManager that P1 won
            if (HangmanGameManager.Instance != null)
            {
                bool isP1 = NetworkManager.Singleton.LocalClientId == HangmanGameManager.Instance.hostClientId.Value;
                // If local player is P1, they won. If they're P2, they lost.
                bool localPlayerWon = isP1;
                Debug.Log($"P2 died - Local player is P1: {isP1}, Local player won: {localPlayerWon}");
                HangmanGameManager.Instance.EndGame(true, p1, p2); // true means host (P1) won
            }
        }
    }

    // 🔽 Yeni eklenen fonksiyon: Can değerini dışarıdan almayı sağlar
    public int GetPlayerHealth(int playerNumber)
    {
        return (playerNumber == 1) ? p1Health : p2Health;
    }
}
