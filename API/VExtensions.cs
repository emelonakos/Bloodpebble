using ProjectM;
using ProjectM.Network;
using Unity.Entities;

namespace Bloodstone.API;

/// <summary>
/// Various extensions to make it easier to work with VRising APIs.
/// </summary>
public static class VExtensions
{
    /// <summary>
    /// Send the given system message to the user.
    /// </summary>
    public static void SendSystemMessage(this User user, string message)
    {
        if (!VWorld.IsServer) throw new System.Exception("SendSystemMessage can only be called on the server.");

        var messageString512Bytes = new Unity.Collections.FixedString512Bytes(message); 
        ServerChatUtils.SendSystemMessageToClient(VWorld.Server.EntityManager, user, ref messageString512Bytes);
    }

}