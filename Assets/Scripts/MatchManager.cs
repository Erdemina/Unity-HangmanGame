using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Threading.Tasks;

public class MatchManager : MonoBehaviour
{
    public static MatchManager Instance;

    [Header("Networking")]
    [SerializeField] private GameObject networkManagerPrefab;

    private UnityTransport unityTransport;

    [Header("Settings")]
    [SerializeField] private string gameSceneName = "PvP";
    [SerializeField] private GameObject waitingPanel;

    public Lobby joinedLobby;
    private string joinCode;

    private async void Awake()
    {
        Instance = this;

        if (NetworkManager.Singleton == null && networkManagerPrefab != null)
        {
            var nm = Instantiate(networkManagerPrefab);
            DontDestroyOnLoad(nm);
            Debug.Log("📦 NetworkManager prefab’tan instantiate edildi.");
        }

        await UnityServices.InitializeAsync();

        unityTransport = NetworkManager.Singleton?.GetComponent<UnityTransport>();
    }

    public void ClearLobbyData()
    {
        joinedLobby = null;
        joinCode = null;
    }

    public async Task ResetAllNetworking()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
            Debug.Log("🧹 NetworkManager shutdown yapıldı.");
        }

        if (joinedLobby != null)
        {
            try
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
                {
                    await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
                    Debug.Log("🗑️ Host lobby'yi sildi.");
                }
                else
                {
                    await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
                    Debug.Log("👤 Client lobby'den çıkarıldı.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("⚠️ Lobby temizlenemedi: " + e.Message);
            }
        }

        if (unityTransport == null && NetworkManager.Singleton != null)
            unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        if (unityTransport != null)
            unityTransport.SetConnectionData("127.0.0.1", 7777);

        ClearLobbyData();
    }

    public async void OnStartButtonPressed()
    {
        if (waitingPanel != null)
            waitingPanel.SetActive(true);

        // 👇 Giriş kontrolü: Unity Auth oturumu açık mı?
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogWarning("🚫 Unity Auth oturumu yok. Matchmaking başlatılamaz.");
            return;
        }

        try
        {
            var lobbies = await LobbyService.Instance.QueryLobbiesAsync();

            foreach (var lobby in lobbies.Results)
            {
                if (lobby.Players.Count >= lobby.MaxPlayers)
                    continue;

                if (lobby.Data != null &&
                    lobby.Data.TryGetValue("joinCode", out var joinCodeData) &&
                    !string.IsNullOrEmpty(joinCodeData.Value))
                {
                    Debug.Log("🎯 Katılınabilir boş lobby bulundu. Client olarak katılıyor...");
                    await JoinLobbyAndRelay(lobby);
                    return;
                }
            }

            Debug.Log("🛠️ Uygun lobby bulunamadı. Yeni lobby oluşturuluyor...");
            await CreateLobbyAndRelay();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("❌ OnStartButtonPressed hatası: " + ex.Message);
        }
    }

    private async Task CreateLobbyAndRelay()
    {
        try
        {
            if (unityTransport == null)
                unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            var player = new Player(id: AuthenticationService.Instance.PlayerId);
            var lobbyOptions = new CreateLobbyOptions { IsPrivate = false, Player = player };

            var lobbyTask = LobbyService.Instance.CreateLobbyAsync("Lobby_" + Random.Range(1000, 9999), 2, lobbyOptions);
            var relayTask = RelayService.Instance.CreateAllocationAsync(1);

            await Task.WhenAll(lobbyTask, relayTask);

            joinedLobby = lobbyTask.Result;
            var allocation = relayTask.Result;
            joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            var data = new Dictionary<string, DataObject>
            {
                { "joinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) }
            };
            await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions { Data = data });

            unityTransport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            // 📦 Reconnect bilgileri kaydediliyor
            PlayerPrefs.SetString("lastLobbyId", joinedLobby.Id);
            PlayerPrefs.SetString("lastJoinCode", joinCode);
            PlayerPrefs.SetString("unity_player_id", AuthenticationService.Instance.PlayerId);
            PlayerPrefs.Save();

            NetworkManager.Singleton.StartHost();
            Debug.Log("🚀 Host başlatıldı. Oyuncular bekleniyor...");

            _ = WaitForPlayersThenLoadScene();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("❌ CreateLobbyAndRelay hatası: " + ex.Message);
        }
    }

    private async Task JoinLobbyAndRelay(Lobby lobby)
    {
        try
        {
            if (unityTransport == null)
                unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id);

            if (!lobby.Data.TryGetValue("joinCode", out var joinCodeData) || string.IsNullOrEmpty(joinCodeData.Value))
            {
                Debug.LogWarning("⚠️ JoinCode bulunamadı.");
                return;
            }

            joinCode = joinCodeData.Value;

            var joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);

            unityTransport.SetClientRelayData(
                joinAlloc.RelayServer.IpV4,
                (ushort)joinAlloc.RelayServer.Port,
                joinAlloc.AllocationIdBytes,
                joinAlloc.Key,
                joinAlloc.ConnectionData,
                joinAlloc.HostConnectionData
            );

            // 📦 Reconnect bilgileri kaydediliyor
            PlayerPrefs.SetString("lastLobbyId", lobby.Id);
            PlayerPrefs.SetString("lastJoinCode", joinCode);
            PlayerPrefs.SetString("unity_player_id", AuthenticationService.Instance.PlayerId);
            PlayerPrefs.Save();

            NetworkManager.Singleton.StartClient();
            Debug.Log("🟢 Client Relay'e bağlandı. JoinCode: " + joinCode);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("❌ JoinLobbyAndRelay hatası: " + ex.Message);
        }
    }

    private async Task WaitForPlayersThenLoadScene()
    {
        Debug.Log("⌛ Oyuncular bekleniyor...");
        while (true)
        {
            try
            {
                var refreshedLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                if (refreshedLobby.Players.Count >= 2)
                {
                    Debug.Log("🎯 2 oyuncu eşleşti. Geçiş başlıyor...");
                    await Task.Delay(3000);
                    if (waitingPanel != null)
                        waitingPanel.SetActive(false);

                    await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions { IsPrivate = true });

                    NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
                    break;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("🔄 Lobby güncellenemedi: " + e.Message);
            }

            await Task.Delay(1000);
        }
    }
}