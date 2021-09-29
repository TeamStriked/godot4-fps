using Godot;
using System;
namespace FPS.Game.Logic.Player
{
    public abstract partial class NetworkPlayer : CharacterBody3D
    {
        public int networkId = 0;

        public NetworkPlayer() : base()
        {
            RpcConfig("onNetworkTeleport", RPCMode.Any, TransferMode.Reliable);
        }

        [Puppet]
        public virtual void onNetworkTeleport(Vector3 origin)
        {
        }


        public virtual void DoTeleport(Vector3 origin)
        {
            var gf = GlobalTransform;
            gf.origin = origin;
            GlobalTransform = gf;
        }
    }
}