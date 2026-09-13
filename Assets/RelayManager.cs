//using System.Threading.Tasks;
//using Unity.Services.Relay;
//using Unity.Services.Relay.Models;
//using Unity.Networking.Transport.Relay;
//using Unity.Netcode;
//using UnityEngine;

//public static class RelayManager
//{
//    //主机：分配中继服务器，获取加入码
//    public static async Task<(string joinCode, RelayServerData relayData)> CreateRelay(int maxConnections = 4)
//    {
//        try
//        {
//            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
//            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
//            RelayServerData relayData = new RelayServerData(allocation, "dtls");

//            return (joinCode, relayData);
//        }
//        catch (System.Exception e)
//        {
//            Debug.LogError($"创建中继失败: {e.Message}");
//            return (null, default);
//        }
//    }

//    //客户端：通过加入码加入中继
//    public static async Task<RelayServerData> JoinRelay(string joinCode)
//    {
//        try
//        {
//            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
//            RelayServerData relayData = new RelayServerData(allocation, "dtls");
//            return relayData;
//        }
//        catch (System.Exception e)
//        {
//            Debug.LogError($"加入中继失败: {e.Message}");
//            return default;
//        }
//    }
//}