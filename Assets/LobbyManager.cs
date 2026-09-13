//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using Unity.Netcode;
//using Unity.Netcode.Transports.UTP;
//using Unity.Networking.Transport;
//using Unity.Networking.Transport.Relay;
//using Unity.Services.Authentication;
//using Unity.Services.Core;
//using Unity.Services.Lobbies;
//using Unity.Services.Lobbies.Models;
//using UnityEngine;

//public class LobbyManager : MonoBehaviour
//{
//    public static LobbyManager Instance { get; private set; }

//    public Lobby currentLobby;
//    public string currentJoinCode;

//    private float heartbeatTimer;

//    private void Awake()
//    {
//        if (Instance == null) Instance = this;
//        else Destroy(gameObject);
//        DontDestroyOnLoad(gameObject);
//    }

//    private async void Start()
//    {
//        await InitializeUGS();
//    }

//    //初始化UGS和认证（匿名）
//    private async Task InitializeUGS()
//    {
//        try
//        {
//            await UnityServices.InitializeAsync();
//            if (!AuthenticationService.Instance.IsSignedIn)
//            {
//                await AuthenticationService.Instance.SignInAnonymouslyAsync();
//            }
//            Debug.Log($"UGS 初始化成功，玩家ID: {AuthenticationService.Instance.PlayerId}");
//        }
//        catch (System.Exception e)
//        {
//            Debug.LogError($"UGS 初始化失败: {e.Message}");
//        }
//    }

//    //创建大厅 + 自动关联中继
//    public async Task<string> CreateLobbyWithRelay(int maxPlayers = 4)
//    {
//        //1. 先创建中继
//        var (joinCode, relayData) = await RelayManager.CreateRelay(maxPlayers);
//        if (string.IsNullOrEmpty(joinCode))
//        {
//            Debug.LogError("创建中继失败，无法创建大厅");
//            return null;
//        }

//        //2. 创建大厅数据
//        string lobbyName = "房间" + UnityEngine.Random.Range(1000, 9999);
//        CreateLobbyOptions options = new CreateLobbyOptions
//        {
//            IsPrivate = false,
//            Player = new Unity.Services.Lobbies.Models.Player
//            {
//                Data = new Dictionary<string, PlayerDataObject>
//                {
//                    { "JoinCode", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, joinCode) }
//                }
//            }
//        };

//        try
//        {
//            currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
//            currentJoinCode = joinCode;

//            //3. 设置 Netcode 的 Transport（关键步骤）
//            var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
//            if (transport != null)
//            {
//                transport.SetRelayServerData(relayData);
//            }

//            Debug.Log($"大厅创建成功！房间码: {joinCode}，Lobby ID: {currentLobby.Id}");
//            return joinCode;
//        }
//        catch (System.Exception e)
//        {
//            Debug.LogError($"创建大厅失败: {e.Message}");
//            return null;
//        }
//    }

//    //通过房间码加入大厅 + 中继
//    public async Task<bool> JoinLobbyWithRelay(string joinCode)
//    {
//        try
//        {
//            //查询所有公开大厅，手动匹配房间码
//            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(new QueryLobbiesOptions { Count = 20 });
//            foreach (Lobby lobby in response.Results)
//            {
//                if (lobby.Players != null)
//                {
//                    foreach (var player in lobby.Players)
//                    {
//                        if (player.Data != null && player.Data.TryGetValue("JoinCode", out var dataObj))
//                        {
//                            if (dataObj.Value == joinCode)
//                            {
//                                //找到了！加入这个大厅
//                                currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id);
//                                //加入中继
//                                RelayServerData relayData = await RelayManager.JoinRelay(joinCode);
//                                var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
//                                if (transport != null)
//                                {
//                                    transport.SetRelayServerData(relayData);
//                                }
//                                Debug.Log($"成功加入大厅！房间码: {joinCode}");
//                                return true;
//                            }
//                        }
//                    }
//                }
//            }

//            Debug.LogWarning($"未找到房间码为 {joinCode} 的大厅");
//            return false;
//        }
//        catch (System.Exception e)
//        {
//            Debug.LogError($"加入大厅失败: {e.Message}");
//            return false;
//        }
//    }

//    //心跳（保持大厅活跃）
//    private void Update()
//    {
//        if (currentLobby != null && !string.IsNullOrEmpty(currentLobby.Id))
//        {
//            heartbeatTimer += Time.deltaTime;
//            if (heartbeatTimer >= 15f)
//            {
//                heartbeatTimer = 0f;
//                LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
//            }
//        }
//    }

//    public async void LeaveLobby()
//    {
//        if (currentLobby != null)
//        {
//            try
//            {
//                await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
//            }
//            catch { }
//            currentLobby = null;
//            currentJoinCode = null;
//        }
//    }
//}