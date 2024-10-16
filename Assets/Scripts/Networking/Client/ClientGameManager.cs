using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientGameManager : IDisposable
{
    private const string menuSceneName = "Menu";
    
    private JoinAllocation _joinAllocation;
    private NetworkClient _networkClient;
    
    public async Task<bool> InitAsync()
    {
        await UnityServices.InitializeAsync();

        _networkClient = new NetworkClient(NetworkManager.Singleton);

        var authState = await AuthenticationWrapper.DoAuthentication();

        if(authState == AuthState.Authenticated)
        {
            return true;
        }

        return false;
    }

    public void GoToMenu()
    {
        SceneManager.LoadScene(menuSceneName);
    }

    public async Task StartClientAsync(string joinCode)
    {
        try
        {
            _joinAllocation = await Relay.Instance.JoinAllocationAsync(joinCode);
        }
        catch (Exception e)
        {
            Debug.Log(e);
            throw;
        }

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        var relayServerData = new RelayServerData(_joinAllocation, "dtls");
        transport.SetRelayServerData(relayServerData);

        var playerData = new PlayerData()
        {
            playerName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, "Missing Name"),
            playerAuthId = AuthenticationService.Instance.PlayerId
        };

        var payload = JsonUtility.ToJson(playerData);
        var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

        NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes;
        
        NetworkManager.Singleton.StartClient();
    }
    
    public void Dispose()
    {
        _networkClient?.Dispose();
    }
}
