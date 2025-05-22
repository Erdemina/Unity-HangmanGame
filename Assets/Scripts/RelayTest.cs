using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using System.Threading.Tasks;

public class RelayTest : MonoBehaviour
{
    private UnityTransport transport;

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("✅ Auth başarılı: " + AuthenticationService.Instance.PlayerId);
        }

        transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
    }

    private async void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("🚀 Host başlatılıyor...");
            await StartHostRelay();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("🟠 Client başlatılıyor...");
            string joinCode = PlayerPrefs.GetString("RelayJoinCode", "");
            if (!string.IsNullOrEmpty(joinCode))
            {
                await StartClientRelay(joinCode);
            }
            else
            {
                Debug.LogError("❌ Kayıtlı JoinCode yok!");
            }
        }
    }

    private async Task StartHostRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            PlayerPrefs.SetString("RelayJoinCode", joinCode); // Client için kaydet

            transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            NetworkManager.Singleton.StartHost();
            Debug.Log("✅ Host Relay başlatıldı! JoinCode: " + joinCode);
        }
        catch (System.Exception e)
        {
            Debug.LogError("❌ StartHostRelay Hatası: " + e.Message);
        }
    }

    private async Task StartClientRelay(string joinCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            transport.SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            NetworkManager.Singleton.StartClient();
            Debug.Log("✅ Client Relay başlatıldı! JoinCode: " + joinCode);
        }
        catch (System.Exception e)
        {
            Debug.LogError("❌ StartClientRelay Hatası: " + e.Message);
        }
    }
}
