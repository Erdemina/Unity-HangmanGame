using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System.Linq;

public class ReconnectManager : MonoBehaviour
{
    [SerializeField] private GameObject networkManagerPrefab;
    [SerializeField] private string gameplaySceneName = "PvP";

    private async void Awake()
    {
        DontDestroyOnLoad(gameObject);

        if (NetworkManager.Singleton == null && networkManagerPrefab != null)
        {
            var nm = Instantiate(networkManagerPrefab);
            DontDestroyOnLoad(nm);
        }

        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log("❗ Unity Auth oturumu açık değil. Reconnect kontrolü atlanıyor.");
            return;
        }

        await TryReconnect();
    }

    private async Task TryReconnect()
    {
        string lobbyId = PlayerPrefs.GetString("lastLobbyId", "");
        string joinCode = PlayerPrefs.GetString("lastJoinCode", "");
        string savedPlayerId = PlayerPrefs.GetString("unity_player_id", "");

        if (string.IsNullOrEmpty(lobbyId) || string.IsNullOrEmpty(joinCode) || string.IsNullOrEmpty(savedPlayerId))
        {
            Debug.Log("🔁 Reconnect için gerekli PlayerPrefs bilgileri eksik.");
            return;
        }

        if (AuthenticationService.Instance.PlayerId != savedPlayerId)
        {
            Debug.LogWarning("⚠️ Unity PlayerId değişmiş. Eski lobby’ye bağlanılamaz.");
            return;
        }

        try
        {
            var lobby = await LobbyService.Instance.GetLobbyAsync(lobbyId);

            if (!lobby.Players.Any(p => p.Id == savedPlayerId))
            {
                Debug.Log("🚫 Oyuncu bu lobby'de kayıtlı değil.");
                return;
            }

            Debug.Log("🔗 Reconnect uygun. Lobby bulundu. Relay'e bağlanılıyor...");

            var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetClientRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                allocation.HostConnectionData
            );

            NetworkManager.Singleton.StartClient();

            Debug.Log("✅ Reconnect başarılı. PvP sahnesine geçiliyor...");
            SceneManager.LoadScene(gameplaySceneName);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning("❌ LobbyServiceException: " + e.Message);
            ClearSavedReconnectData();
        }
        catch (RelayServiceException e)
        {
            Debug.LogWarning("❌ RelayServiceException: " + e.Message);
            ClearSavedReconnectData();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("❌ Genel Reconnect hatası: " + ex.Message);
            ClearSavedReconnectData();
        }
    }

    private void ClearSavedReconnectData()
    {
        PlayerPrefs.DeleteKey("lastLobbyId");
        PlayerPrefs.DeleteKey("lastJoinCode");
        PlayerPrefs.DeleteKey("unity_player_id");
        PlayerPrefs.Save();
        Debug.Log("🧹 Kaydedilen reconnect bilgileri silindi.");
    }
}