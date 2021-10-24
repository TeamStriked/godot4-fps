using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using FPS.Game.Logic.World;

namespace FPS.Game.Logic.Server
{

    public enum ServerClientState
    {
        INIT,
        CONNECTED
    }
    public class ServerClient
    {
        public int id;
        public ServerClientState state;

        public ServerClient(int _id, ServerClientState _state = ServerClientState.CONNECTED)
        {
            this.id = _id;
            this.state = _state;
        }
    }
}