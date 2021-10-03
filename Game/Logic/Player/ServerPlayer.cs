using Godot;
using System;

namespace FPS.Game.Logic.Player
{
    public partial class ServerPlayer : NetworkPlayer
    {

        public override void _Ready()
        {
            base._Ready();
        }

        public override void DoTeleport(Vector3 origin)
        {
            base.DoTeleport(origin);
            GD.Print("Server -> Client -> do teleport to " + origin);
            Rpc("onNetworkTeleport", origin);
        }
    }
}