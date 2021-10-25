using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using FPS.Game.Logic.World;
using FPS.Game.Logic.Level;

namespace FPS.Game.Logic.Server
{

    public enum ServerClientState
    {
        INIT,
        Final,
        CONNECTED
    }
    public class ServerClient
    {
        public int id;
        public ServerClientState state;

        public GameSpwanPoint spawnPoint = null;

        public ServerClient(int _id, ServerClientState _state = ServerClientState.CONNECTED)
        {
            this.id = _id;
            this.state = _state;
        }
    }
}